using UnityEngine;

namespace JumJump.Util
{
    public static class GameLogger
    {
        private static GameLogLevel _minimumLevel = GameLogLevel.Info;

        public static GameLogLevel MinimumLevel => _minimumLevel;

        public static void SetMinimumLevel(GameLogLevel minimumLevel)
        {
            _minimumLevel = minimumLevel;
        }

        public static void Debug(string owner, string message)
        {
            Write(GameLogLevel.Debug, owner, message);
        }

        public static void Info(string owner, string message)
        {
            Write(GameLogLevel.Info, owner, message);
        }

        public static void Warning(string owner, string message)
        {
            Write(GameLogLevel.Warning, owner, message);
        }

        public static void Error(string owner, string message)
        {
            Write(GameLogLevel.Error, owner, message);
        }

        public static float BeginWork(string owner, string workName, GameLogLevel level = GameLogLevel.Info)
        {
            var startedAt = Time.realtimeSinceStartup;
            Write(level, owner, $"Begin work: {workName}");
            return startedAt;
        }

        public static void EndWork(
            string owner,
            string workName,
            float startedAt,
            GameLogLevel level = GameLogLevel.Info)
        {
            var elapsedMs = Mathf.Max(0f, (Time.realtimeSinceStartup - startedAt) * 1000f);
            Write(level, owner, $"End work: {workName} ({elapsedMs:0.0} ms)");
        }

        public static bool IsEnabled(GameLogLevel level)
        {
            return _minimumLevel != GameLogLevel.Off && level >= _minimumLevel;
        }

        private static void Write(GameLogLevel level, string owner, string message)
        {
            if (!IsEnabled(level))
            {
                return;
            }

            var formattedMessage = $"[{owner}] {message}";
            switch (level)
            {
                case GameLogLevel.Warning:
                    UnityEngine.Debug.LogWarning(formattedMessage);
                    break;
                case GameLogLevel.Error:
                    UnityEngine.Debug.LogError(formattedMessage);
                    break;
                default:
                    UnityEngine.Debug.Log(formattedMessage);
                    break;
            }
        }
    }
}
