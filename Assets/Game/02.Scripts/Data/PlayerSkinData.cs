using UnityEngine;

namespace JumJump.Data
{
    [CreateAssetMenu(fileName = nameof(PlayerSkinData), menuName = "JumJump/Player Skin Data")]
    public sealed class PlayerSkinData : ScriptableObject
    {
        public int SkinId => _skinId;
        public int Price => _price;
        public string EnglishName => _englishName;
        public string KoreanName => _koreanName;
        public RuntimeAnimatorController AnimatorController => _animatorController;

        [SerializeField] private int _skinId;
        [SerializeField] private int _price;
        [SerializeField] private string _englishName;
        [SerializeField] private string _koreanName;
        [SerializeField] private RuntimeAnimatorController _animatorController;
    }
}
