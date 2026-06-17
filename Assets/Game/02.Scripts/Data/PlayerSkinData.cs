using UnityEngine;

namespace JumJump.Data
{
    [CreateAssetMenu(fileName = nameof(PlayerSkinData), menuName = "JumJump/Player Skin Data")]
    public sealed class PlayerSkinData : ScriptableObject
    {
        public int SkinId => _skinId;
        public RuntimeAnimatorController AnimatorController => _animatorController;

        [SerializeField] private int _skinId;
        [SerializeField] private RuntimeAnimatorController _animatorController;
    }
}
