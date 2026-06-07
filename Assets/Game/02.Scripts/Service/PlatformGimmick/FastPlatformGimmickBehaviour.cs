using JumJump.Controller;
using JumJump.Data;

namespace JumJump.Service.PlatformGimmick
{
    public sealed class FastPlatformGimmickBehaviour : BasePlatformGimmickBehaviour
    {
        public override PlatformGimmickType Type => PlatformGimmickType.Fast;

        public override void Apply(PlatformController platform, PlatformGimmickSetting setting)
        {
            platform.ApplyMoveSpeedScale(ResolveMoveSpeedScale(setting));
        }
    }
}
