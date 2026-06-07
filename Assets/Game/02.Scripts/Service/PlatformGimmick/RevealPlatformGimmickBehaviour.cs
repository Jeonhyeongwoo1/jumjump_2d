using JumJump.Controller;
using JumJump.Data;
using UnityEngine;

namespace JumJump.Service.PlatformGimmick
{
    public sealed class RevealPlatformGimmickBehaviour : BasePlatformGimmickBehaviour
    {
        private const float RevealStartVisibleRatio = 0.67f;

        public override PlatformGimmickType Type => PlatformGimmickType.Reveal;
        public override bool RequiresTick => true;

        public override void Reset(PlatformController platform)
        {
            platform.RevealPlatformVisual();
            platform.SetInteractionEnabled(true);
        }

        public override void Apply(PlatformController platform, PlatformGimmickSetting setting)
        {
            platform.PrepareGimmickTimer(ResolveGhostFadeDuration(setting));
            platform.SetPlatformAlpha(0f);
            platform.SetInteractionEnabled(true);
            platform.EnableGimmickTick();
        }

        public override void Tick(PlatformController platform, float deltaTime)
        {
            if (!platform.IsGimmickTimerRunning)
            {
                if (!platform.IsMostlyInGameCameraView(RevealStartVisibleRatio))
                {
                    return;
                }

                platform.StartPreparedGimmickTimer();
            }

            var revealRatio = platform.HasReachedMoveTarget ? 1f : platform.AdvanceGimmickTimer(deltaTime);
            var alpha = Mathf.Lerp(0f, 1f, revealRatio);
            platform.SetPlatformAlpha(alpha);

            if (revealRatio < 1f)
            {
                return;
            }

            platform.SetPlatformAlpha(1f);
            platform.SetInteractionEnabled(true);
            platform.StopGimmickTick();
        }

        public override void OnLanding(PlatformController platform)
        {
            platform.RevealPlatformVisual();
            platform.SetInteractionEnabled(true);
        }
    }
}
