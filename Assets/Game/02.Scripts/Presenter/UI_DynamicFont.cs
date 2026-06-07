using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using JumJump.Util;
using TMPro;
using UnityEngine;

namespace JumJump.Presenter
{
    public sealed class UI_DynamicFont : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;

        private Action<UI_DynamicFont> _onRelease;
        private CancellationTokenSource _cts;

        public void Show(string text, Vector3 worldPosition, Action<UI_DynamicFont> onRelease)
        {
            _onRelease = onRelease;
            transform.position = worldPosition;
            _text.text = text;
            _text.alpha = 1f;

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            AnimateAsync(_cts.Token).Forget();
        }

        private async UniTask AnimateAsync(CancellationToken ct)
        {
            var startPos = transform.position;
            var elapsed = 0f;

            while (elapsed < GameConst.DynamicFont.AnimationDuration && !ct.IsCancellationRequested)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / GameConst.DynamicFont.AnimationDuration);
                transform.position = startPos + Vector3.up * (GameConst.DynamicFont.RiseHeight * t);
                _text.alpha = 1f - t;
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            if (!ct.IsCancellationRequested)
            {
                _onRelease?.Invoke(this);
            }
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}
