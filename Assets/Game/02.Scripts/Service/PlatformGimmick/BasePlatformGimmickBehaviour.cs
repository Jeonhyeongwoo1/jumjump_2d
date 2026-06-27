using JumJump.Controller;
using JumJump.Data;
using JumJump.Interface;
using UnityEngine;

namespace JumJump.Service.PlatformGimmick
{
    public abstract class BasePlatformGimmickBehaviour : IPlatformGimmickBehaviour
    {
        public abstract PlatformGimmickType Type { get; }
        public virtual bool RequiresTick => false;

        public virtual void Reset(PlatformController platform)
        {
        }

        public virtual void Apply(PlatformController platform, PlatformGimmickSetting setting)
        {
        }

        public virtual void Tick(PlatformController platform, float deltaTime)
        {
        }

        public virtual void OnLanding(PlatformController platform, Player player)
        {
        }

        protected float ResolveWidthScale(PlatformGimmickSetting setting)
        {
            if (setting == null)
            {
                return 1f;
            }

            var minWidthScale = Mathf.Clamp01(setting.MinWidthScale);
            var maxWidthScale = Mathf.Clamp01(setting.MaxWidthScale);
            if (maxWidthScale < minWidthScale)
            {
                maxWidthScale = minWidthScale;
            }

            return Mathf.Max(0.01f, UnityEngine.Random.Range(minWidthScale, maxWidthScale));
        }

        protected float ResolveMoveSpeedScale(PlatformGimmickSetting setting)
        {
            if (setting == null)
            {
                return 1f;
            }

            var minMoveSpeedScale = Mathf.Max(0f, setting.MinMoveSpeedScale);
            var maxMoveSpeedScale = Mathf.Max(0f, setting.MaxMoveSpeedScale);
            if (maxMoveSpeedScale < minMoveSpeedScale)
            {
                maxMoveSpeedScale = minMoveSpeedScale;
            }

            return Mathf.Max(0f, UnityEngine.Random.Range(minMoveSpeedScale, maxMoveSpeedScale));
        }

        protected float ResolveGhostFadeDuration(PlatformGimmickSetting setting)
        {
            if (setting == null)
            {
                return 0f;
            }

            var minGhostFadeDuration = Mathf.Max(0f, setting.MinGhostFadeDuration);
            var maxGhostFadeDuration = Mathf.Max(0f, setting.MaxGhostFadeDuration);
            if (maxGhostFadeDuration < minGhostFadeDuration)
            {
                maxGhostFadeDuration = minGhostFadeDuration;
            }

            return maxGhostFadeDuration <= 0f
                ? 0f
                : UnityEngine.Random.Range(minGhostFadeDuration, maxGhostFadeDuration);
        }
    }
}
