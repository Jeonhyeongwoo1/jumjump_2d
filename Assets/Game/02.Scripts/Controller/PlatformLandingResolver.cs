using JumJump.Data;
using UnityEngine;

namespace JumJump.Controller
{
    internal static class PlatformLandingResolver
    {
        public static bool IsLandingPointInside(
            Vector3 platformPosition,
            float halfWidth,
            Vector3 characterPosition,
            float characterVerticalOffset,
            float verticalTolerance)
        {
            var landingY = platformPosition.y + characterVerticalOffset;

            if (Mathf.Abs(characterPosition.y - landingY) > verticalTolerance)
            {
                return false;
            }

            return characterPosition.x >= platformPosition.x - halfWidth &&
                   characterPosition.x <= platformPosition.x + halfWidth;
        }

        public static bool IsPlayerWithinContact(
            Player player,
            Vector3 platformPosition,
            float halfWidth,
            GameConfigData configData)
        {
            var contactHalfWidth = Mathf.Max(0f, halfWidth + configData.PlayerContactHalfWidth);
            return Mathf.Abs(player.Position.x - platformPosition.x) <= contactHalfWidth;
        }

        public static bool CanResolveLanding(Player player, float landingY)
        {
            return player.CanLand && player.IsDescending && HasCrossedLandingSurface(player, landingY);
        }

        public static bool ShouldResolveSideHit(Player player, float landingY)
        {
            return player.BottomY < landingY;
        }

        public static bool CanResolveStackedLanding(Player player, float landingY, GameConfigData configData)
        {
            if (!player.CanLand || !player.IsDescending)
            {
                return false;
            }

            return HasCrossedLandingSurface(player, landingY) ||
                   IsTouchingLandingSurface(player, landingY, configData);
        }

        public static bool ShouldResolveMissedPlayer(
            Player player,
            Vector3 platformPosition,
            float halfWidth,
            float landingY,
            GameConfigData configData)
        {
            if (!IsPlayerWithinContact(player, platformPosition, halfWidth, configData))
            {
                return false;
            }

            return player.IsJumping &&
                   player.IsDescending &&
                   player.BottomY < landingY - Mathf.Max(0f, configData.PlatformSideHitTopMargin);
        }

        public static Vector2 ResolveKnockbackDirection(Player player, Vector3 platformPosition, float moveDirectionX)
        {
            var directionX = moveDirectionX;
            if (Mathf.Approximately(directionX, 0f) && player != null)
            {
                directionX = Mathf.Sign(player.Position.x - platformPosition.x);
            }

            if (Mathf.Approximately(directionX, 0f))
            {
                directionX = 1f;
            }

            return new Vector2(directionX, 0f);
        }

        public static Player ResolvePlayerFromCollider(Collider2D other, Player player)
        {
            if (other == null || player == null)
            {
                return null;
            }

            if (other.attachedRigidbody != null && other.attachedRigidbody.transform == player.transform)
            {
                return player;
            }

            return other.transform == player.transform || other.transform.IsChildOf(player.transform) ? player : null;
        }

        private static bool HasCrossedLandingSurface(Player player, float landingY)
        {
            return player.PreviousBottomY >= landingY && player.BottomY <= landingY;
        }

        private static bool IsTouchingLandingSurface(Player player, float landingY, GameConfigData configData)
        {
            var snapTolerance = Mathf.Min(0.08f, Mathf.Max(0.01f, configData.PlayerLandingVerticalTolerance));
            return player.BottomY <= landingY + snapTolerance && player.Position.y >= landingY;
        }
    }
}
