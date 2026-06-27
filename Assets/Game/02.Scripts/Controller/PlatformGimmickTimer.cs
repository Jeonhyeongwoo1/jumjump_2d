using UnityEngine;

namespace JumJump.Controller
{
    /// <summary>
    /// 발판 기믹(Ghost/Reveal 등)이 사용하는 진행 타이머. 상태만 보유하고
    /// 행동 판단은 각 Behaviour 가 담당한다. PlatformVisual / PlatformMotion 과
    /// 동일하게 PlatformController 의 보조 값 타입이다.
    /// </summary>
    internal struct PlatformGimmickTimer
    {
        public bool IsRunning => _isRunning;

        private float _duration;
        private float _elapsed;
        private bool _isRunning;

        public void Prepare(float duration)
        {
            _duration = Mathf.Max(0f, duration);
            _elapsed = 0f;
            _isRunning = false;
        }

        public void Start()
        {
            _elapsed = 0f;
            _isRunning = true;
        }

        public float Advance(float deltaTime)
        {
            if (_duration <= 0f)
            {
                return 1f;
            }

            _elapsed = Mathf.Min(_duration, _elapsed + Mathf.Max(0f, deltaTime));
            return Mathf.Clamp01(_elapsed / _duration);
        }

        public void Stop()
        {
            _isRunning = false;
        }

        public void Reset()
        {
            _duration = 0f;
            _elapsed = 0f;
            _isRunning = false;
        }
    }
}
