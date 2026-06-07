using UnityEngine;

namespace JumJump.Data
{
    [CreateAssetMenu(fileName = nameof(PlatformCheatData), menuName = "JumJump/Platform Cheat Data")]
    public sealed class PlatformCheatData : ScriptableObject
    {
        public bool ForcePlatformGimmick => _forcePlatformGimmick;
        public PlatformGimmickType ForcedPlatformGimmickType => _forcedPlatformGimmickType;

        [SerializeField] private bool _forcePlatformGimmick;
        [SerializeField] private PlatformGimmickType _forcedPlatformGimmickType = PlatformGimmickType.Normal;
    }
}
