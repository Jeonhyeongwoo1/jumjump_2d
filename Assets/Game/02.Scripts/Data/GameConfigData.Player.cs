using UnityEngine;

namespace JumJump.Data
{
    public sealed partial class GameConfigData
    {
        public float PlayerJumpDuration => _playerJumpDuration;
        public float PlayerJumpHeight => _playerJumpHeight;
        public float PlayerVerticalOffset => _playerVerticalOffset;
        public float PlayerLandingVerticalTolerance => _playerLandingVerticalTolerance;
        public float PlayerLandingEnabledNormalizedTime => _playerLandingEnabledNormalizedTime;
        public float PlayerContactHalfWidth => _playerContactHalfWidth;
        public float PlayerGameOverKnockbackHorizontalSpeed => _playerGameOverKnockbackHorizontalSpeed;
        public float PlayerGameOverKnockbackUpwardSpeed => _playerGameOverKnockbackUpwardSpeed;
        public float PlayerGameOverKnockbackGravityScale => _playerGameOverKnockbackGravityScale;

        [Header("Player")]
        [SerializeField] private float _playerJumpDuration = 0.48f;
        [SerializeField] private float _playerJumpHeight = 1.35f;
        [SerializeField] private float _playerVerticalOffset = 0.58f;
        [SerializeField] private float _playerLandingVerticalTolerance = 0.35f;
        [SerializeField] private float _playerLandingEnabledNormalizedTime = 0.5f;
        [SerializeField] private float _playerContactHalfWidth = 0.22f;

        [Header("Game Over Knockback")]
        [SerializeField] private float _playerGameOverKnockbackHorizontalSpeed = 3f;
        [SerializeField] private float _playerGameOverKnockbackUpwardSpeed = 2f;
        [SerializeField] private float _playerGameOverKnockbackGravityScale = 2.2f;
    }
}
