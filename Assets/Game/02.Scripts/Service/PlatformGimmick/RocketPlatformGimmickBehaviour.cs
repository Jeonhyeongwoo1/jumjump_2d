using JumJump.Controller;

namespace JumJump.Service.PlatformGimmick
{
    public sealed class RocketPlatformGimmickBehaviour : BasePlatformGimmickBehaviour
    {
        private readonly JumpBoxBoostFXService _boostFXService;

        public override PlatformGimmickType Type => PlatformGimmickType.Rocket;

        public RocketPlatformGimmickBehaviour(JumpBoxBoostFXService boostFXService)
        {
            _boostFXService = boostFXService;
        }

        public override void OnLanding(PlatformController platform, Player player)
        {
            _boostFXService.Play(platform.GetBoostFXPosition());
        }
    }
}
