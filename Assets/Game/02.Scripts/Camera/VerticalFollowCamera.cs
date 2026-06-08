using JumJump.Data;
using JumJump.Event;
using JumJump.Interface;
using UnityEngine;
using VContainer;

namespace JumJump.Camera
{
    public sealed class VerticalFollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform _target;

        private IEventBus _eventBus;
        private GameConfigData _configData;

        [Inject]
        public void Construct(IEventBus eventBus, GameConfigData configData)
        {
            _eventBus = eventBus;
            _configData = configData;
            _eventBus.Subscribe<PlayerSpawnedEvent>(OnPlayerSpawned);
        }

        public void Bind(Transform target)
        {
            _target = target;
        }

        private void OnPlayerSpawned(in PlayerSpawnedEvent ev)
        {
            Bind(ev.Player.transform);
        }

        private void OnDestroy()
        {
            _eventBus.Unsubscribe<PlayerSpawnedEvent>(OnPlayerSpawned);
        }

        private void LateUpdate()
        {
            if (_target == null)
            {
                return;
            }

            var nextPosition = transform.position;
            var targetY = Mathf.Max(_configData.CameraMinimumY, _target.position.y + _configData.CameraVerticalOffset);
            nextPosition.y = Mathf.Lerp(nextPosition.y, targetY, _configData.CameraSmoothSpeed * Time.deltaTime);
            transform.position = nextPosition;
        }
    }
}
