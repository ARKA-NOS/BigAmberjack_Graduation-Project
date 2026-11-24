using _Scripts.Agent.Enemies;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.UI.Enemy
{
    // 고정된 착탄점의 지면 타원·십자와 비행 진행률에 맞춰 줄어드는 원을 한 Canvas에 그립니다.
    public sealed class DroneBombTargetGraphic : MaskableGraphic
    {
        [SerializeField, Min(0.005f)] private float lineWidth = 0.025f;
        [SerializeField, Range(0.05f, 1f)] private float ellipseHeightRatio = 0.2f;
        [SerializeField, Min(0.05f)] private float crossHeight = 0.65f;
        [SerializeField, Min(0.01f)] private float crossHalfSize = 0.16f;
        [SerializeField, Min(0.05f)] private float countdownRadius = 0.48f;
        [SerializeField, Range(0f, 1f)] private float fillOpacity = 0.1f;

        private EnemyLobProjectile _projectile;
        private float _radius;
        private float _progress;

        private const int Segments = 48;

        public float CountdownProgress => _progress;

        public void Bind(EnemyLobProjectile projectile, Vector2 point, float radius)
        {
            _projectile = projectile;
            _radius = Mathf.Max(0.05f, radius);
            transform.SetParent(projectile.transform, false);
            transform.position = new Vector3(point.x, point.y, projectile.transform.position.z);
            transform.rotation = Quaternion.identity;
            Vector3 scale = projectile.transform.lossyScale;

            transform.localScale = new Vector3(1f / Mathf.Max(0.0001f, Mathf.Abs(scale.x)),
                1f / Mathf.Max(0.0001f, Mathf.Abs(scale.y)), 1f);
            rectTransform.sizeDelta = Vector2.one * (_radius * 2f + 1f);
            raycastTarget = false;
            _progress = 0f;
            SetVerticesDirty();
        }

        private void Update()
        {
            if (_projectile == null || !_projectile.IsInFlight)
            {
                _progress = 1f;
                gameObject.SetActive(false);

                return;
            }

            float progress = _projectile.FlightProgress;
            if (Mathf.Approximately(_progress, progress)) return;

            _progress = progress;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Color fill = color;

            fill.a *= fillOpacity;

            for (int i = 0; i < Segments; i++)
            {
                float a = i * 2f * Mathf.PI / Segments;
                float b = (i + 1) * 2f * Mathf.PI / Segments;
                int first = vh.currentVertCount;

                vh.AddVert(Vector2.zero, fill, Vector2.zero);
                vh.AddVert(new Vector2(Mathf.Cos(a) * _radius, Mathf.Sin(a) * _radius * ellipseHeightRatio), fill, Vector2.zero);
                vh.AddVert(new Vector2(Mathf.Cos(b) * _radius, Mathf.Sin(b) * _radius * ellipseHeightRatio), fill, Vector2.zero);
                vh.AddTriangle(first, first + 1, first + 2);
            }

            Ring(vh, Vector2.zero, _radius, _radius * ellipseHeightRatio);
            Vector2 cross = Vector2.up * crossHeight;

            Quad(vh, cross, new Vector2(crossHalfSize * 2f, lineWidth));
            Quad(vh, cross, new Vector2(lineWidth, crossHalfSize * 2f));
            float remainingRadius = countdownRadius * (1f - _progress);

            if (remainingRadius > 0f) Ring(vh, cross, remainingRadius, remainingRadius);
        }

        private void Ring(VertexHelper vh, Vector2 center, float xRadius, float yRadius)
        {
            float half = lineWidth * 0.5f;

            for (int i = 0; i < Segments; i++)
            {
                float a = i * 2f * Mathf.PI / Segments;
                float b = (i + 1) * 2f * Mathf.PI / Segments;
                Vector2 va = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                Vector2 vb = new Vector2(Mathf.Cos(b), Mathf.Sin(b));
                int first = vh.currentVertCount;

                vh.AddVert(center + Vector2.Scale(va, new Vector2(xRadius + half, yRadius + half)), color, Vector2.zero);
                vh.AddVert(center + Vector2.Scale(va, new Vector2(Mathf.Max(0f, xRadius - half), Mathf.Max(0f, yRadius - half))), color, Vector2.zero);
                vh.AddVert(center + Vector2.Scale(vb, new Vector2(Mathf.Max(0f, xRadius - half), Mathf.Max(0f, yRadius - half))), color, Vector2.zero);
                vh.AddVert(center + Vector2.Scale(vb, new Vector2(xRadius + half, yRadius + half)), color, Vector2.zero);
                vh.AddTriangle(first, first + 1, first + 2);
                vh.AddTriangle(first, first + 2, first + 3);
            }
        }

        private void Quad(VertexHelper vh, Vector2 center, Vector2 size)
        {
            Vector2 half = size * 0.5f;
            int first = vh.currentVertCount;

            vh.AddVert(center + new Vector2(-half.x, -half.y), color, Vector2.zero);
            vh.AddVert(center + new Vector2(-half.x, half.y), color, Vector2.zero);
            vh.AddVert(center + new Vector2(half.x, half.y), color, Vector2.zero);
            vh.AddVert(center + new Vector2(half.x, -half.y), color, Vector2.zero);
            vh.AddTriangle(first, first + 1, first + 2);
            vh.AddTriangle(first, first + 2, first + 3);
        }
    }
}
