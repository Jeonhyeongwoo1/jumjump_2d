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

        [SerializeField] private PlatformGimmickType _type;
        [SerializeField] private int _startScore;
        [SerializeField] private float _spawnChance = 1f;
        [SerializeField] private float _minWidthScale = 1f;
        [SerializeField] private float _maxWidthScale = 1f;

        public PlatformGimmickSetting()
        {
        }

        public PlatformGimmickSetting(
            PlatformGimmickType type,
            int startScore,
            float spawnChance,
            float minWidthScale,
            float maxWidthScale)
        {
            _type = type;
            _startScore = startScore;
            _spawnChance = spawnChance;
            _minWidthScale = minWidthScale;
            _maxWidthScale = maxWidthScale;
        }
    }
}
