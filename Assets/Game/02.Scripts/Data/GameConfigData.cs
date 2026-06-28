using UnityEngine;

namespace JumJump.Data
{
    [CreateAssetMenu(fileName = nameof(GameConfigData), menuName = "JumJump/Game Config Data")]
    public sealed class GameConfigData : ScriptableObject
    {
        public int ScorePerLanding => _scorePerLanding;
        public int AdRewardGoldAmount => _adRewardGoldAmount;
        public float ComboLandingCenterTolerance => _comboLandingCenterTolerance;
        public string HighScoreKey => _highScoreKey;
        public string GoldKey => _goldKey;
        public string SelectedPlayerSkinKey => _selectedPlayerSkinKey;
        public string PurchasedPlayerSkinsKey => _purchasedPlayerSkinsKey;
        public string TapActionPath => _tapActionPath;
        public float CameraMinimumY => _cameraMinimumY;
        public float CameraSmoothSpeed => _cameraSmoothSpeed;
        public float CameraVerticalOffset => _cameraVerticalOffset;
        public int RocketBoostPlatformCount => _rocketBoostPlatformCount;
        public GameLogLevel MinimumLogLevel => _minimumLogLevel;
        public bool SoundEnabled => _soundEnabled;
        public bool BgmEnabled => _bgmEnabled;
        public bool SfxEnabled => _sfxEnabled;
        public float MasterVolume => _masterVolume;
        public float BgmVolume => _bgmVolume;
        public float SfxVolume => _sfxVolume;
        public string BgmGameLoopAddressableKey => _bgmGameLoopAddressableKey;
        public string UiButtonTapAddressableKey => _uiButtonTapAddressableKey;
        public string UiCountdownTickAddressableKey => _uiCountdownTickAddressableKey;
        public string PlayerJumpAddressableKey => _playerJumpAddressableKey;
        public string LandingNormalAddressableKey => _landingNormalAddressableKey;
        public string GoldCollectAddressableKey => _goldCollectAddressableKey;
        public string PlayerMissAddressableKey => _playerMissAddressableKey;
        public string PlayerDeadAddressableKey => _playerDeadAddressableKey;

        [Header("Score")]
        [SerializeField] private int _scorePerLanding = 1;
        [SerializeField] private float _comboLandingCenterTolerance = 0.08f;

        [Header("Ad Reward")]
        [SerializeField, Min(0)] private int _adRewardGoldAmount = 1000;

        [Header("Persistence")]
        [SerializeField] private string _highScoreKey = "JumJump.HighScore";
        [SerializeField] private string _goldKey = "JumJump.Gold";
        [SerializeField] private string _selectedPlayerSkinKey = "JumJump.SelectedPlayerSkin";
        [SerializeField] private string _purchasedPlayerSkinsKey = "JumJump.PurchasedPlayerSkins";

        [Header("Input")]
        [SerializeField] private string _tapActionPath = "Player/Attack";

        [Header("Camera")]
        [SerializeField] private float _cameraMinimumY;
        [SerializeField] private float _cameraSmoothSpeed = 8f;
        [SerializeField] private float _cameraVerticalOffset = 1.2f;

        [Header("Rocket")]
        [SerializeField] private int _rocketBoostPlatformCount = 10;

        [Header("Logging")]
        [SerializeField] private GameLogLevel _minimumLogLevel = GameLogLevel.Info;

        [Header("Sound")]
        [SerializeField] private bool _soundEnabled = true;
        [SerializeField] private bool _bgmEnabled = true;
        [SerializeField] private bool _sfxEnabled = true;
        [SerializeField, Range(0f, 1f)] private float _masterVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float _bgmVolume = 0.55f;
        [SerializeField, Range(0f, 1f)] private float _sfxVolume = 0.9f;
        [SerializeField] private string _bgmGameLoopAddressableKey = "Sound_BgmGameLoop";
        [SerializeField] private string _uiButtonTapAddressableKey = "Sound_UiButtonTap";
        [SerializeField] private string _uiCountdownTickAddressableKey = "Sound_UiCountdownTick";
        [SerializeField] private string _playerJumpAddressableKey = "Sound_PlayerJump";
        [SerializeField] private string _landingNormalAddressableKey = "Sound_LandingNormal";
        [SerializeField] private string _goldCollectAddressableKey = "Sound_GoldCollect";
        [SerializeField] private string _playerMissAddressableKey = "Sound_PlayerMiss";
        [SerializeField] private string _playerDeadAddressableKey = "Sound_PlayerDead";
    }
}
