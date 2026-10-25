using UnityEngine;
using UnityEngine.Tilemaps;

namespace SDW.Scripts.Maps
{
    // 방 안 타일맵의 실제 타일 범위를 합친 월드 범위. 카메라 범위와 배경 크기가 같이 쓴다.
    public static class TilemapBoundsUtility
    {
        public static bool TryGetWorldBounds(Transform room, out Bounds bounds)
        {
            bool hasTile = false;
            bounds = default;

            foreach (Tilemap tilemap in room.GetComponentsInChildren<Tilemap>(true))
            {
                tilemap.CompressBounds();
                BoundsInt cells = tilemap.cellBounds;

                if (cells.size.x <= 0 || cells.size.y <= 0)
                    continue;

                Bounds tilemapBounds = new Bounds(tilemap.CellToWorld(cells.min), Vector3.zero);
                tilemapBounds.Encapsulate(tilemap.CellToWorld(cells.max));

                if (hasTile)
                    bounds.Encapsulate(tilemapBounds);
                else
                    bounds = tilemapBounds;

                hasTile = true;
            }

            return hasTile;
        }
    }
}
