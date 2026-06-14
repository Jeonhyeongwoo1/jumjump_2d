using UnityEngine;

namespace JumJump.Data
{
    [CreateAssetMenu(fileName = nameof(GameConfigData), menuName = "JumJump/Game Config Data")]
    public sealed class GameConfigData : ScriptableObject
    {
        public int ScorePerLanding => _scorePerLanding;
        public float ComboLandingCenterTolerance => _comboLandingCenterTolerance;
        public string HighScoreKey => _highScoreKey;
        public string TapActionPath => _tapActionPath;
        public float CameraMinimumY => _cameraMinimumY;
        public float CameraSmoothSpeed => _cameraSmoothSpeed;
        public float CameraVerticalOffset => _cameraVerticalOffset;
        public int RocketBoostPlatformCount => _rocketBoostPlatformCount;

        [Header("Score")]
        [SerializeField] private int _scorePerLanding = 1;
        [SerializeField] private float _comboLandingCenterTolerance = 0.08f;

        [Header("Persistence")]
        [SerializeField] private string _highScoreKey = "JumJump.HighScore";

        [Header("Input")]
        [SerializeField] private string _tapActionPath = "Player/Attack";

        [Header("Camera")]
        [SerializeField] private float _cameraMinimumY;
        [SerializeField] private float _cameraSmoothSpeed = 8f;
        [SerializeField] private float _cameraVerticalOffset = 1.2f;

        [Header("Rocket")]
        [SerializeField] private int _rocketBoostPlatformCount = 10;
    }
}
