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
        private Vector3 _baseScale;

        public void Show(string text, Vector3 worldPosition, bool isComboStyle, Action<UI_DynamicFont> onRelease)
        {
            _onRelease = onRelease;
            transform.position = worldPosition;
            transform.localScale = _baseScale * ResolveScale(isComboStyle);
            _text.text = text;
            _text.alpha = 1f;
            ApplyTextStyle(isComboStyle);

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

        private void ApplyTextStyle(bool isComboStyle)
        {
            _text.enableVertexGradient = isComboStyle;
            if (!isComboStyle)
            {
                _text.color = new Color32(
                    GameConst.DynamicFont.NormalScoreColorR,
                    GameConst.DynamicFont.NormalScoreColorG,
                    GameConst.DynamicFont.NormalScoreColorB,
                    byte.MaxValue);
                return;
            }

            _text.color = Color.white;
            _text.colorGradient = new VertexGradient(
                new Color32(
                    GameConst.DynamicFont.ComboGradientTopLeftR,
                    GameConst.DynamicFont.ComboGradientTopLeftG,
                    GameConst.DynamicFont.ComboGradientTopLeftB,
                    byte.MaxValue),
                new Color32(
                    GameConst.DynamicFont.ComboGradientTopRightR,
                    GameConst.DynamicFont.ComboGradientTopRightG,
                    GameConst.DynamicFont.ComboGradientTopRightB,
                    byte.MaxValue),
                new Color32(
                    GameConst.DynamicFont.ComboGradientBottomLeftR,
                    GameConst.DynamicFont.ComboGradientBottomLeftG,
                    GameConst.DynamicFont.ComboGradientBottomLeftB,
                    byte.MaxValue),
                new Color32(
                    GameConst.DynamicFont.ComboGradientBottomRightR,
                    GameConst.DynamicFont.ComboGradientBottomRightG,
                    GameConst.DynamicFont.ComboGradientBottomRightB,
                    byte.MaxValue));
        }

        private float ResolveScale(bool isComboStyle) =>
            isComboStyle ? GameConst.DynamicFont.ComboScale : 1f;

        private void Awake()
        {
            _baseScale = transform.localScale;
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}
