using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using JumJump.Bridge;
using JumJump.Data;
using JumJump.Event;
using JumJump.Factory;
using JumJump.Interface;
using JumJump.Util;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace JumJump.Service
{
    public sealed class SoundService : IInitializable, IDisposable
    {
        private readonly IEventBus _eventBus;
        private readonly GameConfigData _configData;
        private readonly ResourceService _resourceService;
        private readonly SoundFactory _soundFactory;
        private readonly Dictionary<GameSoundType, AudioClip> _loadedClips = new Dictionary<GameSoundType, AudioClip>(8);

        private Transform _root;
        private AudioSource _bgmSource;
        private AudioSource _sfxSource;
        private CancellationTokenSource _destroyCancellation;
        private bool _isAudioUnlocked;
        private bool _shouldBgmBePlaying;
        private bool _isPreloadingCoreAudio;
        private bool _hasPreloadedCoreAudio;

        [Inject]
        public SoundService(
            IEventBus eventBus,
            GameConfigData configData,
            ResourceService resourceService,
            SoundFactory soundFactory)
        {
            _eventBus = eventBus;
            _configData = configData;
            _resourceService = resourceService;
            _soundFactory = soundFactory;
        }

        public void Initialize()
        {
            _destroyCancellation = new CancellationTokenSource();
            _root = _soundFactory.CreateRoot();
            _bgmSource = _soundFactory.CreateAudioSource(_root, "BGM", true);
            _sfxSource = _soundFactory.CreateAudioSource(_root, "SFX", false);
            ApplyVolumes();

            _eventBus.Subscribe<TapRequestedEvent>(OnTapRequested);
            _eventBus.Subscribe<SoundRequestedEvent>(OnSoundRequested);
            _eventBus.Subscribe<GameResourcesReadyEvent>(OnGameResourcesReady);
            _eventBus.Subscribe<GameStartedEvent>(OnGameStarted);
            _eventBus.Subscribe<GameResetEvent>(OnGameReset);
            _eventBus.Subscribe<PlayerJumpStartedEvent>(OnPlayerJumpStarted);
            _eventBus.Subscribe<PlayerMissedLandingEvent>(OnPlayerMissedLanding);
            _eventBus.Subscribe<PlayerDeadAnimationStartedEvent>(OnPlayerDeadAnimationStarted);
            _eventBus.Subscribe<ScoreChangedEvent>(OnScoreChanged);
            _eventBus.Subscribe<GoldChangedEvent>(OnGoldChanged);
            _eventBus.Subscribe<AdEventLoggedEvent>(OnAdEventLogged);
            _eventBus.Subscribe<GameOverEvent>(OnGameOver);
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<TapRequestedEvent>(OnTapRequested);
            _eventBus.Unsubscribe<SoundRequestedEvent>(OnSoundRequested);
            _eventBus.Unsubscribe<GameResourcesReadyEvent>(OnGameResourcesReady);
            _eventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);
            _eventBus.Unsubscribe<GameResetEvent>(OnGameReset);
            _eventBus.Unsubscribe<PlayerJumpStartedEvent>(OnPlayerJumpStarted);
            _eventBus.Unsubscribe<PlayerMissedLandingEvent>(OnPlayerMissedLanding);
            _eventBus.Unsubscribe<PlayerDeadAnimationStartedEvent>(OnPlayerDeadAnimationStarted);
            _eventBus.Unsubscribe<ScoreChangedEvent>(OnScoreChanged);
            _eventBus.Unsubscribe<GoldChangedEvent>(OnGoldChanged);
            _eventBus.Unsubscribe<AdEventLoggedEvent>(OnAdEventLogged);
            _eventBus.Unsubscribe<GameOverEvent>(OnGameOver);

            if (_destroyCancellation != null)
            {
                _destroyCancellation.Cancel();
                _destroyCancellation.Dispose();
                _destroyCancellation = null;
            }

            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root.gameObject);
            }

            _loadedClips.Clear();
        }

        private void OnTapRequested(in TapRequestedEvent ev)
        {
            UnlockAudio();
            RestoreAudioOutput();
            PreloadCoreAudioAsync().Forget();
        }

        private void OnSoundRequested(in SoundRequestedEvent ev)
        {
            UnlockAudio();
            RestoreAudioOutput();

            if (ev.Type == GameSoundType.BgmGameLoop)
            {
                StartBgmAsync().Forget();
                return;
            }

            PlayOneShotAsync(ev.Type).Forget();
        }

        private void OnGameResourcesReady(in GameResourcesReadyEvent ev)
        {
            PreloadCoreAudioAsync().Forget();
        }

        private void OnGameStarted(in GameStartedEvent ev)
        {
            _shouldBgmBePlaying = true;
            RestoreAudioOutput();
            StartBgmAsync().Forget();
        }

        private void OnGameReset(in GameResetEvent ev)
        {
            _shouldBgmBePlaying = false;
            StopBgm();
        }

        private void OnPlayerJumpStarted(in PlayerJumpStartedEvent ev)
        {
            PlayOneShotAsync(GameSoundType.PlayerJump).Forget();
        }

        private void OnPlayerMissedLanding(in PlayerMissedLandingEvent ev)
        {
            _shouldBgmBePlaying = false;
            StopBgm();
        }

        private void OnPlayerDeadAnimationStarted(in PlayerDeadAnimationStartedEvent ev)
        {
            PlayOneShotAsync(GameSoundType.PlayerDead).Forget();
        }

        private void OnScoreChanged(in ScoreChangedEvent ev)
        {
            if (ev.ScoreDelta <= 0)
            {
                return;
            }

            PlayOneShotAsync(GameSoundType.LandingNormal).Forget();
        }

        private void OnGoldChanged(in GoldChangedEvent ev)
        {
            if (ev.GoldDelta > 0)
            {
                PlayOneShotAsync(GameSoundType.GoldCollect).Forget();
            }
        }

        private void OnGameOver(in GameOverEvent ev)
        {
            _shouldBgmBePlaying = false;
            StopBgm();
        }

        private void OnAdEventLogged(in AdEventLoggedEvent ev)
        {
            if (ev.EventType != "show_rewarded" &&
                ev.EventType != "show_dismissed" &&
                ev.EventType != "show_failed")
            {
                return;
            }

            RestoreAudioOutput();
            if (_shouldBgmBePlaying)
            {
                StartBgmAsync().Forget();
            }
        }

        private void UnlockAudio()
        {
            _isAudioUnlocked = true;
        }

        private void RestoreAudioOutput()
        {
            AudioListener.pause = false;
            AudioListener.volume = 1f;
            ApplyVolumes();
#if UNITY_WEBGL && !UNITY_EDITOR
            AppInTossAdWebGL.ResumeAudio();
#endif
        }

        private void ApplyVolumes()
        {
            _bgmSource.volume = Mathf.Clamp01(_configData.MasterVolume) * Mathf.Clamp01(_configData.BgmVolume);
            _sfxSource.volume = Mathf.Clamp01(_configData.MasterVolume) * Mathf.Clamp01(_configData.SfxVolume);
        }

        private async UniTask StartBgmAsync()
        {
            if (!CanPlayBgm())
            {
                return;
            }

            var clip = await LoadClipAsync(GameSoundType.BgmGameLoop);
            if (clip == null)
            {
                return;
            }

            await PrepareAudioDataAsync(clip);

            if (!CanPlayBgm())
            {
                return;
            }

            if (_bgmSource.isPlaying && _bgmSource.clip == clip)
            {
                return;
            }

            _bgmSource.clip = clip;
            _bgmSource.Play();
        }

        private void StopBgm()
        {
            if (_bgmSource.isPlaying)
            {
                _bgmSource.Stop();
            }
        }

        private async UniTask PlayOneShotAsync(GameSoundType soundType)
        {
            if (!CanPlaySfx())
            {
                return;
            }

            var clip = await LoadClipAsync(soundType);
            if (clip == null)
            {
                return;
            }

            await PrepareAudioDataAsync(clip);

            if (!CanPlaySfx())
            {
                return;
            }

            _sfxSource.PlayOneShot(clip);
        }

        private async UniTask PreloadCoreAudioAsync()
        {
            if (_hasPreloadedCoreAudio || _isPreloadingCoreAudio)
            {
                return;
            }

            _isPreloadingCoreAudio = true;

            try
            {
                await PreloadClipAsync(GameSoundType.UiButtonTap);
                await PreloadClipAsync(GameSoundType.UiCountdownTick);
                await PreloadClipAsync(GameSoundType.PlayerJump);
                await PreloadClipAsync(GameSoundType.LandingNormal);
                await PreloadClipAsync(GameSoundType.GoldCollect);
                await PreloadClipAsync(GameSoundType.PlayerMiss);
                await PreloadClipAsync(GameSoundType.PlayerDead);
                await PreloadClipAsync(GameSoundType.BgmGameLoop);
                _hasPreloadedCoreAudio = true;
            }
            catch (OperationCanceledException)
            {
                return;
            }
            finally
            {
                _isPreloadingCoreAudio = false;
            }
        }

        private async UniTask PreloadClipAsync(GameSoundType soundType)
        {
            var clip = await LoadClipAsync(soundType);
            if (clip == null)
            {
                return;
            }

            await PrepareAudioDataAsync(clip);

            if (soundType == GameSoundType.BgmGameLoop)
            {
                _bgmSource.clip = clip;
            }
        }

        private async UniTask<AudioClip> LoadClipAsync(GameSoundType soundType)
        {
            if (_loadedClips.TryGetValue(soundType, out var cachedClip))
            {
                return cachedClip;
            }

            var key = ResolveClipKey(soundType);
            if (string.IsNullOrWhiteSpace(key))
            {
                GameLogger.Debug(nameof(SoundService), $"Sound clip key is not assigned: {soundType}");
                return null;
            }

            if (_destroyCancellation == null)
            {
                return null;
            }

            try
            {
                var loaded = await _resourceService.LoadKeyAsync(key, _destroyCancellation.Token);
                if (!loaded)
                {
                    GameLogger.Debug(nameof(SoundService), $"Sound clip failed to load: {soundType}, Key: {key}");
                    return null;
                }
            }
            catch (OperationCanceledException)
            {
                return null;
            }

            var clip = _resourceService.GetAsset<AudioClip>(key);
            if (clip == null)
            {
                GameLogger.Debug(nameof(SoundService), $"Sound clip is not assigned: {soundType}, Key: {key}");
                return null;
            }

            _loadedClips[soundType] = clip;
            return clip;
        }

        private async UniTask PrepareAudioDataAsync(AudioClip clip)
        {
            if (clip.loadState == AudioDataLoadState.Unloaded)
            {
                clip.LoadAudioData();
            }

            if (_destroyCancellation == null)
            {
                return;
            }

            while (clip.loadState == AudioDataLoadState.Loading)
            {
                var isCanceled = await UniTask.Yield(PlayerLoopTiming.Update, _destroyCancellation.Token).SuppressCancellationThrow();
                if (isCanceled)
                {
                    return;
                }
            }
        }

        private bool CanPlayBgm()
        {
            return _configData.SoundEnabled &&
                   _configData.BgmEnabled &&
                   _isAudioUnlocked;
        }

        private bool CanPlaySfx()
        {
            return _configData.SoundEnabled &&
                   _configData.SfxEnabled &&
                   _isAudioUnlocked;
        }

        private string ResolveClipKey(GameSoundType soundType)
        {
            switch (soundType)
            {
                case GameSoundType.BgmGameLoop:
                    return _configData.BgmGameLoopAddressableKey;
                case GameSoundType.UiButtonTap:
                    return _configData.UiButtonTapAddressableKey;
                case GameSoundType.UiCountdownTick:
                    return _configData.UiCountdownTickAddressableKey;
                case GameSoundType.PlayerJump:
                    return _configData.PlayerJumpAddressableKey;
                case GameSoundType.LandingNormal:
                    return _configData.LandingNormalAddressableKey;
                case GameSoundType.GoldCollect:
                    return _configData.GoldCollectAddressableKey;
                case GameSoundType.PlayerMiss:
                    return _configData.PlayerMissAddressableKey;
                case GameSoundType.PlayerDead:
                    return _configData.PlayerDeadAddressableKey;
                default:
                    throw new ArgumentOutOfRangeException(nameof(soundType), soundType, null);
            }
        }
    }
}
