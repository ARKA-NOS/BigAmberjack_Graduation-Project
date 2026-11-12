using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.UI.Enemy
{
    // 착지 피해 범위를 Canvas에 원으로 그립니다.
    public sealed class EnemyLeapLandingGraphic : MaskableGraphic
    {
        private const int Segments = 40;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = rectTransform.rect;
            float radius = Mathf.Min(rect.width, rect.height) * 0.5f;
            if (radius <= 0f) return;
            Vector2 center = rect.center;
            Color32 tint = color;
            vh.AddVert(center, tint, Vector2.zero);
            for (int i = 0; i <= Segments; i++)
            {
                float angle = i * Mathf.PI * 2f / Segments;
                vh.AddVert(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius,
                    tint, Vector2.zero);
                if (i > 0) vh.AddTriangle(0, i, i + 1);
            }
        }
    }
}
