using JumJump.Interface;
using JumJump.Service;
using JumJump.Service.PlatformGimmick;
using VContainer;

namespace JumJump.Factory
{
    public sealed class PlatformGimmickBehaviourFactory
    {
        private const int GimmickTypeCount = (int)PlatformGimmickType.Rocket + 1;

        private readonly IPlatformGimmickBehaviour[] _behaviours;
        private readonly JumpBoxBoostFXService _jumpBoxBoostFXService;

        [Inject]
        public PlatformGimmickBehaviourFactory(JumpBoxBoostFXService jumpBoxBoostFXService)
        {
            _jumpBoxBoostFXService = jumpBoxBoostFXService;
            _behaviours = new IPlatformGimmickBehaviour[GimmickTypeCount];
            Register(new NormalPlatformGimmickBehaviour());
            Register(new SmallPlatformGimmickBehaviour());
            Register(new FastPlatformGimmickBehaviour());
            Register(new SlowPlatformGimmickBehaviour());
            Register(new SmallAndFastPlatformGimmickBehaviour());
            Register(new GhostPlatformGimmickBehaviour());
            Register(new RevealPlatformGimmickBehaviour());
            Register(new ShieldPlatformGimmickBehaviour());
            Register(new RocketPlatformGimmickBehaviour(_jumpBoxBoostFXService));
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
