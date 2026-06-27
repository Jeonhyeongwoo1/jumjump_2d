using System;
using JumJump.Data;
using JumJump.Event;
using JumJump.Factory;
using JumJump.Interface;
using JumJump.Util;
using UnityEngine;
using VContainer.Unity;

namespace JumJump.Service
{
    public sealed class SoundService : IInitializable, IDisposable
    {
        private readonly IEventBus _eventBus;
        private readonly GameConfigData _configData;
        private readonly SoundFactory _soundFactory;

        private Transform _root;
        private AudioSource _bgmSource;
        private AudioSource _sfxSource;
        private bool _isAudioUnlocked;

        public SoundService(
            IEventBus eventBus,
            GameConfigData configData,
            SoundFactory soundFactory)
        {
            _eventBus = eventBus;
            _configData = configData;
            _soundFactory = soundFactory;
        }

        public void Initialize()
        {
            _root = _soundFactory.CreateRoot();
            _bgmSource = _soundFactory.CreateAudioSource(_root, "BGM", true);
            _sfxSource = _soundFactory.CreateAudioSource(_root, "SFX", false);
            ApplyVolumes();

            _eventBus.Subscribe<TapRequestedEvent>(OnTapRequested);
            _eventBus.Subscribe<SoundRequestedEvent>(OnSoundRequested);
            _eventBus.Subscribe<GameStartedEvent>(OnGameStarted);
            _eventBus.Subscribe<GameResetEvent>(OnGameReset);
            _eventBus.Subscribe<PlayerJumpStartedEvent>(OnPlayerJumpStarted);
            _eventBus.Subscribe<PlayerMissedLandingEvent>(OnPlayerMissedLanding);
            _eventBus.Subscribe<PlayerDeadAnimationStartedEvent>(OnPlayerDeadAnimationStarted);
            _eventBus.Subscribe<ScoreChangedEvent>(OnScoreChanged);
            _eventBus.Subscribe<GoldChangedEvent>(OnGoldChanged);
            _eventBus.Subscribe<GameOverEvent>(OnGameOver);
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<TapRequestedEvent>(OnTapRequested);
            _eventBus.Unsubscribe<SoundRequestedEvent>(OnSoundRequested);
            _eventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);
            _eventBus.Unsubscribe<GameResetEvent>(OnGameReset);
            _eventBus.Unsubscribe<PlayerJumpStartedEvent>(OnPlayerJumpStarted);
            _eventBus.Unsubscribe<PlayerMissedLandingEvent>(OnPlayerMissedLanding);
            _eventBus.Unsubscribe<PlayerDeadAnimationStartedEvent>(OnPlayerDeadAnimationStarted);
            _eventBus.Unsubscribe<ScoreChangedEvent>(OnScoreChanged);
            _eventBus.Unsubscribe<GoldChangedEvent>(OnGoldChanged);
            _eventBus.Unsubscribe<GameOverEvent>(OnGameOver);

            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root.gameObject);
            }
        }

        private void OnTapRequested(in TapRequestedEvent ev)
        {
            UnlockAudio();
        }

        private void OnSoundRequested(in SoundRequestedEvent ev)
        {
            UnlockAudio();

            if (ev.Type == GameSoundType.BgmGameLoop)
            {
                StartBgm();
                return;
            }

            PlayOneShot(ev.Type);
        }

        private void OnGameStarted(in GameStartedEvent ev)
        {
            StartBgm();
        }

        private void OnGameReset(in GameResetEvent ev)
        {
            StopBgm();
        }

        private void OnPlayerJumpStarted(in PlayerJumpStartedEvent ev)
        {
            PlayOneShot(GameSoundType.PlayerJump);
        }

        private void OnPlayerMissedLanding(in PlayerMissedLandingEvent ev)
        {
            StopBgm();
        }

        private void OnPlayerDeadAnimationStarted(in PlayerDeadAnimationStartedEvent ev)
        {
            PlayOneShot(GameSoundType.PlayerDead);
        }

        private void OnScoreChanged(in ScoreChangedEvent ev)
        {
            if (ev.ScoreDelta <= 0)
            {
                return;
            }

            PlayOneShot(GameSoundType.LandingNormal);
        }

        private void OnGoldChanged(in GoldChangedEvent ev)
        {
            if (ev.GoldDelta > 0)
            {
                PlayOneShot(GameSoundType.GoldCollect);
            }
        }

        private void OnGameOver(in GameOverEvent ev)
        {
            StopBgm();
        }

        private void UnlockAudio()
        {
            _isAudioUnlocked = true;
        }

        private void ApplyVolumes()
        {
            _bgmSource.volume = Mathf.Clamp01(_configData.MasterVolume) * Mathf.Clamp01(_configData.BgmVolume);
            _sfxSource.volume = Mathf.Clamp01(_configData.MasterVolume) * Mathf.Clamp01(_configData.SfxVolume);
        }

        private void StartBgm()
        {
            if (!CanPlayBgm())
            {
                return;
            }

            var clip = _configData.BgmGameLoopClip;
            if (clip == null)
            {
                GameLogger.Debug(nameof(SoundService), "BGM clip is not assigned.");
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

        private void PlayOneShot(GameSoundType soundType)
        {
            if (!CanPlaySfx())
            {
                return;
            }

            var clip = ResolveClip(soundType);
            if (clip == null)
            {
                GameLogger.Debug(nameof(SoundService), $"Sound clip is not assigned: {soundType}");
                return;
            }

            _sfxSource.PlayOneShot(clip);
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

        private AudioClip ResolveClip(GameSoundType soundType)
        {
            switch (soundType)
            {
                case GameSoundType.BgmGameLoop:
                    return _configData.BgmGameLoopClip;
                case GameSoundType.UiButtonTap:
                    return _configData.UiButtonTapClip;
                case GameSoundType.UiCountdownTick:
                    return _configData.UiCountdownTickClip;
                case GameSoundType.PlayerJump:
                    return _configData.PlayerJumpClip;
                case GameSoundType.LandingNormal:
                    return _configData.LandingNormalClip;
                case GameSoundType.GoldCollect:
                    return _configData.GoldCollectClip;
                case GameSoundType.PlayerMiss:
                    return _configData.PlayerMissClip;
                case GameSoundType.PlayerDead:
                    return _configData.PlayerDeadClip;
                default:
                    throw new ArgumentOutOfRangeException(nameof(soundType), soundType, null);
            }
        }
    }
}
