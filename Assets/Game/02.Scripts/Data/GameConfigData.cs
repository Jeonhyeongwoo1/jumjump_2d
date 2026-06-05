using UnityEngine;

namespace JumJump.Data
{
    [CreateAssetMenu(fileName = nameof(GameConfigData), menuName = "JumJump/Game Config Data")]
    public sealed class GameConfigData : ScriptableObject
    {
        public string PreLoadLabel => _preLoadLabel;
        public string PlayerAddressableKey => _playerAddressableKey;
        public string PlayerPoolKey => _playerPoolKey;
        public int PlayerPrewarmCount => _playerPrewarmCount;
        public string PlatformAddressableKey => _platformAddressableKey;
        public string PlatformPoolKey => _platformPoolKey;
        public int PlatformPrewarmCount => _platformPrewarmCount;
        public float PlatformSpawnDistance => _platformSpawnDistance;
        public int PlatformFirstSpawnDirection => _platformFirstSpawnDirection;
        public bool PlatformAlternatesSpawnSide => _platformAlternatesSpawnSide;
        public float PlatformVerticalStep => _platformVerticalStep;
        public float PlatformWidth => _platformWidth;
        public float PlatformHeight => _platformHeight;
        public float PlatformBaseMoveSpeed => _platformBaseMoveSpeed;
        public float PlatformMaxMoveSpeedBonus => _platformMaxMoveSpeedBonus;
        public float PlatformScoreSpeedMaxScore => _platformScoreSpeedMaxScore;
        public float PlatformLandingHeight => _platformLandingHeight;
        public float PlatformSideHitTopMargin => _platformSideHitTopMargin;
        public float PlatformCleanupBelowDistance => _platformCleanupBelowDistance;
        public float PlayerJumpDuration => _playerJumpDuration;
        public float PlayerJumpHeight => _playerJumpHeight;
        public float PlayerVerticalOffset => _playerVerticalOffset;
        public float PlayerLandingVerticalTolerance => _playerLandingVerticalTolerance;
        public float PlayerLandingEnabledNormalizedTime => _playerLandingEnabledNormalizedTime;
        public float PlayerContactHalfWidth => _playerContactHalfWidth;
        public int ScorePerLanding => _scorePerLanding;
        public string HighScoreKey => _highScoreKey;
        public string TapActionPath => _tapActionPath;
        public float CameraMinimumY => _cameraMinimumY;
        public float CameraSmoothSpeed => _cameraSmoothSpeed;
        public float CameraVerticalOffset => _cameraVerticalOffset;

        [Header("Addressables")]
        [SerializeField] private string _preLoadLabel = "PreLoad";
        [SerializeField] private string _playerAddressableKey = "Player";
        [SerializeField] private string _platformAddressableKey = "Platform";

        [Header("Pooling")]
        [SerializeField] private string _playerPoolKey = "Player";
        [SerializeField] private int _playerPrewarmCount = 1;
        [SerializeField] private string _platformPoolKey = "Platform";
        [SerializeField] private int _platformPrewarmCount = 12;

        [Header("Platform")]
        [SerializeField] private float _platformSpawnDistance = 2.2f;
        [SerializeField] private int _platformFirstSpawnDirection = 1;
        [SerializeField] private bool _platformAlternatesSpawnSide = true;
        [SerializeField] private float _platformVerticalStep = 1f;
        [SerializeField] private float _platformWidth = 1.25f;
        [SerializeField] private float _platformHeight = 1f;
        [SerializeField] private float _platformBaseMoveSpeed = 0.45f;
        [SerializeField] private float _platformMaxMoveSpeedBonus = 1f;
        [SerializeField] private float _platformScoreSpeedMaxScore = 50f;
        [SerializeField] private float _platformLandingHeight = 0.48f;
        [SerializeField] private float _platformSideHitTopMargin = 0.35f;
        [SerializeField] private float _platformCleanupBelowDistance = 2f;

        [Header("Player")]
        [SerializeField] private float _playerJumpDuration = 0.48f;
        [SerializeField] private float _playerJumpHeight = 1.35f;
        [SerializeField] private float _playerVerticalOffset = 0.58f;
        [SerializeField] private float _playerLandingVerticalTolerance = 0.35f;
        [SerializeField] private float _playerLandingEnabledNormalizedTime = 0.5f;
        [SerializeField] private float _playerContactHalfWidth = 0.22f;

        [Header("Score")]
        [SerializeField] private int _scorePerLanding = 1;

        [Header("Persistence")]
        [SerializeField] private string _highScoreKey = "JumJump.HighScore";

        [Header("Input")]
        [SerializeField] private string _tapActionPath = "Player/Attack";

        [Header("Camera")]
        [SerializeField] private float _cameraMinimumY;
        [SerializeField] private float _cameraSmoothSpeed = 8f;
        [SerializeField] private float _cameraVerticalOffset = 1.2f;
    }
}
