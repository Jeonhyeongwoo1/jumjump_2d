using UnityEngine;

namespace JumJump.Data
{
    public sealed partial class GameConfigData
    {
        public float PlatformSpawnDistance => _platformSpawnDistance;
        public int PlatformFirstSpawnDirection => _platformFirstSpawnDirection;
        public bool PlatformAlternatesSpawnSide => _platformAlternatesSpawnSide;
        public int PlatformRandomSpawnSideStartCount => _platformRandomSpawnSideStartCount;
        public float PlatformVerticalStep => _platformVerticalStep;
        public float PlatformWidth => _platformWidth;
        public float PlatformHeight => _platformHeight;
        public PlatformGimmickSetting[] PlatformGimmickSettings => _platformGimmickSettings;
        public float PlatformBaseMoveSpeed => _platformBaseMoveSpeed;
        public float PlatformBaseMoveSpeedMinScale => _platformBaseMoveSpeedMinScale;
        public float PlatformBaseMoveSpeedMaxScale => _platformBaseMoveSpeedMaxScale;
        public float PlatformMaxMoveSpeedBonus => _platformMaxMoveSpeedBonus;
        public float PlatformScoreSpeedMaxScore => _platformScoreSpeedMaxScore;
        public float PlatformLandingHeight => _platformLandingHeight;
        public float PlatformStackVerticalOffset => _platformStackVerticalOffset;
        public float PlatformDoublePreSpawnDelay => _platformDoublePreSpawnDelay;
        public float PlatformDoubleActivationDelay => _platformDoubleActivationDelay;
        public float PlatformDoublePreviewAlpha => _platformDoublePreviewAlpha;
        public float PlatformDoubleFollowUpMoveSpeedScale => _platformDoubleFollowUpMoveSpeedScale;
        public float PlatformSideHitTopMargin => _platformSideHitTopMargin;
        public float PlatformCleanupBelowDistance => _platformCleanupBelowDistance;

        [Header("Platform")]
        [SerializeField] private float _platformSpawnDistance = 2.2f;
        [SerializeField] private int _platformFirstSpawnDirection = 1;
        [SerializeField] private bool _platformAlternatesSpawnSide = true;
        [SerializeField] private int _platformRandomSpawnSideStartCount = 10;
        [SerializeField] private float _platformVerticalStep = 1f;
        [SerializeField] private float _platformWidth = 1.25f;
        [SerializeField] private float _platformHeight = 1f;
        [SerializeField] private PlatformGimmickSetting[] _platformGimmickSettings =
        {
            new PlatformGimmickSetting(PlatformGimmickType.Normal, 0, 1f, 1f, 1f),
            new PlatformGimmickSetting(PlatformGimmickType.Small, 10, 0.16f, 0.7f, 0.7f),
            new PlatformGimmickSetting(PlatformGimmickType.Fast, 10, 0.16f, 1f, 1f, 1.3f, 1.3f),
            new PlatformGimmickSetting(PlatformGimmickType.Slow, 10, 0.16f, 1f, 1f, 0.85f, 0.85f),
            new PlatformGimmickSetting(PlatformGimmickType.SmallAndFast, 10, 0.16f, 0.7f, 0.7f, 1.3f, 1.3f),
            new PlatformGimmickSetting(PlatformGimmickType.Ghost, 10, 0.16f, 1f, 1f, 1f, 1f, 1.2f, 1.6f),
            new PlatformGimmickSetting(PlatformGimmickType.Double, 20, 0.1f, 1f, 1f),
            new PlatformGimmickSetting(PlatformGimmickType.Reveal, 15, 0.16f, 1f, 1f, 1f, 1f, 0.08f, 0.18f)
        };
        [SerializeField] private float _platformBaseMoveSpeed = 0.45f;
        [SerializeField] private float _platformBaseMoveSpeedMinScale = 1f;
        [SerializeField] private float _platformBaseMoveSpeedMaxScale = 1.15f;
        [SerializeField] private float _platformMaxMoveSpeedBonus = 1f;
        [SerializeField] private float _platformScoreSpeedMaxScore = 50f;
        [SerializeField] private float _platformLandingHeight = 0.48f;
        [SerializeField] private float _platformStackVerticalOffset = 0.02f;
        [SerializeField] private float _platformDoublePreSpawnDelay = 0.25f;
        [SerializeField] private float _platformDoubleActivationDelay = 0.18f;
        [SerializeField] private float _platformDoublePreviewAlpha = 0.35f;
        [SerializeField] private float _platformDoubleFollowUpMoveSpeedScale = 0.8f;
        [SerializeField] private float _platformSideHitTopMargin = 0.35f;
        [SerializeField] private float _platformCleanupBelowDistance = 2f;
    }
}
