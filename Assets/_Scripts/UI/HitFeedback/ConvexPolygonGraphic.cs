using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CTJ.UI
{
    // 순서대로 주어진 볼록 다각형을 기본 Canvas UI로 채웁니다. 피해 판정은 하지 않습니다.
    public sealed class ConvexPolygonGraphic : MaskableGraphic
    {
        private readonly List<Vector2> _points = new List<Vector2>(5);
        public void SetPoints(IReadOnlyList<Vector2> worldPoints, Vector2 center)
        {
            bool changed = _points.Count != worldPoints.Count;
            if (!changed)
                for (int i = 0; i < _points.Count; i++)
                    changed |= _points[i] != worldPoints[i] - center;
            if (!changed) return;
            _points.Clear();
            for (int i = 0; i < worldPoints.Count; i++) _points.Add(worldPoints[i] - center);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_points.Count < 3) return;
            foreach (Vector2 point in _points) vh.AddVert(point, color, Vector2.zero);
            for (int i = 1; i < _points.Count - 1; i++) vh.AddTriangle(0, i, i + 1);
        }
    }
}
