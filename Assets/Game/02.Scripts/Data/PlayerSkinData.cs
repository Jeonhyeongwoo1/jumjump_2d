using UnityEngine;

namespace JumJump.Data
{
    [CreateAssetMenu(fileName = nameof(PlayerSkinData), menuName = "JumJump/Player Skin Data")]
    public sealed class PlayerSkinData : ScriptableObject
    {
        public int SkinId => _skinId;
        public int Price => _price;
        public RuntimeAnimatorController AnimatorController => _animatorController;

        [SerializeField] private int _skinId;
        [SerializeField] private int _price;
        [SerializeField] private RuntimeAnimatorController _animatorController;
    }
}
