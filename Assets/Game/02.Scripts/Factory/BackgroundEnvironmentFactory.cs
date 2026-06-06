using UnityEngine;

namespace JumJump.Factory
{
    public sealed class BackgroundEnvironmentFactory
    {
        private const int GradientTextureWidth = 32;
        private const int GradientTextureHeight = 32;

        public Transform CreateRoot(string name)
        {
            var root = new GameObject(name);
            return root.transform;
        }

        public SpriteRenderer CreateSpriteRenderer(string name, Transform parent, int sortingOrder)
        {
            var instance = new GameObject(name);
            var instanceTransform = instance.transform;
            instanceTransform.SetParent(parent, false);

            var spriteRenderer = instance.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = sortingOrder;
            return spriteRenderer;
        }

        public Sprite CreateGradientSprite()
        {
            var texture = new Texture2D(GradientTextureWidth, GradientTextureHeight, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            return Sprite.Create(
                texture,
                new Rect(0f, 0f, GradientTextureWidth, GradientTextureHeight),
                new Vector2(0.5f, 0.5f),
                GradientTextureHeight);
        }
    }
}
