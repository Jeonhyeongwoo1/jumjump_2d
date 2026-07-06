using JumJump.Data;
using JumJump.Registry;
using UnityEngine;
using VContainer;

namespace JumJump.Service
{
    public sealed class PlatformGimmickSelector
    {
        private const float HarmfulGimmickMaxSpawnChance = 0.75f;
        private const int HelpfulGimmickMinPlatformInterval = 10;
        private const int HelpfulGimmickMaxPlatformInterval = 15;

        private readonly PlatformConfigData _configData;
        private readonly GameCheatConfigData _gameCheatConfigData;
        private readonly ScoreService _scoreService;
        private readonly PlayerRegistry _playerRegistry;
        private int _helpfulGimmickPlatformCount;
        private int _nextHelpfulGimmickPlatformInterval;
        private int _lastObservedBaseScore;
        private bool _hasHelpfulGimmickSchedule;

        [Inject]
        public PlatformGimmickSelector(
            PlatformConfigData configData,
            GameCheatConfigData gameCheatConfigData,
            ScoreService scoreService,
            PlayerRegistry playerRegistry)
        {
            _configData = configData;
            _gameCheatConfigData = gameCheatConfigData;
            _scoreService = scoreService;
            _playerRegistry = playerRegistry;
        }

        public PlatformGimmickSetting ResolvePrimary()
        {
            var settings = _configData.PlatformGimmickSettings;
            var normalSetting = Find(PlatformGimmickType.Normal);
            if (_gameCheatConfigData.ForcePlatformGimmick)
            {
                var forcedSetting = Find(_gameCheatConfigData.ForcedPlatformGimmickType);
                if (ShouldSkipShieldGimmick(forcedSetting))
                {
                    return normalSetting;
                }

                return forcedSetting ?? normalSetting;
            }

            if (settings == null || settings.Length == 0)
            {
                return normalSetting;
            }

            RefreshHelpfulGimmickSchedule();
            _helpfulGimmickPlatformCount++;
            var helpfulSetting = ResolveScheduledHelpfulGimmick(settings);
            if (helpfulSetting != null)
            {
                ScheduleNextHelpfulGimmick();
                return helpfulSetting;
            }

            var totalChance = Mathf.Min(ResolveGroupChance(settings, false, true), HarmfulGimmickMaxSpawnChance);
            if (totalChance <= 0f)
            {
                return normalSetting;
            }

            if (Random.value > totalChance)
            {
                return normalSetting;
            }

            var selected = ResolveWeightedGimmick(settings, false, true);
            if (selected != null)
            {
                return selected;
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

        private bool HasReachedStartScore(PlatformGimmickSetting setting, bool useStartScore)
        {
            return !useStartScore || _scoreService.BaseScore >= setting.StartScore;
        }

        private void RefreshHelpfulGimmickSchedule()
        {
            var baseScore = _scoreService.BaseScore;
            if (!_hasHelpfulGimmickSchedule || baseScore < _lastObservedBaseScore)
            {
                ScheduleNextHelpfulGimmick();
            }

            _lastObservedBaseScore = baseScore;
        }

        private void ScheduleNextHelpfulGimmick()
        {
            _helpfulGimmickPlatformCount = 0;
            _nextHelpfulGimmickPlatformInterval = Random.Range(
                HelpfulGimmickMinPlatformInterval,
                HelpfulGimmickMaxPlatformInterval + 1);
            _hasHelpfulGimmickSchedule = true;
        }

        private PlatformGimmickSetting ResolveScheduledHelpfulGimmick(PlatformGimmickSetting[] settings)
        {
            if (!_hasHelpfulGimmickSchedule ||
                _helpfulGimmickPlatformCount < _nextHelpfulGimmickPlatformInterval)
            {
                return null;
            }

            return ResolveWeightedGimmick(settings, true, false);
        }

        private float ResolveGroupChance(
            PlatformGimmickSetting[] settings,
            bool isHelpfulGroup,
            bool useStartScore)
        {
            var totalChance = 0f;
            for (var i = 0; i < settings.Length; i++)
            {
                var setting = settings[i];
                if (!CanSpawnGimmick(setting, isHelpfulGroup, useStartScore))
                {
                    continue;
                }

                totalChance += Mathf.Clamp01(setting.SpawnChance);
            }

            return totalChance;
        }

        private PlatformGimmickSetting ResolveWeightedGimmick(
            PlatformGimmickSetting[] settings,
            bool isHelpfulGroup,
            bool useStartScore)
        {
            var totalChance = ResolveGroupChance(settings, isHelpfulGroup, useStartScore);
            if (totalChance <= 0f)
            {
                return null;
            }

            var roll = Random.Range(0f, totalChance);
            var cumulativeChance = 0f;
            for (var i = 0; i < settings.Length; i++)
            {
                var setting = settings[i];
                if (!CanSpawnGimmick(setting, isHelpfulGroup, useStartScore))
                {
                    continue;
                }

                cumulativeChance += Mathf.Clamp01(setting.SpawnChance);
                if (roll <= cumulativeChance)
                {
                    return setting;
                }
            }

            return null;
        }

        private bool IsHelpfulGimmick(PlatformGimmickType type)
        {
            return type == PlatformGimmickType.Shield ||
                   type == PlatformGimmickType.Rocket;
        }

        private bool CanSpawnGimmick(
            PlatformGimmickSetting setting,
            bool isHelpfulGroup,
            bool useStartScore)
        {
            return setting != null &&
                   setting.Type != PlatformGimmickType.Normal &&
                   IsHelpfulGimmick(setting.Type) == isHelpfulGroup &&
                   HasReachedStartScore(setting, useStartScore) &&
                   !ShouldSkipShieldGimmick(setting);
        }

        private bool ShouldSkipShieldGimmick(PlatformGimmickSetting setting)
        {
            return setting != null &&
                   setting.Type == PlatformGimmickType.Shield &&
                   PlayerHasShield();
        }

        private bool PlayerHasShield()
        {
            var player = _playerRegistry.Player;
            return player != null && player.HasShield;
        }
    }
}
