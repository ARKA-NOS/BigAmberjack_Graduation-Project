using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.UI.Enemy
{
    // 텍스처 없이 원판과 두 개의 열린 호를 하나의 UI 메시로 그립니다.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class DroneShieldGraphic : MaskableGraphic
    {
        [SerializeField] private Color shieldColor = new Color(0.35f, 0.12f, 0.65f, 0.22f);
        [SerializeField] private Color ringColor = new Color(0.4f, 0.8f, 1f, 0.85f);
        [SerializeField, Range(0.5f, 0.95f)] private float innerRingRatio = 0.86f;
        [SerializeField, Min(0.5f)] private float lineWidth = 1.5f;
        [SerializeField, Range(30f, 350f)] private float innerArcDegrees = 250f;
        [SerializeField, Range(30f, 350f)] private float outerArcDegrees = 250f;
        [SerializeField, Range(24, 128)] private int segments = 96;

        private float _centerOpacity, _innerOpacity, _outerOpacity;
        private float _innerAngle, _outerAngle;

        internal void SetPresentation(float center, float inner, float outer, float innerAngle, float outerAngle)
        {
            _centerOpacity = Mathf.Clamp01(center);
            _innerOpacity = Mathf.Clamp01(inner);
            _outerOpacity = Mathf.Clamp01(outer);
            _innerAngle = innerAngle;
            _outerAngle = outerAngle;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = GetPixelAdjustedRect();
            Vector2 center = rect.center;
            float radius = Mathf.Min(rect.width, rect.height) * 0.5f;
            AddDisk(vh, center, radius * (innerRingRatio - 0.04f));
            AddArc(vh, center, radius * innerRingRatio, innerArcDegrees, _innerAngle, _innerOpacity);
            AddArc(vh, center, radius, outerArcDegrees, _outerAngle, _outerOpacity);
        }

        private void AddDisk(VertexHelper vh, Vector2 center, float radius)
        {
            if (_centerOpacity <= 0f) return;

            Color fill = shieldColor;
            fill.a *= _centerOpacity;
            int start = vh.currentVertCount;
            vh.AddVert(center, fill, Vector2.zero);
            for (int i = 0; i <= segments; i++)
            {
                float angle = i * 2f * Mathf.PI / segments;
                vh.AddVert(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, fill, Vector2.zero);
                if (i > 0) vh.AddTriangle(start, start + i, start + i + 1);
            }
        }

        private void AddArc(VertexHelper vh, Vector2 center, float radius, float degrees, float rotation, float opacity)
        {
            if (opacity <= 0f) return;

            Color tint = ringColor;
            tint.a *= opacity;
            int start = vh.currentVertCount;
            float arc = Mathf.Clamp(degrees, 1f, 350f);
            int steps = Mathf.Max(2, Mathf.CeilToInt(segments * arc / 360f));
            for (int i = 0; i <= steps; i++)
            {
                float angle = (rotation + arc * i / steps) * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                vh.AddVert(center + direction * radius, tint, Vector2.zero);
                vh.AddVert(center + direction * Mathf.Max(0f, radius - lineWidth), tint, Vector2.zero);
                if (i == 0) continue;

                int edge = start + (i - 1) * 2;
                vh.AddTriangle(edge, edge + 2, edge + 1);
                vh.AddTriangle(edge + 1, edge + 2, edge + 3);
            }
        }
    }
}
