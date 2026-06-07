using JumJump.Controller;
using JumJump.Data;

namespace JumJump.Service.PlatformGimmick
{
    public sealed class GhostPlatformGimmickBehaviour : BasePlatformGimmickBehaviour
    {
        public override PlatformGimmickType Type => PlatformGimmickType.Ghost;
        public override bool RequiresTick => true;

        public override void Reset(PlatformController platform)
        {
            platform.RevealPlatformVisual();
        }

        public override void Apply(PlatformController platform, PlatformGimmickSetting setting)
        {
            platform.PrepareGimmickTimer(ResolveGhostFadeDuration(setting));
            platform.SetPlatformAlpha(1f);
            platform.EnableGimmickTick();
        }

        public override void Tick(PlatformController platform, float deltaTime)
        {
            if (!platform.IsGimmickTimerRunning)
            {
                if (!platform.IsFullyInGameCameraView())
                {
                    return;
                }

                platform.StartPreparedGimmickTimer();
                return;
            }

            var fadeRatio = platform.AdvanceGimmickTimer(deltaTime);
            platform.SetPlatformAlpha(1f - fadeRatio);

            if (!platform.IsGimmickTimerComplete)
            {
                return;
            }

            platform.SetPlatformAlpha(0f);
            platform.StopGimmickTick();
        }

        public override void OnLanding(PlatformController platform)
        {
            platform.RevealPlatformVisual();
        }
    }
}
