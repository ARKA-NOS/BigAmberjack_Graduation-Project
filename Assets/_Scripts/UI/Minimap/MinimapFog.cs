using UnityEngine;

namespace _Scripts.UI.Minimap
{
    // 전장의 안개. 투명한 텍스처에서 시작해 밝힌 원 안의 지형만 옮겨 칠한다. 한 번 밝힌 곳은 계속 보인다.
    public class MinimapFog
    {
        private readonly Color32[] _terrain;
        private readonly Color32[] _visible;
        private readonly Texture2D _texture;
        private Vector2Int? _lastCenter;

        // texture는 terrain과 같은 크기이고, 계속 고칠 수 있어야 한다(읽기 가능 상태 유지).
        public MinimapFog(Color32[] terrain, Texture2D texture)
        {
            _terrain = terrain;
            _visible = new Color32[terrain.Length];
            _texture = texture;

            Upload();
        }

        // center와 radius는 텍스처 픽셀 단위. 같은 칸에 머무는 동안에는 아무것도 하지 않는다.
        public void Reveal(Vector2Int center, int radius)
        {
            if (_lastCenter == center) return;
            _lastCenter = center;

            int width = _texture.width;
            int xMin = Mathf.Max(0, center.x - radius);
            int xMax = Mathf.Min(width - 1, center.x + radius);
            int yMin = Mathf.Max(0, center.y - radius);
            int yMax = Mathf.Min(_texture.height - 1, center.y + radius);
            bool changed = false;

            for (int y = yMin; y <= yMax; y++)
            {
                for (int x = xMin; x <= xMax; x++)
                {
                    int dx = x - center.x;
                    int dy = y - center.y;
                    if (dx * dx + dy * dy > radius * radius) continue;

                    int index = y * width + x;
                    if (_visible[index].a == _terrain[index].a) continue;

                    _visible[index] = _terrain[index];
                    changed = true;
                }
            }

            if (changed)
                Upload();
        }

        private void Upload()
        {
            _texture.SetPixels32(_visible);
            _texture.Apply(false);
        }
    }
}
