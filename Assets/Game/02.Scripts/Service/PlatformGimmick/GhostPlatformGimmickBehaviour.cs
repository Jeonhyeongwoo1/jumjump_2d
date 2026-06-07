using JumJump.Controller;
using JumJump.Data;
using UnityEngine;

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
            }

            var fadeRatio = platform.HasReachedMoveTarget ? 1f : platform.AdvanceGimmickTimer(deltaTime);
            var alpha = Mathf.Lerp(1f, 0f, fadeRatio);
            platform.SetPlatformAlpha(alpha);

            if (fadeRatio < 1f)
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
