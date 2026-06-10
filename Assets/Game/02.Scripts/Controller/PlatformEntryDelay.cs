namespace JumJump.Controller
{
    internal struct PlatformEntryDelay
    {
        private bool _isWaiting;
        private bool _hideWhileWaiting;
        private float _elapsed;
        private float _duration;

        public bool IsWaiting => _isWaiting;
        public bool HideWhileWaiting => _hideWhileWaiting;

        public void Reset()
        {
            _isWaiting = false;
            _hideWhileWaiting = false;
            _elapsed = 0f;
            _duration = 0f;
        }

        public bool Start(float duration, bool hideWhileWaiting)
        {
            if (duration <= 0f)
            {
                Reset();
                return false;
            }

            _isWaiting = true;
            _hideWhileWaiting = hideWhileWaiting;
            _elapsed = 0f;
            _duration = duration;
            return true;
        }

        public bool Tick(float deltaTime)
        {
            if (!_isWaiting)
            {
                return false;
            }

            _elapsed += deltaTime;
            if (_elapsed < _duration)
            {
                return true;
            }

            Reset();
            return false;
        }
    }
}
