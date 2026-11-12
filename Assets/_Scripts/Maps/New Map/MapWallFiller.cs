using System.Collections.Generic;
using System.Threading;
using _Scripts.Maps.New_Map.MapGenerateData;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace _Scripts.Maps.New_Map
{
    /*
     * 방 사이 빈 공간을 벽 타일로 채우는 벽 전용 타일맵.
     * - 방 프리팹의 tilemap(배치 겹침 검사에 쓰는 방 영역)에 타일이 있는 칸을 방 칸으로 본다.
     * - 모든 방 칸을 감싸는 사각형을 padding칸만큼 넓히고, 그 안에서 방 칸이 아닌 칸을 모두 채운다.
     *   그래서 방 사이 틈뿐 아니라 맵 바깥 가장자리에도 벽이 생긴다.
     * - MapGenerator가 생성 과정(프리팹 생성 → 연결 지점 마감 다음)에서 Fill을 호출한다. 타일 설정은 MapGenerateSetting에 있다.
     *
     * 사용법: MapGenerator가 있는 Grid 오브젝트 아래에 벽 전용 Tilemap을 만들고 이 컴포넌트를 붙인다.
     *         충돌이 필요하면 같은 오브젝트에 TilemapCollider2D(+ CompositeCollider2D)를 붙인다.
     */
    [RequireComponent(typeof(Tilemap))]
    public class MapWallFiller : MonoBehaviour
    {
        private Tilemap _wallTilemap;

        private Tilemap WallTilemap
        {
            get
            {
                if (_wallTilemap == null)
                    _wallTilemap = GetComponent<Tilemap>();
                return _wallTilemap;
            }
        }

        // onProgress(넣은 타일 수, 전체 타일 수). 메인 스레드에서 호출한다.
        public async UniTask Fill(IReadOnlyList<GeneratedRoom> rooms, TileBase wallTile, int padding, int tilesPerFrame,
            System.Action<int, int> onProgress, CancellationToken ct)
        {
            Clear();
            if (wallTile == null) return;

            HashSet<Vector3Int> roomCells = CollectRoomCells(rooms);
            if (roomCells.Count == 0) return;

            BoundsInt area = GetFillArea(roomCells, Mathf.Max(0, padding));

            // allPositionsWithin은 아래 줄부터 x 순서로 돈다. 그래서 나눠 넣어도 줄 단위로 이어진다.
            List<Vector3Int> positions = new List<Vector3Int>(area.size.x * area.size.y);
            foreach (Vector3Int cell in area.allPositionsWithin)
            {
                if (!roomCells.Contains(cell))
                    positions.Add(cell);
            }

            int total = positions.Count;
            int chunkSize = tilesPerFrame > 0 ? tilesPerFrame : total;
            onProgress?.Invoke(0, total);

            for (int start = 0; start < total; start += chunkSize)
            {
                ct.ThrowIfCancellationRequested();

                int count = Mathf.Min(chunkSize, total - start);
                TileBase[] tiles = new TileBase[count];
                System.Array.Fill(tiles, wallTile);

                // 묶어서 넣어야 RuleTile 이웃 갱신이 타일마다 따로 돌지 않는다.
                WallTilemap.SetTiles(positions.GetRange(start, count).ToArray(), tiles);
                onProgress?.Invoke(start + count, total);

                if (start + count < total)
                    await UniTask.Yield(ct);
            }
        }

        public void Clear()
            => WallTilemap.ClearAllTiles();

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
                    Vector3Int wallCell = WallTilemap.WorldToCell(roomTilemap.GetCellCenterWorld(cell));
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
            => Clear();
    }
}
