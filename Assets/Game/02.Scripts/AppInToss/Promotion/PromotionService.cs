using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using JumJump.Bridge;
using JumJump.Data;
using JumJump.Event;
using JumJump.Interface;
using JumJump.Registry;
using JumJump.Util;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace JumJump.Service
{
    public sealed class PromotionService : IInitializable, IDisposable
    {
        private const string FirstPlayCampaignType = "first_play_complete";
        private const string FirstPlayClaimKeyPrefix = "appintoss_promotion_first_play_";

        private readonly IEventBus _eventBus;
        private readonly AppInTossConfigSO _config;
        private readonly AuthRegistry _authRegistry;

        private CancellationTokenSource _grantCts;
        private bool _isGranting;

        [Inject]
        public PromotionService(
            IEventBus eventBus,
            AppInTossConfigSO config,
            AuthRegistry authRegistry)
        {
            _eventBus = eventBus;
            _config = config;
            _authRegistry = authRegistry;
        }

        public void Initialize()
        {
            _eventBus.Subscribe<GameOverResultViewRequestedEvent>(OnGameOverResultViewRequested);
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<GameOverResultViewRequestedEvent>(OnGameOverResultViewRequested);
            DisposeGrantRequest();
        }

        private void OnGameOverResultViewRequested(in GameOverResultViewRequestedEvent ev)
        {
            if (!HasConfiguredPromotion())
            {
                return;
            }

            if (_isGranting)
            {
                return;
            }

            StartGrantRequest();
        }

        private async UniTask GrantEligiblePromotionsAsync(CancellationToken cancellationToken)
        {
            _isGranting = true;

            try
            {
                if (!_authRegistry.IsLoggedIn)
                {
                    PublishPromotionEvent(string.Empty, "auth_required", "grant_skipped", 0, errorCode: "auth_required");
                    return;
                }

                await TryGrantPromotionAsync(
                    FirstPlayCampaignType,
                    _config.FirstPlayPromotionCode,
                    _config.FirstPlayPromotionAmount,
                    ResolveFirstPlayClaimKey(),
                    cancellationToken);
            }
            catch (OperationCanceledException)
            {
                GameLogger.Warning(nameof(PromotionService), "promotion_grant_cancelled");
            }
            catch (Exception ex)
            {
                GameLogger.Error(nameof(PromotionService), $"promotion_grant_failed: {ex.Message}");
            }
            finally
            {
                _isGranting = false;
            }
        }

        private async UniTask TryGrantPromotionAsync(
            string campaignType,
            string promotionCode,
            int amount,
            string claimKey,
            CancellationToken cancellationToken)
        {
            if (!IsPromotionConfigured(promotionCode, amount) || IsClaimed(claimKey))
            {
                return;
            }

            PublishPromotionEvent(promotionCode, campaignType, "grant_requested", amount);
            var result = await GrantPromotionRewardAsync(campaignType, promotionCode, amount, cancellationToken);
            if (result.IsSuccess)
            {
                MarkClaimed(claimKey);
                PublishPromotionEvent(promotionCode, campaignType, "grant_success", amount, result.RewardKey);
                return;
            }

            if (result.ErrorCode == "4113")
            {
                MarkClaimed(claimKey);
                PublishPromotionEvent(promotionCode, campaignType, "already_granted", amount, errorCode: result.ErrorCode);
                return;
            }

            var eventType = result.IsUnsupported ? "grant_unsupported" : "grant_failed";
            PublishPromotionEvent(
                promotionCode,
                campaignType,
                eventType,
                amount,
                errorCode: result.ErrorCode,
                errorMessage: result.ErrorMessage);
        }

        private async UniTask<PromotionRewardResult> GrantPromotionRewardAsync(
            string campaignType,
            string promotionCode,
            int amount,
            CancellationToken cancellationToken)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            AppInTossPromotionWebGL.Grant(promotionCode, amount);
            await UniTask.WaitUntil(AppInTossPromotionWebGL.IsGrantCompleted, cancellationToken: cancellationToken)
                .Timeout(TimeSpan.FromMilliseconds(Mathf.Max(1, _config.PromotionGrantTimeoutMs)), DelayType.Realtime);

            var error = AppInTossPromotionWebGL.GetGrantError();
            if (!string.IsNullOrEmpty(error))
            {
                return PromotionRewardResult.Failure(error);
            }

            var json = AppInTossPromotionWebGL.GetGrantResultJson();
            if (string.IsNullOrEmpty(json))
            {
                return PromotionRewardResult.Failure("empty_promotion_response");
            }

            var response = JsonUtility.FromJson<PromotionRewardResponse>(json);
            if (response == null)
            {
                return PromotionRewardResult.Failure("invalid_promotion_response");
            }

            if (response.unsupported)
            {
                return PromotionRewardResult.Unsupported();
            }

            if (response.success && !string.IsNullOrEmpty(response.key))
            {
                return PromotionRewardResult.Success(response.key);
            }

            if (!string.IsNullOrEmpty(response.errorCode))
            {
                return PromotionRewardResult.Failure(response.errorCode, response.message);
            }

            return PromotionRewardResult.Failure("invalid_promotion_response");
#else
            await UniTask.Delay(
                Mathf.Max(1, _config.EditorPromotionGrantDelayMs),
                DelayType.Realtime,
                cancellationToken: cancellationToken);
            return PromotionRewardResult.Success($"editor-{campaignType}-{Guid.NewGuid():N}");
#endif
        }

        private bool HasConfiguredPromotion()
        {
            return IsPromotionConfigured(_config.FirstPlayPromotionCode, _config.FirstPlayPromotionAmount);
        }

        private bool IsPromotionConfigured(string promotionCode, int amount)
        {
            return !string.IsNullOrWhiteSpace(promotionCode) && amount > 0;
        }

        private string ResolveFirstPlayClaimKey()
        {
            return $"{FirstPlayClaimKeyPrefix}{_authRegistry.UserId}_{_config.FirstPlayPromotionCode}";
        }

        private bool IsClaimed(string claimKey)
        {
            return PlayerPrefs.GetInt(claimKey, 0) == 1;
        }

        private void MarkClaimed(string claimKey)
        {
            PlayerPrefs.SetInt(claimKey, 1);
            PlayerPrefs.Save();
        }

        private void PublishPromotionEvent(
            string promotionCode,
            string campaignType,
            string eventType,
            int amount,
            string rewardKey = "",
            string errorCode = "",
            string errorMessage = "")
        {
            _eventBus.Publish(new PromotionEventLoggedEvent(
                promotionCode,
                campaignType,
                eventType,
                amount,
                rewardKey,
                errorCode,
                errorMessage));
        }

        private void StartGrantRequest()
        {
            DisposeGrantRequest();
            _grantCts = new CancellationTokenSource();
            GrantEligiblePromotionsAsync(_grantCts.Token).Forget();
        }

        private void DisposeGrantRequest()
        {
            if (_grantCts == null)
            {
                return;
            }

            _grantCts.Cancel();
            _grantCts.Dispose();
            _grantCts = null;
        }
    }
}
