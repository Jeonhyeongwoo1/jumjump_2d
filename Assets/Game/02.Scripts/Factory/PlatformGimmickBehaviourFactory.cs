using JumJump.Interface;
using JumJump.Service.PlatformGimmick;

namespace JumJump.Factory
{
    public sealed class PlatformGimmickBehaviourFactory
    {
        private const int GimmickTypeCount = (int)PlatformGimmickType.Ghost + 1;

        private readonly IPlatformGimmickBehaviour[] _behaviours;

        public PlatformGimmickBehaviourFactory()
        {
            _behaviours = new IPlatformGimmickBehaviour[GimmickTypeCount];
            Register(new NormalPlatformGimmickBehaviour());
            Register(new SmallPlatformGimmickBehaviour());
            Register(new FastPlatformGimmickBehaviour());
            Register(new SlowPlatformGimmickBehaviour());
            Register(new SmallAndFastPlatformGimmickBehaviour());
            Register(new GhostPlatformGimmickBehaviour());
        }

        public IPlatformGimmickBehaviour Get(PlatformGimmickType type)
        {
            var index = (int)type;
            if (index < 0 || index >= _behaviours.Length)
            {
                return _behaviours[(int)PlatformGimmickType.Normal];
            }

            return _behaviours[index] ?? _behaviours[(int)PlatformGimmickType.Normal];
        }

        private void Register(IPlatformGimmickBehaviour behaviour)
        {
            _behaviours[(int)behaviour.Type] = behaviour;
        }
    }
}
