using UnityEngine;
using UnityEngine.Tilemaps;

namespace _Scripts.UI.Minimap
{
    // 방의 지형을 미니맵용 텍스처 한 장으로 굽는다.
    // 장식 타일은 빼고, 플레이어가 밟거나 막히는 타일(TilemapCollider2D가 붙은 타일맵)만 칠한다.
    public static class MinimapTerrainBaker
    {
        public static Texture2D Bake(Transform room, Rect worldRect, int pixelsPerUnit, Color32 terrainColor)
        {
            int width = Mathf.Max(1, Mathf.CeilToInt(worldRect.width * pixelsPerUnit));
            int height = Mathf.Max(1, Mathf.CeilToInt(worldRect.height * pixelsPerUnit));
            Color32[] pixels = new Color32[width * height];

            foreach (Tilemap tilemap in room.GetComponentsInChildren<Tilemap>(true))
            {
                if (!tilemap.TryGetComponent(out TilemapCollider2D _))
                    continue;

                foreach (Vector3Int cell in tilemap.cellBounds.allPositionsWithin)
                {
                    if (!tilemap.HasTile(cell))
                        continue;

                    Vector2 min = (Vector2)tilemap.CellToWorld(cell) - worldRect.min;
                    Vector2 max = (Vector2)tilemap.CellToWorld(cell + new Vector3Int(1, 1, 0)) - worldRect.min;
                    FillRect(pixels, width, height, Vector2.Min(min, max) * pixelsPerUnit, Vector2.Max(min, max) * pixelsPerUnit, terrainColor);
                }
            }

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = $"{room.name}_Minimap",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);

            return texture;
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
