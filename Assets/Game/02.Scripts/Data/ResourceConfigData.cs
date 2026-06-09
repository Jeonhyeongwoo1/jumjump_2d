using UnityEngine;

namespace JumJump.Data
{
    [CreateAssetMenu(fileName = nameof(ResourceConfigData), menuName = "JumJump/Resource Config Data")]
    public sealed class ResourceConfigData : ScriptableObject
    {
        public string PreLoadLabel => _preLoadLabel;
        public string PlayerAddressableKey => _playerAddressableKey;
        public string GameSceneUiAddressableKey => _gameSceneUiAddressableKey;
        public string GameOverPopupAddressableKey => _gameOverPopupAddressableKey;
        public string DynamicFontAddressableKey => _dynamicFontAddressableKey;
        public string PlatformAddressableKey => _platformAddressableKey;

        public string PlayerPoolKey => _playerPoolKey;
        public int PlayerPrewarmCount => _playerPrewarmCount;
        public string DynamicFontPoolKey => _dynamicFontPoolKey;
        public int DynamicFontPrewarmCount => _dynamicFontPrewarmCount;
        public string PlatformPoolKey => _platformPoolKey;
        public int PlatformPrewarmCount => _platformPrewarmCount;

        [Header("Addressables")]
        [SerializeField] private string _preLoadLabel = "PreLoad";
        [SerializeField] private string _playerAddressableKey = "Player";
        [SerializeField] private string _platformAddressableKey = "Platform";
        [SerializeField] private string _gameSceneUiAddressableKey = "UI_GameScene";
        [SerializeField] private string _gameOverPopupAddressableKey = "UI_GameOverPopup";
        [SerializeField] private string _dynamicFontAddressableKey = "UI_DynamicFont";

        [Header("Pooling")]
        [SerializeField] private string _playerPoolKey = "Player";
        [SerializeField] private int _playerPrewarmCount = 1;
        [SerializeField] private string _platformPoolKey = "Platform";
        [SerializeField] private int _platformPrewarmCount = 12;
        [SerializeField] private string _dynamicFontPoolKey = "UI_DynamicFont";
        [SerializeField] private int _dynamicFontPrewarmCount = 5;
    }
}
