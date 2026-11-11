using System.Collections.Generic;
using _Scripts._Core._Debug;
using _Scripts._Core._EventSystem;
using _Scripts.Maps.New_Map.MapGenerateData;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace _Scripts.Maps.New_Map
{
    /*
     * 맵 생성이 끝나면 방 사이 빈 공간을 벽 타일로 채운다.
     * - 방 프리팹의 tilemap(배치 겹침 검사에 쓰는 방 영역)에 타일이 있는 칸을 방 칸으로 본다.
     * - 모든 방 칸을 감싸는 사각형을 padding칸만큼 넓히고, 그 안에서 방 칸이 아닌 칸을 모두 채운다.
     *   그래서 방 사이 틈뿐 아니라 맵 바깥 가장자리에도 벽이 생긴다.
     * - 생성기는 이 컴포넌트를 모른다. MapGeneratedEvent만 구독하므로 기본 맵(Fallback)에도 똑같이 채운다.
     *
     * 사용법: MapGenerator가 있는 Grid 오브젝트 아래에 벽 전용 Tilemap을 만들고 이 컴포넌트를 붙인다.
     *         충돌이 필요하면 같은 오브젝트에 TilemapCollider2D(+ CompositeCollider2D)를 붙인다.
     */
    [RequireComponent(typeof(Tilemap))]
    public class MapWallFiller : MonoBehaviour
    {
        [SerializeField] private TileBase wallTile;
        // 방 칸을 감싸는 사각형 바깥으로 더 채울 칸 수
        [SerializeField, Min(0)] private int padding = 5;

        private Tilemap _wallTilemap;

        private void Awake()
        {
            _wallTilemap = GetComponent<Tilemap>();
        }

        private void OnEnable()
        {
            EventBus<MapGeneratedEvent>.Subscribe(HandleMapGenerated);
        }

        private void OnDisable()
        {
            EventBus<MapGeneratedEvent>.UnSubscribe(HandleMapGenerated);
        }

        private void HandleMapGenerated(MapGeneratedEvent evt)
            => Fill(evt.Rooms);

        public void Fill(IReadOnlyList<GeneratedRoom> rooms)
        {
            // 다시 생성한 맵이면 이전 벽부터 지운다.
            _wallTilemap.ClearAllTiles();

            if (wallTile == null)
            {
                FDebug.LogWarning("[MapWallFiller] wallTile이 없어 벽을 채우지 않습니다.", this);
                return;
            }

            HashSet<Vector3Int> roomCells = CollectRoomCells(rooms);
            if (roomCells.Count == 0) return;

            BoundsInt area = GetFillArea(roomCells, padding);

            List<Vector3Int> positions = new List<Vector3Int>(area.size.x * area.size.y);
            foreach (Vector3Int cell in area.allPositionsWithin)
            {
                if (!roomCells.Contains(cell))
                    positions.Add(cell);
            }

            TileBase[] tiles = new TileBase[positions.Count];
            System.Array.Fill(tiles, wallTile);

            // 한 번에 넣어야 RuleTile 이웃 갱신도 한 번만 돈다.
            _wallTilemap.SetTiles(positions.ToArray(), tiles);
        }

        // 모든 방의 방 칸을 벽 타일맵 셀 좌표로 모은다.
        private HashSet<Vector3Int> CollectRoomCells(IReadOnlyList<GeneratedRoom> rooms)
        {
            HashSet<Vector3Int> cells = new HashSet<Vector3Int>();
            if (rooms == null) return cells;

            foreach (GeneratedRoom room in rooms)
            {
                Tilemap roomTilemap = room.Instance != null ? room.Instance.GetTileMap() : null;
                if (roomTilemap == null) continue;

                foreach (Vector3Int cell in roomTilemap.cellBounds.allPositionsWithin)
                {
                    if (!roomTilemap.HasTile(cell)) continue;

                    // 방 타일맵과 벽 타일맵의 원점이 달라도 월드 위치를 거쳐 같은 칸으로 맞춘다.
                    Vector3Int wallCell = _wallTilemap.WorldToCell(roomTilemap.GetCellCenterWorld(cell));
                    wallCell.z = 0;
                    cells.Add(wallCell);
                }
            }

            return cells;
        }

        private static BoundsInt GetFillArea(HashSet<Vector3Int> cells, int padding)
        {
            int xMin = int.MaxValue, yMin = int.MaxValue;
            int xMax = int.MinValue, yMax = int.MinValue;

            foreach (Vector3Int cell in cells)
            {
                if (cell.x < xMin) xMin = cell.x;
                if (cell.y < yMin) yMin = cell.y;
                if (cell.x > xMax) xMax = cell.x;
                if (cell.y > yMax) yMax = cell.y;
            }

            return new BoundsInt(
                xMin - padding, yMin - padding, 0,
                xMax - xMin + 1 + padding * 2, yMax - yMin + 1 + padding * 2, 1);
        }

        [ContextMenu("벽 지우기")]
        private void ClearWalls()
        {
            if (_wallTilemap == null)
                _wallTilemap = GetComponent<Tilemap>();

            _wallTilemap.ClearAllTiles();
        }
    }
}
