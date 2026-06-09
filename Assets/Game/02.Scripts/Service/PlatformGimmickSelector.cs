using JumJump.Data;
using UnityEngine;

namespace JumJump.Service
{
    public sealed class PlatformGimmickSelector
    {
        private readonly GameConfigData _configData;
        private readonly PlatformCheatData _platformCheatData;
        private readonly ScoreService _scoreService;

        public PlatformGimmickSelector(
            GameConfigData configData,
            PlatformCheatData platformCheatData,
            ScoreService scoreService)
        {
            _configData = configData;
            _platformCheatData = platformCheatData;
            _scoreService = scoreService;
        }

        public PlatformGimmickSetting ResolvePrimary()
        {
            var settings = _configData.PlatformGimmickSettings;
            var normalSetting = Find(PlatformGimmickType.Normal);
            if (_platformCheatData != null && _platformCheatData.ForcePlatformGimmick)
            {
                var forcedSetting = Find(_platformCheatData.ForcedPlatformGimmickType);
                return forcedSetting ?? normalSetting;
            }

            if (settings == null || settings.Length == 0)
            {
                return normalSetting;
            }

            var totalChance = 0f;
            for (var i = 0; i < settings.Length; i++)
            {
                var setting = settings[i];
                if (!CanSpawnAsRandomGimmick(setting))
                {
                    continue;
                }

                totalChance += Mathf.Clamp01(setting.SpawnChance);
            }

            if (totalChance <= 0f)
            {
                return normalSetting;
            }

            var roll = totalChance <= 1f
                ? Random.value
                : Random.Range(0f, totalChance);
            var cumulativeChance = 0f;
            for (var i = 0; i < settings.Length; i++)
            {
                var setting = settings[i];
                if (!CanSpawnAsRandomGimmick(setting))
                {
                    continue;
                }

                cumulativeChance += Mathf.Clamp01(setting.SpawnChance);
                if (roll <= cumulativeChance)
                {
                    return setting;
                }
            }

            return normalSetting;
        }

        public PlatformGimmickSetting ResolveDoubleFollowUp()
        {
            var normalSetting = Find(PlatformGimmickType.Normal);
            var smallSetting = Find(PlatformGimmickType.Small);
            var fastSetting = Find(PlatformGimmickType.Fast);

            if (smallSetting == null && fastSetting == null)
            {
                return normalSetting;
            }

            return Random.value < 0.5f
                ? smallSetting ?? fastSetting
                : fastSetting ?? smallSetting;
        }

        public PlatformGimmickSetting Find(PlatformGimmickType type)
        {
            var settings = _configData.PlatformGimmickSettings;
            if (settings == null)
            {
                return null;
            }

            for (var i = 0; i < settings.Length; i++)
            {
                var setting = settings[i];
                if (setting != null && setting.Type == type)
                {
                    return setting;
                }
            }

            return null;
        }

        private bool CanSpawnAsRandomGimmick(PlatformGimmickSetting setting)
        {
            return setting != null &&
                   setting.Type != PlatformGimmickType.Normal &&
                   _scoreService.Score >= setting.StartScore;
        }
    }
}
