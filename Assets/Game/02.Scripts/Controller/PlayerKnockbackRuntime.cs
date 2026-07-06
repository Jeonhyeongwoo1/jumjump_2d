namespace JumJump.Controller
{
    internal struct PlayerKnockbackRuntime
    {
        public bool IsFreezeLocked { get; private set; }

        public void Reset()
        {
            IsFreezeLocked = false;
        }

        public void Freeze()
        {
            IsFreezeLocked = true;
        }
    }
}
