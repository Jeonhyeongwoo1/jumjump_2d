using UnityEngine;

namespace JumJump.Data
{
    [CreateAssetMenu(fileName = nameof(GameConfigData), menuName = "JumJump/Game Config Data")]
    public sealed class GameConfigData : ScriptableObject
    {
        public int ScorePerLanding => _scorePerLanding;
        public float ComboLandingCenterTolerance => _comboLandingCenterTolerance;
        public string HighScoreKey => _highScoreKey;
        public string GoldKey => _goldKey;
        public string SelectedPlayerSkinKey => _selectedPlayerSkinKey;
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
        public AudioClip BgmGameLoopClip => _bgmGameLoopClip;
        public AudioClip UiButtonTapClip => _uiButtonTapClip;
        public AudioClip UiCountdownTickClip => _uiCountdownTickClip;
        public AudioClip PlayerJumpClip => _playerJumpClip;
        public AudioClip LandingNormalClip => _landingNormalClip;
        public AudioClip GoldCollectClip => _goldCollectClip;
        public AudioClip PlayerMissClip => _playerMissClip;
        public AudioClip PlayerDeadClip => _playerDeadClip;

        [Header("Score")]
        [SerializeField] private int _scorePerLanding = 1;
        [SerializeField] private float _comboLandingCenterTolerance = 0.08f;

        [Header("Persistence")]
        [SerializeField] private string _highScoreKey = "JumJump.HighScore";
        [SerializeField] private string _goldKey = "JumJump.Gold";
        [SerializeField] private string _selectedPlayerSkinKey = "JumJump.SelectedPlayerSkin";

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
        [SerializeField] private AudioClip _bgmGameLoopClip;
        [SerializeField] private AudioClip _uiButtonTapClip;
        [SerializeField] private AudioClip _uiCountdownTickClip;
        [SerializeField] private AudioClip _playerJumpClip;
        [SerializeField] private AudioClip _landingNormalClip;
        [SerializeField] private AudioClip _goldCollectClip;
        [SerializeField] private AudioClip _playerMissClip;
        [SerializeField] private AudioClip _playerDeadClip;
    }
}
