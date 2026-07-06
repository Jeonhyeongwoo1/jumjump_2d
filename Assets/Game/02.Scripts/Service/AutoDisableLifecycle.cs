using System;
using UnityEngine;

namespace JumJump.Service
{
    public sealed class AutoDisableLifecycle : MonoBehaviour
    {
        public float Duration => _duration;

        [SerializeField] private float _duration = 0.8f;

        private float _elapsed;
        private bool _isRunning;
        private Action _onExpired = () => { };
        private Action _onDisabled = () => { };

        public void Configure(float duration, Action onExpired, Action onDisabled)
        {
            SetDuration(duration);
            _onExpired = onExpired;
            _onDisabled = onDisabled;
        }

        public void SetDuration(float duration)
        {
            _duration = Mathf.Max(0.01f, duration);
        }

        public void Begin()
        {
            _elapsed = 0f;
            _isRunning = true;
        }

        public void Stop()
        {
            _elapsed = 0f;
            _isRunning = false;
        }

        private void Update()
        {
            if (!_isRunning)
            {
                return;
            }

            _elapsed += Time.deltaTime;
            if (_elapsed < _duration)
            {
                return;
            }

            _isRunning = false;
            _onExpired.Invoke();
        }

        private void OnDisable()
        {
            _isRunning = false;
            _elapsed = 0f;
            _onDisabled.Invoke();
        }
    }
}
