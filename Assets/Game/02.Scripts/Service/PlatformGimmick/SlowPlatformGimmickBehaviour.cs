using JumJump.Controller;
using JumJump.Data;

namespace JumJump.Service.PlatformGimmick
{
    public sealed class SlowPlatformGimmickBehaviour : BasePlatformGimmickBehaviour
    {
        public override PlatformGimmickType Type => PlatformGimmickType.Slow;

        public override void Apply(PlatformController platform, PlatformGimmickSetting setting)
        {
            platform.ApplyMoveSpeedScale(ResolveMoveSpeedScale(setting));
        }
    }
}
