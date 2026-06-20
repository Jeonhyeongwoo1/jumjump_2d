using UnityEngine;

namespace JumJump.Data
{
    [CreateAssetMenu(fileName = nameof(GameCheatConfigData), menuName = "JumJump/Game Cheat Config Data")]
    public sealed class GameCheatConfigData : ScriptableObject
    {
        public bool ForcePlatformGimmick => _forcePlatformGimmick;
        public PlatformGimmickType ForcedPlatformGimmickType => _forcedPlatformGimmickType;
        public bool ForcePlayerSkin => _forcePlayerSkin;
        public PlayerSkinType ForcedPlayerSkinType => _forcedPlayerSkinType;

        [Header("Platform")]
        [SerializeField] private bool _forcePlatformGimmick;
        [SerializeField] private PlatformGimmickType _forcedPlatformGimmickType = PlatformGimmickType.Normal;

        [Header("Player Skin")]
        [SerializeField] private bool _forcePlayerSkin;
        [SerializeField] private PlayerSkinType _forcedPlayerSkinType = PlayerSkinType.Player_1;
    }
}
