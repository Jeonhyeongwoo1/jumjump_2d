using JumJump.Util;
using UnityEngine;

namespace JumJump.Controller
{
    internal struct PlatformMotion
    {
        public float MoveDirectionX => _moveDirectionX;

        private float _targetX;
        private float _moveSpeed;
        private float _pausedMoveSpeed;
        private float _moveDirectionX;
        private bool _hasPausedMoveSpeed;

        public void Configure(float spawnX, float targetX, float moveSpeed)
        {
            _targetX = targetX;
            _moveSpeed = moveSpeed;
            _moveDirectionX = ResolveMoveDirectionX(spawnX, targetX);
            _pausedMoveSpeed = 0f;
            _hasPausedMoveSpeed = false;
        }

        public bool HasReachedTarget(float currentX)
        {
            return Mathf.Abs(currentX - _targetX) <= GameConst.Platform.MoveTargetEpsilon;
        }

        public void MoveTowardTarget(Transform rootTransform, Rigidbody2D rigidbody, float deltaTime)
        {
            if (_moveSpeed <= 0f)
            {
                return;
            }

            var nextPosition = rootTransform.position;
            nextPosition.x = Mathf.MoveTowards(nextPosition.x, _targetX, _moveSpeed * deltaTime);
            rigidbody.MovePosition(nextPosition);
        }

        public void ApplySpeedScale(float moveSpeedScale)
        {
            _moveSpeed *= Mathf.Max(0f, moveSpeedScale);
        }

        public void Pause()
        {
            if (_hasPausedMoveSpeed)
            {
                return;
            }

            _pausedMoveSpeed = _moveSpeed;
            _moveSpeed = 0f;
            _hasPausedMoveSpeed = true;
        }

        public void Resume()
        {
            if (!_hasPausedMoveSpeed)
            {
                return;
            }

            _moveSpeed = _pausedMoveSpeed;
            _pausedMoveSpeed = 0f;
            _hasPausedMoveSpeed = false;
        }

        public void Stop()
        {
            _moveSpeed = 0f;
        }

        private float ResolveMoveDirectionX(float spawnX, float targetX)
        {
            var deltaX = targetX - spawnX;
            if (Mathf.Approximately(deltaX, 0f))
            {
                return 0f;
            }

            return Mathf.Sign(deltaX);
        }
    }
}
