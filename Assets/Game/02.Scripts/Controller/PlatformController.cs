using System;
using UnityEngine;

namespace JumJump.Controller
{
    public sealed class PlatformController : MonoBehaviour
    {
        public int PlatformIndex => _platformIndex;
        public float CenterY => transform.position.y;

        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private BoxCollider2D _landingCollider;
        [SerializeField] private float _landingHeight = 0.48f;

        private int _platformIndex;
        private float _halfWidth = 0.7f;
        private float _moveSpeed;
        private float _leftBound;
        private float _rightBound;
        private int _moveDirection = 1;
        private bool _isActive;
        private Action<PlatformController> _onReleaseAction;

        public void InjectRelease(Action<PlatformController> onReleaseAction)
        {
            _onReleaseAction = onReleaseAction;
        }

        public void Initialize(int platformIndex, Vector3 position, float width, float moveSpeed, float leftBound, float rightBound)
        {
            _platformIndex = platformIndex;
            _halfWidth = width * 0.5f;
            _moveSpeed = moveSpeed;
            _leftBound = leftBound;
            _rightBound = rightBound;
            _moveDirection = 1;
            _isActive = true;
            transform.position = position;
            transform.localScale = new Vector3(width, 0.32f, 1f);
            gameObject.SetActive(true);

            if (_spriteRenderer != null)
            {
                _spriteRenderer.size = new Vector2(width, 0.32f);
            }

            if (_landingCollider != null)
            {
                _landingCollider.size = new Vector2(width, _landingHeight);
            }
        }

        public Vector3 GetLandingPosition(float characterVerticalOffset)
        {
            var platformPosition = transform.position;
            return new Vector3(platformPosition.x, platformPosition.y + characterVerticalOffset, platformPosition.z);
        }

        public bool IsLandingPointInside(Vector3 characterPosition, float characterVerticalOffset, float verticalTolerance)
        {
            var platformPosition = transform.position;
            var landingY = platformPosition.y + characterVerticalOffset;

            if (Mathf.Abs(characterPosition.y - landingY) > verticalTolerance)
            {
                return false;
            }

            return characterPosition.x >= platformPosition.x - _halfWidth &&
                   characterPosition.x <= platformPosition.x + _halfWidth;
        }

        public void Release()
        {
            if (!_isActive)
            {
                return;
            }

            _isActive = false;
            gameObject.SetActive(false);
            _onReleaseAction?.Invoke(this);
        }

        private void Awake()
        {
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (_landingCollider == null)
            {
                _landingCollider = GetComponent<BoxCollider2D>();
            }
        }

        private void Update()
        {
            if (_moveSpeed <= 0f)
            {
                return;
            }

            var nextPosition = transform.position;
            nextPosition.x += _moveDirection * _moveSpeed * Time.deltaTime;

            if (nextPosition.x > _rightBound)
            {
                nextPosition.x = _rightBound;
                _moveDirection = -1;
            }
            else if (nextPosition.x < _leftBound)
            {
                nextPosition.x = _leftBound;
                _moveDirection = 1;
            }

            transform.position = nextPosition;
        }
    }
}
