using System.Collections.Generic;
using _Scripts._Core._Debug;
using _Scripts.Maps.New_Map.MapGenerateData;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace _Scripts.Maps.New_Map.MapSettingAsset
{
    [CreateAssetMenu(fileName = "MapModule", menuName = "MapSystem/MapModule", order = 0)]
    public class RoomSO : ScriptableObject
    {
        [field:SerializeField] public Room RoomPrefab { get; private set; }
        [field:SerializeField] public float Priority { get; private set; }
        
        private void OnValidate()
        {
            if (RoomPrefab == null)
            {
                FDebug.LogWarning("[Room SO] RoomPrefab is null", this);
            }
        }
        
        #region Rect Bake
        
        public List<RectInt> GetBounds(Vector2Int position)
        {
            Tilemap tileMap = RoomPrefab.GetTileMap();
            
            HashSet<Vector3Int> hashSet = new HashSet<Vector3Int>();
            
            BoundsInt space = tileMap.cellBounds;
            for (int x = space.xMin; x <= space.xMax; x++)
            {
                for (int y = space.yMin; y <= space.yMax; y++)
                {
                    if (!tileMap.HasTile(new Vector3Int(x, y, 0))) continue;
                    
                    hashSet.Add(new Vector3Int(x, y, 0) + (Vector3Int)position);
                }
            }
            
            return MergeIntoRectangles(hashSet);
        }
        
        private static List<RectInt> MergeIntoRectangles(HashSet<Vector3Int> walkable)
        {
            HashSet<Vector3Int> visited = new HashSet<Vector3Int>();
            List<RectInt> rects   = new List<RectInt>();

            List<Vector3Int> sorted = new List<Vector3Int>(walkable);
            //Y축 우선 오름차순 정렬.
            sorted.Sort((a, b) => a.y != b.y ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));

            foreach (Vector3Int origin in sorted)
            {
                if (visited.Contains(origin)) continue;

                // 오른쪽으로 최대 확장
                int w = 1;
                while (walkable.Contains(new Vector3Int(origin.x + w, origin.y, 0)) &&
                       !visited.Contains(new Vector3Int(origin.x + w, origin.y, 0)))
                    w++;

                // 위쪽으로 최대 확장 (행 전체가 빈 walkable 셀이어야 함)
                int h = 1;
                while (IsRowAvailable(walkable, visited, origin.x, origin.y + h, w))
                    h++;

                var rect = new RectInt(origin.x, origin.y, w, h);
                rects.Add(rect);

                //해당 사각형에 있는 모든 점을 더한다.
                for (int dx = 0; dx < w; dx++)
                for (int dy = 0; dy < h; dy++)
                    visited.Add(new Vector3Int(origin.x + dx, origin.y + dy, 0));
            }

            return rects;
        }
        
        private static bool IsRowAvailable(HashSet<Vector3Int> walkable, HashSet<Vector3Int> visited, int startX, int y, int width)
        {
            for (int dx = 0; dx < width; dx++)
            {
                Vector3Int cell = new Vector3Int(startX + dx, y, 0);
                if (!walkable.Contains(cell) || visited.Contains(cell)) return false;
            }
            return true;
        }
        
        #endregion
        
        #region Link Bake

        public List<RoomLinkData> GetLinks(Vector2 position)
        {
            List<RoomLinkData> links = new();
            
            RoomLink[] roomLinks = RoomPrefab.GetRoomLink();
            
            foreach (RoomLink roomLink in roomLinks)
            {
                if(roomLink == null) continue;
                Vector2 localPos = roomLink.transform.position - (Vector3)position;
                links.Add(new RoomLinkData(localPos, roomLink.Direction, roomLink.LinkSize));
            }
            
            return links;
        }

        #endregion
        
    }
}