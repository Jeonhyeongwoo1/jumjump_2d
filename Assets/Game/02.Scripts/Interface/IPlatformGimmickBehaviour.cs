using JumJump.Controller;
using JumJump.Data;

namespace JumJump.Interface
{
    public interface IPlatformGimmickBehaviour
    {
        PlatformGimmickType Type { get; }
        bool RequiresTick { get; }

        void Reset(PlatformController platform);
        void Apply(PlatformController platform, PlatformGimmickSetting setting);
        void Tick(PlatformController platform, float deltaTime);
        void OnLanding(PlatformController platform);
    }
}
