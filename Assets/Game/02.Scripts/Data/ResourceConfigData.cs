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
        public string ScoreBoardAddressableKey => _scoreBoardAddressableKey;
        public string[] ScoreBoardSpriteAddressableKeys => _scoreBoardSpriteAddressableKeys;
        public string[] PlayerSpriteAddressableKeys => _playerSpriteAddressableKeys;

        public string PlayerPoolKey => _playerPoolKey;
        public int PlayerPrewarmCount => _playerPrewarmCount;
        public string DynamicFontPoolKey => _dynamicFontPoolKey;
        public int DynamicFontPrewarmCount => _dynamicFontPrewarmCount;
        public string PlatformPoolKey => _platformPoolKey;
        public int PlatformPrewarmCount => _platformPrewarmCount;
        public string ScoreBoardPoolKey => _scoreBoardPoolKey;
        public int ScoreBoardPrewarmCount => _scoreBoardPrewarmCount;

        [Header("Addressables")]
        [SerializeField] private string _preLoadLabel = "PreLoad";
        [SerializeField] private string _playerAddressableKey = "Player";
        [SerializeField] private string _platformAddressableKey = "Platform";
        [SerializeField] private string _gameSceneUiAddressableKey = "UI_GameScene";
        [SerializeField] private string _gameOverPopupAddressableKey = "UI_GameOverPopup";
        [SerializeField] private string _dynamicFontAddressableKey = "UI_DynamicFont";
        [SerializeField] private string _scoreBoardAddressableKey = "ScoreBoard";
        [SerializeField] private string[] _playerSpriteAddressableKeys =
        {
            "Player_1001",
            "Player_1002",
            "Player_1003",
            "Player_1004",
            "Player_1005",
            "Player_1006",
            "Player_1007",
            "Player_1008"
        };
        [SerializeField] private string[] _scoreBoardSpriteAddressableKeys =
        {
            "Scoreboard_1.sprite",
            "Scoreboard_2.sprite",
            "Scoreboard_3.sprite",
            "Scoreboard_4.sprite",
            "Scoreboard_5.sprite"
        };

        [Header("Pooling")]
        [SerializeField] private string _playerPoolKey = "Player";
        [SerializeField] private int _playerPrewarmCount = 1;
        [SerializeField] private string _platformPoolKey = "Platform";
        [SerializeField] private int _platformPrewarmCount = 12;
        [SerializeField] private string _dynamicFontPoolKey = "UI_DynamicFont";
        [SerializeField] private int _dynamicFontPrewarmCount = 5;
        [SerializeField] private string _scoreBoardPoolKey = "ScoreBoard";
        [SerializeField] private int _scoreBoardPrewarmCount = 1;
    }
}
