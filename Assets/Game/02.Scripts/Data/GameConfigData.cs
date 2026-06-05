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
        public int InitialPlatformCount => _initialPlatformCount;
        public int PlatformsAhead => _platformsAhead;
        public int StationaryPlatformCount => _stationaryPlatformCount;
        public float PlatformVerticalSpacing => _platformVerticalSpacing;
        public float PlatformXRange => _platformXRange;
        public float PlatformWidth => _platformWidth;
        public float PlatformHeight => _platformHeight;
        public float PlatformBaseMoveSpeed => _platformBaseMoveSpeed;
        public float PlatformScoreSpeedMaxScore => _platformScoreSpeedMaxScore;
        public int PlatformXPatternMultiplier => _platformXPatternMultiplier;
        public int PlatformXPatternModulo => _platformXPatternModulo;
        public float PlatformLandingHeight => _platformLandingHeight;
        public float PlayerJumpDuration => _playerJumpDuration;
        public float PlayerJumpHeight => _playerJumpHeight;
        public float PlayerVerticalOffset => _playerVerticalOffset;
        public float PlayerLandingVerticalTolerance => _playerLandingVerticalTolerance;
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
        [SerializeField] private int _initialPlatformCount = 8;
        [SerializeField] private int _platformsAhead = 6;
        [SerializeField] private int _stationaryPlatformCount = 3;
        [SerializeField] private float _platformVerticalSpacing = 1.8f;
        [SerializeField] private float _platformXRange = 2.2f;
        [SerializeField] private float _platformWidth = 1.25f;
        [SerializeField] private float _platformHeight = 1f;
        [SerializeField] private float _platformBaseMoveSpeed = 0.45f;
        [SerializeField] private float _platformScoreSpeedMaxScore = 50f;
        [SerializeField] private int _platformXPatternMultiplier = 37;
        [SerializeField] private int _platformXPatternModulo = 100;
        [SerializeField] private float _platformLandingHeight = 0.48f;

        [Header("Player")]
        [SerializeField] private float _playerJumpDuration = 0.48f;
        [SerializeField] private float _playerJumpHeight = 1.35f;
        [SerializeField] private float _playerVerticalOffset = 0.58f;
        [SerializeField] private float _playerLandingVerticalTolerance = 0.35f;

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
