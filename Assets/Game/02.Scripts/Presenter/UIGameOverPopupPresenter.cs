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
                ClosePopup();
            }
        }

        private void OnAdClicked()
        {
            ClosePopup();
            _eventBus.Publish(new RestartRequestedEvent());
        }

        private void OnCloseClicked()
        {
            ClosePopup();
        }

        private void ClosePopup()
        {
            DisposeCountdown();
            _view?.RemoveEvents();
            _popupService.Pop();
            _view = null;
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
    }
}
