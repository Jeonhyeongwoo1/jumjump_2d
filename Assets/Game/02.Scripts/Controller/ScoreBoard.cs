using UnityEngine;

namespace JumJump.Controller
{
    public sealed class ScoreBoard : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;

        public void Show(Vector3 worldPosition, Sprite sprite)
        {
            transform.position = worldPosition;
            _spriteRenderer.sprite = sprite;
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
