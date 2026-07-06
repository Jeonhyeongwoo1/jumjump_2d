using System;
using UnityEditor;

namespace JumJump.Editor
{
    [InitializeOnLoad]
    public static class AITDevServerEnvironment
    {
        private const string CiEnvironmentKey = "CI";

        static AITDevServerEnvironment()
        {
            // AIT Dev Server runs pnpm without a TTY; CI mode skips pnpm's purge confirmation prompt.
            Environment.SetEnvironmentVariable(CiEnvironmentKey, "true", EnvironmentVariableTarget.Process);
        }
    }
}
