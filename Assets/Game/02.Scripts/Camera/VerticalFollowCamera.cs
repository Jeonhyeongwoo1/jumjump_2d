using UnityEngine;

namespace JumJump.Camera
{
    public sealed class VerticalFollowCamera : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private float _minimumY;
        [SerializeField] private float _smoothSpeed = 8f;
        [SerializeField] private float _verticalOffset = 1.2f;

        public void Bind(Transform target)
        {
            _target = target;
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
