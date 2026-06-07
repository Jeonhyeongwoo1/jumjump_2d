using UnityEngine;

namespace JumJump.Data
{
    [System.Serializable]
    public sealed class PlatformGimmickSetting
    {
        public PlatformGimmickType Type => _type;
        public int StartScore => _startScore;
        public float SpawnChance => _spawnChance;
        public float MinWidthScale => _minWidthScale;
        public float MaxWidthScale => _maxWidthScale;
        public float MinMoveSpeedScale => _minMoveSpeedScale;
        public float MaxMoveSpeedScale => _maxMoveSpeedScale;
        public float MinGhostFadeDuration => _minGhostFadeDuration;
        public float MaxGhostFadeDuration => _maxGhostFadeDuration;

        [SerializeField] private PlatformGimmickType _type;
        [SerializeField] private int _startScore;
        [SerializeField] private float _spawnChance = 1f;
        [SerializeField] private float _minWidthScale = 1f;
        [SerializeField] private float _maxWidthScale = 1f;
        [SerializeField] private float _minMoveSpeedScale = 1f;
        [SerializeField] private float _maxMoveSpeedScale = 1f;
        [SerializeField] private float _minGhostFadeDuration;
        [SerializeField] private float _maxGhostFadeDuration;

        public PlatformGimmickSetting()
        {
        }

        public PlatformGimmickSetting(
            PlatformGimmickType type,
            int startScore,
            float spawnChance,
            float minWidthScale,
            float maxWidthScale,
            float minMoveSpeedScale = 1f,
            float maxMoveSpeedScale = 1f,
            float minGhostFadeDuration = 0f,
            float maxGhostFadeDuration = 0f)
        {
            _type = type;
            _startScore = startScore;
            _spawnChance = spawnChance;
            _minWidthScale = minWidthScale;
            _maxWidthScale = maxWidthScale;
            _minMoveSpeedScale = minMoveSpeedScale;
            _maxMoveSpeedScale = maxMoveSpeedScale;
            _minGhostFadeDuration = minGhostFadeDuration;
            _maxGhostFadeDuration = maxGhostFadeDuration;
        }
    }
}
