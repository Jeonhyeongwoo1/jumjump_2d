using UnityEngine;

namespace JumJump.Data
{
    /// <summary>
    /// 모든 런타임 튜닝값과 Addressable/Pool 키의 단일 소스(ScriptableObject).
    /// 도메인별 필드·프로퍼티는 partial 파일로 분리되어 있다:
    /// GameConfigData.Platform.cs / .Player.cs / .Gameplay.cs / .Background.cs
    /// </summary>
    [CreateAssetMenu(fileName = nameof(GameConfigData), menuName = "JumJump/Game Config Data")]
    public sealed partial class GameConfigData : ScriptableObject
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
