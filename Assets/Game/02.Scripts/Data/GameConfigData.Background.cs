using UnityEngine;

namespace JumJump.Data
{
    public sealed partial class GameConfigData
    {
        public float BackgroundGradientMinHeight => _backgroundGradientMinHeight;
        public float BackgroundGradientMaxHeight => _backgroundGradientMaxHeight;
        public Color BackgroundLowBottomColor => _backgroundLowBottomColor;
        public Color BackgroundLowTopColor => _backgroundLowTopColor;
        public Color BackgroundHighBottomColor => _backgroundHighBottomColor;
        public Color BackgroundHighTopColor => _backgroundHighTopColor;
        public Color BackgroundHighObjectTint => _backgroundHighObjectTint;
        public float BackgroundGradientUpdateThreshold => _backgroundGradientUpdateThreshold;
        public float BackgroundGradientScreenPadding => _backgroundGradientScreenPadding;
        public float BackgroundGradientZ => _backgroundGradientZ;
        public int BackgroundGradientSortingOrder => _backgroundGradientSortingOrder;
        public int BackgroundObjectSortingOrder => _backgroundObjectSortingOrder;
        public int BackgroundObjectPoolCount => _backgroundObjectPoolCount;
        public float BackgroundObjectXRange => _backgroundObjectXRange;
        public float BackgroundObjectRecycleBelowY => _backgroundObjectRecycleBelowY;
        public float BackgroundObjectSpawnAheadY => _backgroundObjectSpawnAheadY;
        public float BackgroundObjectMinScale => _backgroundObjectMinScale;
        public float BackgroundObjectMaxScale => _backgroundObjectMaxScale;
        public float BackgroundObjectMinAlpha => _backgroundObjectMinAlpha;
        public float BackgroundObjectMaxAlpha => _backgroundObjectMaxAlpha;
        public float BackgroundObjectMaxRotation => _backgroundObjectMaxRotation;
        public float BackgroundObjectDriftAmplitude => _backgroundObjectDriftAmplitude;
        public float BackgroundObjectMinDriftSpeed => _backgroundObjectMinDriftSpeed;
        public float BackgroundObjectMaxDriftSpeed => _backgroundObjectMaxDriftSpeed;
        public int BackgroundObjectAlphaFromDepthIndex => _backgroundObjectAlphaFromDepthIndex;
        public float BackgroundObjectMinParallax => _backgroundObjectMinParallax;
        public float BackgroundObjectMaxParallax => _backgroundObjectMaxParallax;
        public float BackgroundObjectZ => _backgroundObjectZ;
        public BackgroundDepthLayer[] BackgroundDepthLayers => _backgroundDepthLayers;

        [Header("Background Gradient")]
        [SerializeField] private float _backgroundGradientMinHeight = -2f;
        [SerializeField] private float _backgroundGradientMaxHeight = 80f;
        [SerializeField] private Color _backgroundLowBottomColor = new Color(0.47f, 0.82f, 1f, 1f);
        [SerializeField] private Color _backgroundLowTopColor = new Color(0.18f, 0.55f, 0.95f, 1f);
        [SerializeField] private Color _backgroundHighBottomColor = new Color(0.34f, 0.22f, 0.75f, 1f);
        [SerializeField] private Color _backgroundHighTopColor = new Color(0.1f, 0.05f, 0.32f, 1f);
        [SerializeField] private Color _backgroundHighObjectTint = new Color(0.86f, 0.78f, 1f, 1f);
        [SerializeField] private float _backgroundGradientUpdateThreshold = 0.005f;
        [SerializeField] private float _backgroundGradientScreenPadding = 1.15f;
        [SerializeField] private float _backgroundGradientZ = 10f;
        [SerializeField] private int _backgroundGradientSortingOrder = -1000;

        [Header("Background Objects")]
        [SerializeField] private int _backgroundObjectSortingOrder = -900;
        [SerializeField] private int _backgroundObjectPoolCount = 12;
        [SerializeField] private float _backgroundObjectXRange = 3.2f;
        [SerializeField] private float _backgroundObjectRecycleBelowY = 8f;
        [SerializeField] private float _backgroundObjectSpawnAheadY = 11f;
        [SerializeField] private float _backgroundObjectMinScale = 0.35f;
        [SerializeField] private float _backgroundObjectMaxScale = 1.3f;
        [SerializeField] private float _backgroundObjectMinAlpha = 0.3f;
        [SerializeField] private float _backgroundObjectMaxAlpha = 0.7f;
        [SerializeField] private float _backgroundObjectMaxRotation = 30f;
        [SerializeField] private float _backgroundObjectDriftAmplitude = 0.4f;
        [SerializeField] private float _backgroundObjectMinDriftSpeed = 0.3f;
        [SerializeField] private float _backgroundObjectMaxDriftSpeed = 0.8f;
        [SerializeField] private int _backgroundObjectAlphaFromDepthIndex = 1;
        [SerializeField] private float _backgroundObjectMinParallax = 0.3f;
        [SerializeField] private float _backgroundObjectMaxParallax = 0.75f;
        [SerializeField] private float _backgroundObjectZ = 9f;
        [SerializeField] private BackgroundDepthLayer[] _backgroundDepthLayers;
    }
}
