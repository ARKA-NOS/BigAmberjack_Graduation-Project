using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace _Scripts.UI.Minimap
{
    // 타일맵들의 지형을 미니맵용 픽셀 배열로 칠한다. 어떤 타일맵을 칠할지는 IMinimapSource가 고른다.
    public static class MinimapTerrainBaker
    {
        // 타일이 있는 칸을 모두 감싸는 월드 범위. 타일이 하나도 없으면 false.
        public static bool TryGetWorldRect(IReadOnlyList<Tilemap> tilemaps, out Rect worldRect)
        {
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            bool hasTile = false;

            foreach (Tilemap tilemap in tilemaps)
            {
                tilemap.CompressBounds();
                BoundsInt cells = tilemap.cellBounds;

                if (cells.size.x <= 0 || cells.size.y <= 0)
                    continue;

                Vector2 a = tilemap.CellToWorld(cells.min);
                Vector2 b = tilemap.CellToWorld(cells.max);
                min = Vector2.Min(min, Vector2.Min(a, b));
                max = Vector2.Max(max, Vector2.Max(a, b));
                hasTile = true;
            }

            worldRect = hasTile ? Rect.MinMaxRect(min.x, min.y, max.x, max.y) : default;
            return hasTile;
        }

        // 아래 줄부터 x 순서인 width * height 배열. 타일이 없는 곳은 투명하다.
        public static Color32[] Paint(IReadOnlyList<Tilemap> tilemaps, Rect worldRect, int width, int height,
            int pixelsPerUnit, Color32 terrainColor)
        {
            Color32[] pixels = new Color32[width * height];

            foreach (Tilemap tilemap in tilemaps)
            {
                foreach (Vector3Int cell in tilemap.cellBounds.allPositionsWithin)
                {
                    if (!tilemap.HasTile(cell))
                        continue;

                    Vector2 min = (Vector2)tilemap.CellToWorld(cell) - worldRect.min;
                    Vector2 max = (Vector2)tilemap.CellToWorld(cell + new Vector3Int(1, 1, 0)) - worldRect.min;
                    FillRect(pixels, width, height, Vector2.Min(min, max) * pixelsPerUnit, Vector2.Max(min, max) * pixelsPerUnit, terrainColor);
                }
            }

            return pixels;
        }

        private static void FillRect(Color32[] pixels, int width, int height, Vector2 min, Vector2 max, Color32 color)
        {
            int xMin = Mathf.Clamp(Mathf.FloorToInt(min.x), 0, width);
            int xMax = Mathf.Clamp(Mathf.CeilToInt(max.x), 0, width);
            int yMin = Mathf.Clamp(Mathf.FloorToInt(min.y), 0, height);
            int yMax = Mathf.Clamp(Mathf.CeilToInt(max.y), 0, height);

            for (int y = yMin; y < yMax; y++)
            {
                for (int x = xMin; x < xMax; x++)
                    pixels[y * width + x] = color;
            }
        }
    }
}
