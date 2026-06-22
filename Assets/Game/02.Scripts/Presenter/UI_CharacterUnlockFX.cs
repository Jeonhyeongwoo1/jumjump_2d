using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace JumJump.Presenter
{
    public sealed class UI_CharacterUnlockFX : MaskableGraphic
    {
        private const int RingSegmentCount = 48;
        private const int SparkleCount = 12;
        private const float FullCircleDegrees = 360f;

        [SerializeField] private float _duration = 0.72f;
        [SerializeField] private float _ringStartRadius = 38f;
        [SerializeField] private float _ringEndRadius = 158f;
        [SerializeField] private float _ringStartThickness = 13f;
        [SerializeField] private float _ringEndThickness = 3f;
        [SerializeField] private float _sparkleStartRadius = 26f;
        [SerializeField] private float _sparkleEndRadius = 140f;
        [SerializeField] private float _sparkleStartSize = 24f;
        [SerializeField] private float _sparkleEndSize = 8f;
        [SerializeField] private Color _ringColor = new Color(1f, 0.83f, 0.18f, 0.88f);
        [SerializeField] private Color _sparkleColor = new Color(1f, 0.96f, 0.42f, 0.95f);

        private Coroutine _playRoutine;
        private float _normalizedTime = 1f;

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        public void PlayAtCenter()
        {
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
            MoveToCenter();

            if (_playRoutine != null)
            {
                StopCoroutine(_playRoutine);
            }

            _normalizedTime = 0f;
            SetVerticesDirty();
            _playRoutine = StartCoroutine(PlayRoutine());
        }

        public void HideImmediate()
        {
            _normalizedTime = 1f;
            SetVerticesDirty();
            gameObject.SetActive(false);
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            if (_normalizedTime >= 1f)
            {
                return;
            }

            DrawRing(vertexHelper, _normalizedTime);
            DrawSparkles(vertexHelper, _normalizedTime);
        }

        private IEnumerator PlayRoutine()
        {
            var elapsed = 0f;
            var duration = Mathf.Max(0.01f, _duration);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                _normalizedTime = Mathf.Clamp01(elapsed / duration);
                SetVerticesDirty();
                yield return null;
            }

            _playRoutine = null;
            HideImmediate();
        }

        private void MoveToCenter()
        {
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = Vector3.one;
            var currentLocalPosition = rectTransform.localPosition;
            rectTransform.localPosition = new Vector3(currentLocalPosition.x, currentLocalPosition.y, 0f);
        }

        private void DrawRing(VertexHelper vertexHelper, float normalized)
        {
            var eased = EaseOutCubic(normalized);
            var radius = Mathf.Lerp(_ringStartRadius, _ringEndRadius, eased);
            var thickness = Mathf.Lerp(_ringStartThickness, _ringEndThickness, normalized);
            var halfThickness = thickness * 0.5f;
            var alpha = ResolveFadeAlpha(_ringColor.a, 0.25f, normalized);
            var drawColor = ResolveColor(_ringColor, alpha);

            for (var i = 0; i < RingSegmentCount; i++)
            {
                var startAngle = FullCircleDegrees * i / RingSegmentCount;
                var endAngle = FullCircleDegrees * (i + 1) / RingSegmentCount;
                var startDirection = ResolveDirection(startAngle);
                var endDirection = ResolveDirection(endAngle);

                AddQuad(
                    vertexHelper,
                    startDirection * (radius - halfThickness),
                    startDirection * (radius + halfThickness),
                    endDirection * (radius + halfThickness),
                    endDirection * (radius - halfThickness),
                    drawColor);
            }
        }

        private void DrawSparkles(VertexHelper vertexHelper, float normalized)
        {
            var eased = EaseOutCubic(normalized);
            var radius = Mathf.Lerp(_sparkleStartRadius, _sparkleEndRadius, eased);
            var size = Mathf.Lerp(_sparkleStartSize, _sparkleEndSize, normalized);
            var alpha = ResolveFadeAlpha(_sparkleColor.a, 0.15f, normalized);
            var drawColor = ResolveColor(_sparkleColor, alpha);

            for (var i = 0; i < SparkleCount; i++)
            {
                var angle = FullCircleDegrees * i / SparkleCount;
                var direction = ResolveDirection(angle);
                var center = direction * radius;
                var sparkleSize = size * ResolveSparkleScale(i);
                AddDiamond(vertexHelper, center, sparkleSize, drawColor);
            }
        }

        private float ResolveSparkleScale(int index)
        {
            return index % 3 == 0 ? 1.16f : 0.86f;
        }

        private Vector2 ResolveDirection(float angle)
        {
            var radians = angle * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        }

        private Color32 ResolveColor(Color sourceColor, float alpha)
        {
            sourceColor.a = alpha;
            return sourceColor;
        }

        private float ResolveFadeAlpha(float sourceAlpha, float fadeStart, float normalized)
        {
            if (normalized <= fadeStart)
            {
                return sourceAlpha;
            }

            var fadeProgress = Mathf.InverseLerp(fadeStart, 1f, normalized);
            return Mathf.Lerp(sourceAlpha, 0f, Mathf.SmoothStep(0f, 1f, fadeProgress));
        }

        private float EaseOutCubic(float normalized)
        {
            var inverted = 1f - normalized;
            return 1f - inverted * inverted * inverted;
        }

        private void AddDiamond(VertexHelper vertexHelper, Vector2 center, float size, Color32 drawColor)
        {
            var halfSize = size * 0.5f;
            AddQuad(
                vertexHelper,
                center + new Vector2(0f, halfSize),
                center + new Vector2(halfSize, 0f),
                center + new Vector2(0f, -halfSize),
                center + new Vector2(-halfSize, 0f),
                drawColor);
        }

        private void AddQuad(
            VertexHelper vertexHelper,
            Vector2 first,
            Vector2 second,
            Vector2 third,
            Vector2 fourth,
            Color32 drawColor)
        {
            var startIndex = vertexHelper.currentVertCount;
            AddVertex(vertexHelper, first, drawColor);
            AddVertex(vertexHelper, second, drawColor);
            AddVertex(vertexHelper, third, drawColor);
            AddVertex(vertexHelper, fourth, drawColor);
            vertexHelper.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
            vertexHelper.AddTriangle(startIndex, startIndex + 2, startIndex + 3);
        }

        private void AddVertex(VertexHelper vertexHelper, Vector2 position, Color32 drawColor)
        {
            var vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = drawColor;
            vertexHelper.AddVert(vertex);
        }
    }
}
