using System.Threading;
using Cysharp.Threading.Tasks;
using JumJump.Data;
using JumJump.Event;
using JumJump.Interface;
using JumJump.Service;
using UnityEngine;

namespace JumJump.Presenter
{
    public sealed class UIGameOverPopupPresenter
    {
        private readonly IEventBus _eventBus;
        private readonly PopupService _popupService;
        private readonly GameConfigData _configData;

        private UI_GameOverPopup _view;
        private CancellationTokenSource _countdownCts;
        private const int CountdownSeconds = 5;

        public bool IsShowing => _view != null && _view.gameObject.activeSelf;

        public UIGameOverPopupPresenter(IEventBus eventBus, PopupService popupService, GameConfigData configData)
        {
            _eventBus = eventBus;
            _popupService = popupService;
            _configData = configData;
        }

        public void Show()
        {
            var view = _popupService.Push<UI_GameOverPopup>(_configData.GameOverPopupAddressableKey);
            _view = view;
            _view.AddEvents(OnAdClicked, OnCloseClicked);
            
            _countdownCts?.Cancel();
            _countdownCts?.Dispose();
            _countdownCts = new CancellationTokenSource();
            RunCountdownAsync(_countdownCts.Token).Forget();
        }

        public void Hide()
        {
            _countdownCts?.Cancel();
            _popupService.PopAll();
        }

        private async UniTask RunCountdownAsync(CancellationToken ct)
        {
            var remaining = (float)CountdownSeconds;

            while (remaining > 0f && !ct.IsCancellationRequested)
            {
                remaining -= Time.deltaTime;
                remaining = Mathf.Max(0f, remaining);
                _view?.SetCountdown(remaining / CountdownSeconds, Mathf.CeilToInt(remaining));
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            if (!ct.IsCancellationRequested)
            {
                _popupService.Pop();
            }
        }

        private void OnAdClicked()
        {
            _countdownCts?.Cancel();
            _popupService.Pop();
            _eventBus.Publish(new RestartRequestedEvent());
        }

        private void OnCloseClicked()
        {
            _countdownCts?.Cancel();
            _popupService.Pop();
        }
    }
}
