using System;
using JumJump.Controller;
using JumJump.Data;
using UnityEngine;

namespace JumJump.Factory
{
    public sealed class ComboPlatformFXFactory
    {
        private const int AuraSortingOrder = 4;
        private const int BurstSortingOrder = 6;
        private const int SparkleCount = 4;

        private readonly EffectConfigData _configData;

        public ComboPlatformFXFactory(EffectConfigData configData)
        {
            _configData = configData;
        }

        public Transform CreateRoot()
        {
            var root = new GameObject("FX_ComboPlatformPool");
            return root.transform;
        }

        public ComboPlatformAuraFX CreateAura(Transform parent, Action<ComboPlatformAuraFX> onReturnedToPool)
        {
            var gameObject = new GameObject("ComboPlatformAuraFX");
            gameObject.transform.SetParent(parent, false);
            var auraRenderer = CreateRenderer(
                gameObject.transform,
                "Aura",
                _configData.ComboPlatformAuraSprite,
                AuraSortingOrder);
            var fx = gameObject.AddComponent<ComboPlatformAuraFX>();
            fx.Bind(auraRenderer, onReturnedToPool);
            return fx;
        }

        public ComboPlatformBurstFX CreateBurst(Transform parent, Action<ComboPlatformBurstFX> onReturnedToPool)
        {
            var gameObject = new GameObject("ComboPlatformBurstFX");
            gameObject.transform.SetParent(parent, false);
            var ringRenderer = CreateRenderer(
                gameObject.transform,
                "PulseRing",
                _configData.ComboPlatformPulseSprite,
                BurstSortingOrder);
            var sparkleRenderers = new SpriteRenderer[SparkleCount];
            for (var i = 0; i < sparkleRenderers.Length; i++)
            {
                sparkleRenderers[i] = CreateRenderer(
                    gameObject.transform,
                    $"Sparkle_{i:00}",
                    _configData.ComboPlatformSparkleSprite,
                    BurstSortingOrder + 1);
            }

            var fx = gameObject.AddComponent<ComboPlatformBurstFX>();
            fx.Bind(ringRenderer, sparkleRenderers, onReturnedToPool);
            return fx;
        }

        private SpriteRenderer CreateRenderer(Transform parent, string name, Sprite sprite, int sortingOrder)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            var renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            renderer.color = new Color(1f, 1f, 1f, 0f);
            renderer.enabled = false;
            return renderer;
        }
    }
}
