using JumJump.Util;
using UnityEngine;

namespace JumJump.Controller
{
    internal struct PlayerShieldRuntime
    {
        private bool _hasShield;
        private bool _isBreakAnimating;
        private float _breakElapsed;
        private Sprite _idleSprite;

        public bool HasShield => _hasShield;

        public void CacheIdleSprite(Sprite sprite)
        {
            _idleSprite = sprite;
        }

        public void Reset(GameObject visualRoot, SpriteRenderer spriteRenderer)
        {
            _hasShield = false;
            Hide(visualRoot, spriteRenderer);
        }

        public void Grant(
            GameObject visualRoot,
            SpriteRenderer spriteRenderer,
            Animator animator,
            int idleStateHash)
        {
            _hasShield = true;
            _isBreakAnimating = false;
            _breakElapsed = 0f;
            spriteRenderer.sprite = _idleSprite;
            visualRoot.SetActive(true);
            animator.Play(idleStateHash, 0, 0f);
        }

        public bool TryBlock(GameObject visualRoot, Animator animator, int breakStateHash)
        {
            if (!_hasShield)
            {
                return false;
            }

            _hasShield = false;
            _isBreakAnimating = true;
            _breakElapsed = 0f;
            visualRoot.SetActive(true);
            animator.Play(breakStateHash, 0, 0f);
            return true;
        }

        public void TickBreakAnimation(float deltaTime, GameObject visualRoot, SpriteRenderer spriteRenderer)
        {
            if (!_isBreakAnimating)
            {
                return;
            }

            _breakElapsed += deltaTime;
            if (_breakElapsed >= GameConst.Player.ShieldBreakAnimationDuration)
            {
                Hide(visualRoot, spriteRenderer);
            }
        }

        private void Hide(GameObject visualRoot, SpriteRenderer spriteRenderer)
        {
            _isBreakAnimating = false;
            _breakElapsed = 0f;
            spriteRenderer.sprite = _idleSprite;
            visualRoot.SetActive(false);
        }
    }
}
