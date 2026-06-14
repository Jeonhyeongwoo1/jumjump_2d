using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using JumJump.Data;
using JumJump.Event;
using JumJump.Interface;
using JumJump.Service;
using JumJump.Util;
using UnityEngine;

namespace JumJump.Presenter
{
    public sealed class UIGameOverPopupPresenter : IDisposable
    {
        private readonly IEventBus _eventBus;
        private readonly PopupService _popupService;
        private readonly ResourceConfigData _resourceConfigData;

        private UI_GameOverPopup _view;
        private CancellationTokenSource _countdownCts;
        private CancellationTokenSource _resultRestartCts;

        public bool IsShowing => _view != null && _view.gameObject.activeSelf;

        public UIGameOverPopupPresenter(IEventBus eventBus, PopupService popupService, ResourceConfigData resourceConfigData)
        {
            _eventBus = eventBus;
            _popupService = popupService;
            _resourceConfigData = resourceConfigData;
        }

        public void Show()
        {
            var view = _popupService.Push<UI_GameOverPopup>(_resourceConfigData.GameOverPopupAddressableKey);
            if (view == null)
            {
                return;
            }

            _view = view;
            _view.AddEvents(OnAdClicked, OnCloseClicked);

            DisposeCountdown();
            _countdownCts = new CancellationTokenSource();
            RunCountdownAsync(_countdownCts.Token).Forget();
        }

        public void Hide()
        {
            DisposeCountdown();
            DisposeResultRestart();
            _view?.RemoveEvents();
            _popupService.PopAll();
            _view = null;
        }

        public void Dispose()
        {
            Hide();
        }

        private async UniTask RunCountdownAsync(CancellationToken ct)
        {
            var remaining = (float)GameConst.UI.GameOverCountdownSeconds;

            while (remaining > 0f && !ct.IsCancellationRequested)
            {
                remaining -= Time.deltaTime;
                remaining = Mathf.Max(0f, remaining);
                _view?.SetCountdown(remaining / GameConst.UI.GameOverCountdownSeconds, Mathf.CeilToInt(remaining));
                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            if (!ct.IsCancellationRequested)
            {
                ClosePopup(true);
            }
        }

        private void OnAdClicked()
        {
            ClosePopup(false);
            _eventBus.Publish(new ReviveRequestedEvent());
        }

        private void OnCloseClicked()
        {
            ClosePopup(true);
        }

        private void ClosePopup(bool requestResultView)
        {
            DisposeCountdown();
            _view?.RemoveEvents();
            _popupService.Pop();
            _view = null;

            if (requestResultView)
            {
                _eventBus.Publish(new GameOverResultViewRequestedEvent());
                ScheduleResultRestart();
            }
        }

        private void ScheduleResultRestart()
        {
            DisposeResultRestart();
            _resultRestartCts = new CancellationTokenSource();
            RunResultRestartAsync(_resultRestartCts.Token).Forget();
        }

        private async UniTask RunResultRestartAsync(CancellationToken ct)
        {
            var isCanceled = await UniTask.Delay(
                TimeSpan.FromSeconds(GameConst.UI.GameOverResultViewRestartDelay),
                cancellationToken: ct).SuppressCancellationThrow();

            if (!isCanceled)
            {
                _eventBus.Publish(new RestartRequestedEvent());
            }
        }

        private void DisposeCountdown()
        {
            if (_countdownCts == null)
            {
                return;
            }

            _countdownCts.Cancel();
            _countdownCts.Dispose();
            _countdownCts = null;
        }

        private void DisposeResultRestart()
        {
            if (_resultRestartCts == null)
            {
                return;
            }

            _resultRestartCts.Cancel();
            _resultRestartCts.Dispose();
            _resultRestartCts = null;
        }
    }
}
