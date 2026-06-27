using JumJump.Data;
using JumJump.Event;
using JumJump.Interface;
using JumJump.Registry;
using JumJump.Util;
using UnityEngine;
using VContainer;

namespace JumJump.Camera
{
    public sealed class VerticalFollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform _target;

        private bool _isOverviewMode;
        private float _defaultOrthographicSize;
        private float _overviewTargetY;
        private float _overviewTargetOrthographicSize;
        private IEventBus _eventBus;
        private GameConfigData _configData;
        private PlatformRegistry _platformRegistry;
        private UnityEngine.Camera _camera;

        [Inject]
        public void Construct(
            IEventBus eventBus,
            GameConfigData configData,
            PlatformRegistry platformRegistry,
            UnityEngine.Camera gameCamera)
        {
            _eventBus = eventBus;
            _configData = configData;
            _platformRegistry = platformRegistry;
            _camera = gameCamera;
            _defaultOrthographicSize = _camera.orthographicSize;
            _eventBus.Subscribe<PlayerSpawnedEvent>(OnPlayerSpawned);
            _eventBus.Subscribe<GameOverResultViewRequestedEvent>(OnGameOverResultViewRequested);
            _eventBus.Subscribe<GameResetEvent>(OnGameReset);
        }

        public void Bind(Transform target)
        {
            _target = target;
        }

        private void OnPlayerSpawned(in PlayerSpawnedEvent ev)
        {
            ExitOverviewMode();
            Bind(ev.Player.transform);
        }

        private void OnGameOverResultViewRequested(in GameOverResultViewRequestedEvent ev)
        {
            _platformRegistry.ShowAllForResultView();
            if (!_platformRegistry.TryGetRoundVerticalBounds(out var bottomY, out var topY))
            {
                return;
            }

            var bottomViewportY = Mathf.Clamp01(GameConst.Camera.GameOverOverviewBottomViewportY);
            var topViewportY = Mathf.Clamp(
                GameConst.Camera.GameOverOverviewTopViewportY,
                bottomViewportY + 0.01f,
                1f);
            var platformHeight = Mathf.Max(0.01f, topY - bottomY);
            var visibleHeightRatio = Mathf.Max(0.01f, topViewportY - bottomViewportY);
            var requiredOrthographicSize = platformHeight / (2f * visibleHeightRatio);

            _overviewTargetOrthographicSize = Mathf.Max(_defaultOrthographicSize, requiredOrthographicSize);
            _overviewTargetY = Mathf.Max(
                _configData.CameraMinimumY,
                bottomY + _overviewTargetOrthographicSize * (1f - bottomViewportY * 2f));
            _isOverviewMode = true;
        }

        private void OnGameReset(in GameResetEvent ev)
        {
            ExitOverviewMode();
        }

        private void OnDestroy()
        {
            _eventBus.Unsubscribe<PlayerSpawnedEvent>(OnPlayerSpawned);
            _eventBus.Unsubscribe<GameOverResultViewRequestedEvent>(OnGameOverResultViewRequested);
            _eventBus.Unsubscribe<GameResetEvent>(OnGameReset);
        }

        private void LateUpdate()
        {
            if (_isOverviewMode)
            {
                UpdateOverviewCamera();
                return;
            }

            if (_target == null)
            {
                return;
            }

            var nextPosition = transform.position;
            var targetY = Mathf.Max(_configData.CameraMinimumY, _target.position.y + _configData.CameraVerticalOffset);
            nextPosition.y = Mathf.Lerp(nextPosition.y, targetY, _configData.CameraSmoothSpeed * Time.deltaTime);
            transform.position = nextPosition;
        }

        private void UpdateOverviewCamera()
        {
            var nextPosition = transform.position;
            nextPosition.y = Mathf.Lerp(
                nextPosition.y,
                _overviewTargetY,
                _configData.CameraSmoothSpeed * Time.deltaTime);
            transform.position = nextPosition;
            _camera.orthographicSize = Mathf.Lerp(
                _camera.orthographicSize,
                _overviewTargetOrthographicSize,
                _configData.CameraSmoothSpeed * Time.deltaTime);
        }

        private void ExitOverviewMode()
        {
            _isOverviewMode = false;
            _overviewTargetY = 0f;
            _overviewTargetOrthographicSize = _defaultOrthographicSize;
            _camera.orthographicSize = _defaultOrthographicSize;
        }
    }
}
