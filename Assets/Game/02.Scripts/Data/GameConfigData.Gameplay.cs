using UnityEngine;

namespace JumJump.Data
{
    public sealed partial class GameConfigData
    {
        public int ScorePerLanding => _scorePerLanding;
        public string HighScoreKey => _highScoreKey;
        public string TapActionPath => _tapActionPath;
        public float CameraMinimumY => _cameraMinimumY;
        public float CameraSmoothSpeed => _cameraSmoothSpeed;
        public float CameraVerticalOffset => _cameraVerticalOffset;

        [Header("Score")]
        [SerializeField] private int _scorePerLanding = 1;

        [Header("Persistence")]
        [SerializeField] private string _highScoreKey = "JumJump.HighScore";

        [Header("Input")]
        [SerializeField] private string _tapActionPath = "Player/Attack";

        [Header("Camera")]
        [SerializeField] private float _cameraMinimumY;
        [SerializeField] private float _cameraSmoothSpeed = 8f;
        [SerializeField] private float _cameraVerticalOffset = 1.2f;
    }
}
