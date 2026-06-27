using UnityEngine;
using UnityEngine.Events;

namespace JumJump.Controller
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class CoinPickup : MonoBehaviour
    {
        [SerializeField] private CoinCollectFX _collectFXPrefab;
        [SerializeField] private CoinCollectFX _collectFXInScene;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private LayerMask _playerLayer;
        [SerializeField] private string _playerTag = "Player";
        [SerializeField] private int _goldAmount = 1;
        [SerializeField] private bool _usePlayerLayer = true;
        [SerializeField] private bool _usePlayerTag = true;
        [SerializeField] private bool _hideSpriteRendererOnCollect = true;
        [SerializeField] private bool _destroyOnCollect;
        [SerializeField] private UnityEvent _onCollected = new UnityEvent();
        [SerializeField] private UnityEvent<int> _onGoldCollected = new UnityEvent<int>();

        private bool _collected;

        public void ResetPickup()
        {
            _collected = false;

            if (_spriteRenderer != null)
            {
                _spriteRenderer.enabled = true;
            }

            gameObject.SetActive(true);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryCollect(other.gameObject);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TryCollect(collision.gameObject);
        }

        private void TryCollect(GameObject other)
        {
            if (_collected || !IsPlayer(other))
            {
                return;
            }

            _collected = true;
            PlayCollectFX();
            _onCollected.Invoke();
            _onGoldCollected.Invoke(_goldAmount);

            if (_hideSpriteRendererOnCollect && _spriteRenderer != null)
            {
                _spriteRenderer.enabled = false;
            }

            if (_destroyOnCollect)
            {
                Destroy(gameObject);
                return;
            }

            gameObject.SetActive(false);
        }

        private bool IsPlayer(GameObject other)
        {
            if (other == null)
            {
                return false;
            }

            if (_usePlayerLayer && ((_playerLayer.value & (1 << other.layer)) != 0))
            {
                return true;
            }

            return _usePlayerTag && !string.IsNullOrEmpty(_playerTag) && other.CompareTag(_playerTag);
        }

        private void PlayCollectFX()
        {
            if (_collectFXPrefab != null)
            {
                var fx = Instantiate(_collectFXPrefab);
                fx.Play(transform.position);
                return;
            }

            if (_collectFXInScene != null)
            {
                _collectFXInScene.Play(transform.position);
            }
        }
    }
}
