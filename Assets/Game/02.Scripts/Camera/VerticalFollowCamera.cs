using JumJump.Event;
using JumJump.Interface;
using UnityEngine;
using VContainer;

namespace JumJump.Camera
{
    public sealed class VerticalFollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private float _minimumY;
        [SerializeField] private float _smoothSpeed = 8f;
        [SerializeField] private float _verticalOffset = 1.2f;

        private IEventBus _eventBus;

        [Inject]
        public void Construct(IEventBus eventBus)
        {
            _eventBus = eventBus;
            _eventBus.Subscribe<PlayerSpawnedEvent>(OnPlayerSpawned);
        }

        public void Bind(Transform target)
        {
            _target = target;
        }

        private void OnPlayerSpawned(in PlayerSpawnedEvent ev)
        {
            if (ev.Player == null)
            {
                return;
            }

            Bind(ev.Player.transform);
        }

        private void OnDestroy()
        {
            _eventBus?.Unsubscribe<PlayerSpawnedEvent>(OnPlayerSpawned);
        }

        private void LateUpdate()
        {
            if (_target == null)
            {
                return;
            }

            var nextPosition = transform.position;
            var targetY = Mathf.Max(_minimumY, _target.position.y + _verticalOffset);
            nextPosition.y = Mathf.Lerp(nextPosition.y, targetY, _smoothSpeed * Time.deltaTime);
            transform.position = nextPosition;
        }
    }
}
