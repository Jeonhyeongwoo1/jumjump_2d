using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using JumJump.Bridge;
using JumJump.Data;
using JumJump.Event;
using JumJump.Interface;
using UnityEngine;
using VContainer;

namespace JumJump.Service
{
    public sealed class IAPService : IIAPService, IDisposable
    {
        private readonly AppInTossConfigSO _config;
        private readonly IEventBus _eventBus;
        private CancellationTokenSource _cts;

        [Inject]
        public IAPService(AppInTossConfigSO config, IEventBus eventBus)
        {
            _config = config;
            _eventBus = eventBus;
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        public async UniTask<IAPResult> PurchaseAsync(string productId, CancellationToken cancellationToken = default)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            try
            {
                _eventBus.Publish(new IAPEventLoggedEvent(productId, "purchase_start"));
                IAPResult result = await DoPurchaseAsync(productId, _cts.Token);
                PublishPurchaseResult(productId, result);
                return result;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _eventBus.Publish(new IAPEventLoggedEvent(productId, "purchase_failed", error: ex.Message));
                Debug.LogError($"[{nameof(IAPService)}] Purchase failed: {ex.Message}");
                return IAPResult.Failure(ex.Message);
            }
        }

        private async UniTask<IAPResult> DoPurchaseAsync(string productId, CancellationToken cancellationToken)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            AppInTossIAPWebGL.Purchase(productId);
            await UniTask.WaitUntil(AppInTossIAPWebGL.IsPurchaseCompleted, cancellationToken: cancellationToken)
                .Timeout(TimeSpan.FromMilliseconds(_config.IAPPurchaseTimeoutMs), DelayType.Realtime);
            string error = AppInTossIAPWebGL.GetPurchaseError();
            if (!string.IsNullOrEmpty(error))
            {
                return error == "cancelled" ? IAPResult.Cancelled() : IAPResult.Failure(error);
            }
            string json = AppInTossIAPWebGL.GetPurchaseResultJson();
            var response = JsonUtility.FromJson<IAPPurchaseResponse>(json);
            if (response == null || string.IsNullOrEmpty(response.orderId))
            {
                return IAPResult.Failure("invalid_purchase_response");
            }
            return IAPResult.Success(response.orderId, response.productId, response.purchasedAtMillis);
#else
            await UniTask.Delay(_config.EditorIAPDelayMs, DelayType.Realtime, cancellationToken: cancellationToken);
            return IAPResult.Success($"editor-order-{Guid.NewGuid():N}", productId, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
#endif
        }

        private void PublishPurchaseResult(string requestedProductId, IAPResult result)
        {
            if (result.IsSuccess)
            {
                _eventBus.Publish(new IAPEventLoggedEvent(result.ProductId, "purchase_success", result.OrderId));
                return;
            }
            if (result.IsCancelled)
            {
                _eventBus.Publish(new IAPEventLoggedEvent(requestedProductId, "purchase_cancelled"));
                return;
            }
            _eventBus.Publish(new IAPEventLoggedEvent(requestedProductId, "purchase_failed", error: result.Error));
        }
    }
}
