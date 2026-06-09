using JumJump.Controller;

namespace JumJump.Service.PlatformGimmick
{
    public sealed class ShieldPlatformGimmickBehaviour : BasePlatformGimmickBehaviour
    {
        public override PlatformGimmickType Type => PlatformGimmickType.Shield;

        public override void OnLanding(PlatformController platform, Player player)
        {
            player.GrantShield();
        }
    }
}
