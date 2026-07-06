using UnityEngine;

namespace JumJump.Data
{
    [CreateAssetMenu(fileName = nameof(PlayerConfigData), menuName = "JumJump/Player Config Data")]
    public sealed class PlayerConfigData : ScriptableObject
    {
        public float PlayerJumpDuration => _playerJumpDuration;
        public float PlayerJumpHeight => _playerJumpHeight;
        public float PlayerVerticalOffset => _playerVerticalOffset;
        public float PlayerLandingVerticalTolerance => _playerLandingVerticalTolerance;
        public float PlayerLandingEnabledNormalizedTime => _playerLandingEnabledNormalizedTime;
        public float PlayerJumpBufferDuration => _playerJumpBufferDuration;
        public float PlayerLandingSinkOffset => _playerLandingSinkOffset;
        public float PlayerLandingSinkDuration => _playerLandingSinkDuration;
        public float PlayerContactHalfWidth => _playerContactHalfWidth;
        public float PlayerGameOverKnockbackHorizontalSpeed => _playerGameOverKnockbackHorizontalSpeed;
        public float PlayerGameOverKnockbackUpwardSpeed => _playerGameOverKnockbackUpwardSpeed;
        public float PlayerGameOverKnockbackGravityScale => _playerGameOverKnockbackGravityScale;
        public float PlayerGameOverPopupDelay => _playerGameOverPopupDelay;
        public float PlayerRocketBoostDuration => _playerRocketBoostDuration;
        public float PlayerRocketDropHeight => _playerRocketDropHeight;
        public float PlayerRocketDropDownwardSpeed => _playerRocketDropDownwardSpeed;

        [Header("Player")]
        [SerializeField] private float _playerJumpDuration = 0.48f;
        [SerializeField] private float _playerJumpHeight = 1.35f;
        [SerializeField] private float _playerVerticalOffset = 0.58f;
        [SerializeField] private float _playerLandingVerticalTolerance = 0.35f;
        [SerializeField] private float _playerLandingEnabledNormalizedTime = 0.5f;
        [SerializeField] private float _playerJumpBufferDuration = 0.12f;
        [SerializeField] private float _playerLandingSinkOffset = 0.16f;
        [SerializeField] private float _playerLandingSinkDuration = 0.18f;
        [SerializeField] private float _playerContactHalfWidth = 0.22f;

        [Header("Game Over Knockback")]
        [SerializeField] private float _playerGameOverKnockbackHorizontalSpeed = 3f;
        [SerializeField] private float _playerGameOverKnockbackUpwardSpeed = 2f;
        [SerializeField] private float _playerGameOverKnockbackGravityScale = 2.2f;
        [SerializeField] private float _playerGameOverPopupDelay = 1f;

        [Header("Rocket")]
        [SerializeField] private float _playerRocketBoostDuration = 0.75f;
        [SerializeField] private float _playerRocketDropHeight = 1.2f;
        [SerializeField] private float _playerRocketDropDownwardSpeed = 0.2f;
    }
}
