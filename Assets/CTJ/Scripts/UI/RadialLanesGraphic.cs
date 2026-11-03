using UnityEngine;
using UnityEngine.UI;

namespace CTJ.UI
{
    // 한 Canvas 안에 탄환 수만큼 사각 경고를 그립니다. 공용 경고의 알파/점멸을 그대로 사용합니다.
    public sealed class RadialLanesGraphic : MaskableGraphic
    {
        private int _count;
        private float _distance;
        private float _width;

        public void SetLanes(int count, float distance, float width)
        {
            count = Mathf.Max(1, count);
            distance = Mathf.Max(0f, distance);
            width = Mathf.Max(0.01f, width);
            if (_count == count && _distance == distance && _width == width) return;
            _count = count; _distance = distance; _width = width;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            for (int i = 0; i < _count; i++)
            {
                float angle = 2f * Mathf.PI * i / _count;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Vector2 side = new Vector2(-direction.y, direction.x) * (_width * 0.5f);
                Vector2 start = -direction * (_width * 0.5f);
                Vector2 end = direction * (_distance + _width * 0.5f);
                int first = vh.currentVertCount;
                vh.AddVert(start - side, color, Vector2.zero);
                vh.AddVert(start + side, color, Vector2.zero);
                vh.AddVert(end + side, color, Vector2.zero);
                vh.AddVert(end - side, color, Vector2.zero);
                vh.AddTriangle(first, first + 1, first + 2);
                vh.AddTriangle(first, first + 2, first + 3);
            }
        }
    }
}
