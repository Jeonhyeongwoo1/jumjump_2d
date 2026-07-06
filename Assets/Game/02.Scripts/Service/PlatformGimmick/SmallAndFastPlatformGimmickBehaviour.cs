using JumJump.Controller;
using JumJump.Data;

namespace JumJump.Service.PlatformGimmick
{
    public sealed class SmallAndFastPlatformGimmickBehaviour : BasePlatformGimmickBehaviour
    {
        public override PlatformGimmickType Type => PlatformGimmickType.SmallAndFast;

        public override void Apply(PlatformController platform, PlatformGimmickSetting setting)
        {
            platform.ApplyWidthScale(ResolveWidthScale(setting));
            platform.ApplyMoveSpeedScale(ResolveMoveSpeedScale(setting));
        }
    }
}
