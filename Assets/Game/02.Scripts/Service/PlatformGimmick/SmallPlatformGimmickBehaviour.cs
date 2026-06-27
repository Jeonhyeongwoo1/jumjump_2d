using JumJump.Controller;
using JumJump.Data;

namespace JumJump.Service.PlatformGimmick
{
    public sealed class SmallPlatformGimmickBehaviour : BasePlatformGimmickBehaviour
    {
        public override PlatformGimmickType Type => PlatformGimmickType.Small;

        public override void Apply(PlatformController platform, PlatformGimmickSetting setting)
        {
            platform.ApplyWidthScale(ResolveWidthScale(setting));
        }
    }
}
