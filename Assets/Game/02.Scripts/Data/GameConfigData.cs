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
        public PlatformGimmickSetting[] PlatformGimmickSettings => _platformGimmickSettings;
        public float PlatformBaseMoveSpeed => _platformBaseMoveSpeed;
        public float PlatformMaxMoveSpeedBonus => _platformMaxMoveSpeedBonus;
        public float PlatformScoreSpeedMaxScore => _platformScoreSpeedMaxScore;
        public float PlatformLandingHeight => _platformLandingHeight;
        public float PlatformStackVerticalOffset => _platformStackVerticalOffset;
        public float PlatformSideHitTopMargin => _platformSideHitTopMargin;
        public float PlatformCleanupBelowDistance => _platformCleanupBelowDistance;
        public float PlayerJumpDuration => _playerJumpDuration;
        public float PlayerJumpHeight => _playerJumpHeight;
        public float PlayerVerticalOffset => _playerVerticalOffset;
        public float PlayerLandingVerticalTolerance => _playerLandingVerticalTolerance;
        public float PlayerLandingEnabledNormalizedTime => _playerLandingEnabledNormalizedTime;
        public float PlayerContactHalfWidth => _playerContactHalfWidth;
        public float PlayerGameOverKnockbackHorizontalSpeed => _playerGameOverKnockbackHorizontalSpeed;
        public float PlayerGameOverKnockbackUpwardSpeed => _playerGameOverKnockbackUpwardSpeed;
        public float PlayerGameOverKnockbackGravityScale => _playerGameOverKnockbackGravityScale;
        public int ScorePerLanding => _scorePerLanding;
        public string HighScoreKey => _highScoreKey;
        public string TapActionPath => _tapActionPath;
        public float CameraMinimumY => _cameraMinimumY;
        public float CameraSmoothSpeed => _cameraSmoothSpeed;
        public float CameraVerticalOffset => _cameraVerticalOffset;
        public float BackgroundGradientMinHeight => _backgroundGradientMinHeight;
        public float BackgroundGradientMaxHeight => _backgroundGradientMaxHeight;
        public Color BackgroundLowBottomColor => _backgroundLowBottomColor;
        public Color BackgroundLowTopColor => _backgroundLowTopColor;
        public Color BackgroundHighBottomColor => _backgroundHighBottomColor;
        public Color BackgroundHighTopColor => _backgroundHighTopColor;
        public Color BackgroundHighObjectTint => _backgroundHighObjectTint;
        public float BackgroundGradientUpdateThreshold => _backgroundGradientUpdateThreshold;
        public float BackgroundGradientScreenPadding => _backgroundGradientScreenPadding;
        public float BackgroundGradientZ => _backgroundGradientZ;
        public int BackgroundGradientSortingOrder => _backgroundGradientSortingOrder;
        public int BackgroundObjectSortingOrder => _backgroundObjectSortingOrder;
        public int BackgroundObjectPoolCount => _backgroundObjectPoolCount;
        public float BackgroundObjectXRange => _backgroundObjectXRange;
        public float BackgroundObjectRecycleBelowY => _backgroundObjectRecycleBelowY;
        public float BackgroundObjectSpawnAheadY => _backgroundObjectSpawnAheadY;
        public float BackgroundObjectMinScale => _backgroundObjectMinScale;
        public float BackgroundObjectMaxScale => _backgroundObjectMaxScale;
        public float BackgroundObjectMinAlpha => _backgroundObjectMinAlpha;
        public float BackgroundObjectMaxAlpha => _backgroundObjectMaxAlpha;
        public float BackgroundObjectMaxRotation => _backgroundObjectMaxRotation;
        public float BackgroundObjectDriftAmplitude => _backgroundObjectDriftAmplitude;
        public float BackgroundObjectMinDriftSpeed => _backgroundObjectMinDriftSpeed;
        public float BackgroundObjectMaxDriftSpeed => _backgroundObjectMaxDriftSpeed;
        public int BackgroundObjectAlphaFromDepthIndex => _backgroundObjectAlphaFromDepthIndex;
        public float BackgroundObjectMinParallax => _backgroundObjectMinParallax;
        public float BackgroundObjectMaxParallax => _backgroundObjectMaxParallax;
        public float BackgroundObjectZ => _backgroundObjectZ;
        public BackgroundDepthLayer[] BackgroundDepthLayers => _backgroundDepthLayers;

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
        [SerializeField] private PlatformGimmickSetting[] _platformGimmickSettings =
        {
            new PlatformGimmickSetting(PlatformGimmickType.Normal, 0, 1f, 1f, 1f),
            new PlatformGimmickSetting(PlatformGimmickType.Small, 10, 0.2f, 0.7f, 0.7f),
            new PlatformGimmickSetting(PlatformGimmickType.Fast, 10, 0.2f, 1f, 1f, 1.3f, 1.3f),
            new PlatformGimmickSetting(PlatformGimmickType.Slow, 10, 0.2f, 1f, 1f, 0.85f, 0.85f),
            new PlatformGimmickSetting(PlatformGimmickType.SmallAndFast, 10, 0.2f, 0.7f, 0.7f, 1.3f, 1.3f)
        };
        [SerializeField] private float _platformBaseMoveSpeed = 0.45f;
        [SerializeField] private float _platformMaxMoveSpeedBonus = 1f;
        [SerializeField] private float _platformScoreSpeedMaxScore = 50f;
        [SerializeField] private float _platformLandingHeight = 0.48f;
        [SerializeField] private float _platformStackVerticalOffset = 0.02f;
        [SerializeField] private float _platformSideHitTopMargin = 0.35f;
        [SerializeField] private float _platformCleanupBelowDistance = 2f;

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

        [Header("Background Gradient")]
        [SerializeField] private float _backgroundGradientMinHeight = -2f;
        [SerializeField] private float _backgroundGradientMaxHeight = 80f;
        [SerializeField] private Color _backgroundLowBottomColor = new Color(0.47f, 0.82f, 1f, 1f);
        [SerializeField] private Color _backgroundLowTopColor = new Color(0.18f, 0.55f, 0.95f, 1f);
        [SerializeField] private Color _backgroundHighBottomColor = new Color(0.34f, 0.22f, 0.75f, 1f);
        [SerializeField] private Color _backgroundHighTopColor = new Color(0.1f, 0.05f, 0.32f, 1f);
        [SerializeField] private Color _backgroundHighObjectTint = new Color(0.86f, 0.78f, 1f, 1f);
        [SerializeField] private float _backgroundGradientUpdateThreshold = 0.005f;
        [SerializeField] private float _backgroundGradientScreenPadding = 1.15f;
        [SerializeField] private float _backgroundGradientZ = 10f;
        [SerializeField] private int _backgroundGradientSortingOrder = -1000;

        [Header("Background Objects")]
        [SerializeField] private int _backgroundObjectSortingOrder = -900;
        [SerializeField] private int _backgroundObjectPoolCount = 12;
        [SerializeField] private float _backgroundObjectXRange = 3.2f;
        [SerializeField] private float _backgroundObjectRecycleBelowY = 8f;
        [SerializeField] private float _backgroundObjectSpawnAheadY = 11f;
        [SerializeField] private float _backgroundObjectMinScale = 0.35f;
        [SerializeField] private float _backgroundObjectMaxScale = 1.3f;
        [SerializeField] private float _backgroundObjectMinAlpha = 0.3f;
        [SerializeField] private float _backgroundObjectMaxAlpha = 0.7f;
        [SerializeField] private float _backgroundObjectMaxRotation = 30f;
        [SerializeField] private float _backgroundObjectDriftAmplitude = 0.4f;
        [SerializeField] private float _backgroundObjectMinDriftSpeed = 0.3f;
        [SerializeField] private float _backgroundObjectMaxDriftSpeed = 0.8f;
        [SerializeField] private int _backgroundObjectAlphaFromDepthIndex = 1;
        [SerializeField] private float _backgroundObjectMinParallax = 0.3f;
        [SerializeField] private float _backgroundObjectMaxParallax = 0.75f;
        [SerializeField] private float _backgroundObjectZ = 9f;
        [SerializeField] private BackgroundDepthLayer[] _backgroundDepthLayers;
    }
}
