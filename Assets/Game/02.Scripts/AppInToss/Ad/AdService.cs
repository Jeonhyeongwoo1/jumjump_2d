using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using JumJump.Bridge;
using JumJump.Data;
using JumJump.Event;
using JumJump.Interface;
using UnityEngine;

namespace JumJump.Service
{
    public sealed class AdService : IAdService, IDisposable
    {
        private readonly IEventBus _eventBus;
        private readonly AppInTossConfigSO _config;
        private readonly ScoreService _scoreService;
        private CancellationTokenSource _cts;

        public AdService(IEventBus eventBus, AppInTossConfigSO config, ScoreService scoreService)
        {
            _eventBus = eventBus;
            _config = config;
            _scoreService = scoreService;
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        public async UniTask<AdResult> ShowRewardedAdAsync(string adGroupId, CancellationToken cancellationToken = default)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            try
            {
                await LoadAsync(adGroupId, _cts.Token);
                AdResult result = await ShowAsync(adGroupId, _cts.Token);
                if (result.HasReward)
                {
                    _eventBus.Publish(new AdRewardedEvent(adGroupId, result.RewardType, result.RewardAmount));
                }
                return result;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[{nameof(AdService)}] Ad failed: {ex.Message}");
                return AdResult.Failure(ex.Message);
            }
        }

        private async UniTask LoadAsync(string adGroupId, CancellationToken cancellationToken)
        {
            PublishAdEvent(adGroupId, "load_requested");
            try
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                AppInTossAdWebGL.Load(adGroupId, _scoreService.Score);
                await UniTask.WaitUntil(AppInTossAdWebGL.IsLoadCompleted, cancellationToken: cancellationToken)
                    .Timeout(TimeSpan.FromMilliseconds(_config.AdLoadTimeoutMs));
                string error = AppInTossAdWebGL.GetLoadError();
                if (!string.IsNullOrEmpty(error))
                {
                    throw new InvalidOperationException(error);
                }
#else
                await UniTask.Delay(_config.EditorAdLoadDelayMs, cancellationToken: cancellationToken);
#endif
                PublishAdEvent(adGroupId, "load_completed");
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                {
                    throw;
                }
                PublishAdEvent(adGroupId, "load_failed", ex.Message);
                throw;
            }
        }

        private async UniTask<AdResult> ShowAsync(string adGroupId, CancellationToken cancellationToken)
        {
            PublishAdEvent(adGroupId, "show_requested");
            try
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                AppInTossAdWebGL.Show(adGroupId, _scoreService.Score);
                await UniTask.WaitUntil(AppInTossAdWebGL.IsShowCompleted, cancellationToken: cancellationToken)
                    .Timeout(TimeSpan.FromMilliseconds(_config.AdShowTimeoutMs));
                string error = AppInTossAdWebGL.GetShowError();
                if (!string.IsNullOrEmpty(error))
                {
                    PublishAdEvent(adGroupId, "show_failed", error);
                    return AdResult.Failure(error);
                }
                AdResult result = AppInTossAdWebGL.HasReward()
                    ? AdResult.Rewarded(AppInTossAdWebGL.GetRewardType(), AppInTossAdWebGL.GetRewardAmount())
                    : AdResult.Dismissed();
#else
                await UniTask.Delay(_config.EditorAdShowDelayMs, cancellationToken: cancellationToken);
                AdResult result = AdResult.Rewarded(_config.EditorAdRewardType, _config.EditorAdRewardAmount);
#endif
                PublishAdResultEvent(adGroupId, result);
                return result;
            }
            catch (Exception ex)
            {
                if (ex is OperationCanceledException)
                {
                    throw;
                }
                PublishAdEvent(adGroupId, "show_failed", ex.Message);
                throw;
            }
        }

        private void PublishAdResultEvent(string adGroupId, AdResult result)
        {
            if (result.HasReward)
            {
                PublishAdEvent(adGroupId, "show_rewarded", hasReward: true, rewardType: result.RewardType, rewardAmount: result.RewardAmount);
                return;
            }
            PublishAdEvent(adGroupId, "show_dismissed");
        }

        private void PublishAdEvent(string adGroupId, string eventType, string error = "", bool hasReward = false, string rewardType = "", int rewardAmount = 0)
        {
            _eventBus.Publish(new AdEventLoggedEvent(adGroupId, eventType, _scoreService.Score, error, hasReward, rewardType, rewardAmount));
        }
    }
}
