using System.Collections.Generic;
using System.Text;
using JumJump.Data;
using JumJump.Util;

namespace JumJump.Service
{
    public sealed class LocalizationService
    {
        private const string PlayerSkinNameKeyPrefix = "player_skin.";
        private const string PlayerSkinNameKeySuffix = ".name";
        private const string PlayerSkinDescriptionKeySuffix = ".description";

        private readonly ResourceConfigData _resourceConfigData;
        private readonly Dictionary<string, LocalizedTextEntry> _textEntries = new Dictionary<string, LocalizedTextEntry>(32);
        private bool _isLoaded;

        public LocalizationService(ResourceConfigData resourceConfigData)
        {
            _resourceConfigData = resourceConfigData;
        }

        public string GetPlayerSkinName(int skinId)
        {
            return GetText($"{PlayerSkinNameKeyPrefix}{skinId}{PlayerSkinNameKeySuffix}");
        }

        public string GetPlayerSkinDescription(int skinId)
        {
            return GetText($"{PlayerSkinNameKeyPrefix}{skinId}{PlayerSkinDescriptionKeySuffix}");
        }

        public string GetText(string key)
        {
            EnsureLoaded();

            if (!_textEntries.TryGetValue(key, out var entry))
            {
                GameLogger.Error(nameof(LocalizationService), $"Missing localization key: {key}");
                return key;
            }

            return ResolveText(entry);
        }

        private void EnsureLoaded()
        {
            if (_isLoaded)
            {
                return;
            }

            LoadCsv(_resourceConfigData.LocalizationCsv.text);
            _isLoaded = true;
        }

        private void LoadCsv(string csv)
        {
            _textEntries.Clear();

            var lines = csv.Split('\n');
            for (var i = 1; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var columns = ParseCsvRow(line);
                if (columns.Length < 3)
                {
                    GameLogger.Error(nameof(LocalizationService), $"Invalid localization row: {line}");
                    continue;
                }

                var key = columns[0].Trim();
                var english = columns[1].Trim();
                var korean = columns[2].Trim();
                _textEntries[key] = new LocalizedTextEntry(english, korean);
            }
        }

        private string[] ParseCsvRow(string line)
        {
            var columns = new List<string>(3);
            var current = new StringBuilder(line.Length);
            var inQuotes = false;

            for (var i = 0; i < line.Length; i++)
            {
                var character = line[i];
                if (character == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append(character);
                        i++;
                        continue;
                    }

                    inQuotes = !inQuotes;
                    continue;
                }

                if (character == ',' && !inQuotes)
                {
                    columns.Add(current.ToString().Trim());
                    current.Clear();
                    continue;
                }

                current.Append(character);
            }

            columns.Add(current.ToString().Trim());
            return columns.ToArray();
        }

        private string ResolveText(LocalizedTextEntry entry)
        {
            if (_resourceConfigData.LocalizationLanguage == LocalizationLanguageType.Korean)
            {
                return entry.Korean;
            }

            return entry.English;
        }

        private readonly struct LocalizedTextEntry
        {
            public string English { get; }
            public string Korean { get; }

            public LocalizedTextEntry(string english, string korean)
            {
                English = english;
                Korean = korean;
            }
        }
    }
}
