using System.Collections.Generic;
using System.Linq;
using System.Threading;
using _Scripts._Core._Debug;
using _Scripts.Maps.New_Map.MapGenerateData;
using _Scripts.Maps.New_Map.MapSettingAsset;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Random = System.Random;

namespace _Scripts.Maps.New_Map
{
    /*
     * 방 배치 흐름
     * - 메인 스레드: RoomSO → RoomBakeData 굽기(Tilemap, Transform은 메인 스레드에서만 쓸 수 있다)
     * - 스레드풀: 연결 그래프(MapGraph) → 배치·백트래킹(MapPlacement) → 경로 검사(MapPathValidation)
     * - 한 번의 시도가 실패하면 그래프부터 다시 만들고, MaxFullRetries번 모두 실패하면 기본 맵을 쓴다.
     * - 난수는 System.Random(Seed) 하나만 쓰고 소비 순서를 고정한다: 그래프 → 배치 → (실패 시) 다음 그래프.
     */
    public partial class MapGenerator
    {
        // 링크 위치 차이를 정수 칸으로 볼 때 허용 오차
        private const float LinkAlignEpsilon = 0.01f;

        private volatile int deploymentCount;

        private async UniTask<DeploymentResult> MapDeployment(MapGenerateSetting mapSetting, int seed, CancellationToken ct)
        {
            if (mapSetting == null) return DeploymentResult.Fallback(0, 0);

            await UniTask.SwitchToMainThread(ct);

            MapSettingData setting = new MapSettingData(mapSetting, seed);
            (RoomBakeData defaultRoom, List<RoomBakeData> rooms) = await BakeRooms(mapSetting, ct);

            deploymentCount = 0;
            int total = setting.TotalRoomCount;
            var work = UniTask.RunOnThreadPool(() => MapDeploymentInThread(setting, defaultRoom, rooms, ct), cancellationToken: ct);

            while (work.Status == UniTaskStatus.Pending)
            {
                SetLoadingGauge("Map Deployment", deploymentCount, total);
                await UniTask.Yield(ct);
            }

            DeploymentResult result = await work;
            SetLoadingGauge("Map Deployment", total, total);
            return result;
        }

        // 여기서부터는 스레드풀에서 실행된다. Unity API(Tilemap, Transform, Object 비교, UnityEngine.Random)를 호출하지 않는다.
        private DeploymentResult MapDeploymentInThread(MapSettingData setting, RoomBakeData defaultRoom, List<RoomBakeData> rooms, CancellationToken ct)
        {
            Random random = new Random(setting.Seed);
            int backtracks = 0;

            for (int attempt = 0; attempt < setting.MaxFullRetries; attempt++)
            {
                ct.ThrowIfCancellationRequested();

                List<MapNode> nodes = BuildGraph(setting, random);

                // 설정 오류는 그래프를 다시 만들어도 같으므로 바로 기본 맵으로 간다.
                if (nodes == null || !HasRequiredRooms(nodes, rooms, defaultRoom))
                    return DeploymentResult.Fallback(backtracks, attempt);

                PlacementContext context = new PlacementContext(nodes, BuildPlacementOrder(nodes), rooms, defaultRoom, setting);
                deploymentCount = 0;

                if (TryPlaceRooms(context, random, ct, ref backtracks) && ValidatePaths(context))
                    return DeploymentResult.Success(CreateDeploymentData(context), backtracks, attempt);
            }

            return DeploymentResult.Fallback(backtracks, setting.MaxFullRetries - 1);
        }

        private static List<RoomDeploymentData> CreateDeploymentData(PlacementContext context)
        {
            List<RoomDeploymentData> result = new List<RoomDeploymentData>(context.State.Count);

            for (int k = 0; k < context.State.Count; k++)
            {
                MapNode node = context.Nodes[context.Order[k]];
                PlacedRoom placed = context.State.Rooms[k];

                result.Add(new RoomDeploymentData(
                    placed.Data.Prefab,
                    placed.Position,
                    node.Role,
                    node.Id,
                    placed.ParentIndex,
                    (bool[])placed.UsedLinks.Clone()));
            }

            return result;
        }

        #region Bake (Main Thread)

        private async UniTask<(RoomBakeData defaultRoom, List<RoomBakeData> rooms)> BakeRooms(MapGenerateSetting mapSetting, CancellationToken ct)
        {
            List<RoomSO> sources = new List<RoomSO> { mapSetting.DefaultRoom };
            if (mapSetting.Rooms != null)
                sources.AddRange(mapSetting.Rooms);

            // 같은 RoomSO가 여러 번 들어 있어도 한 번만 굽는다. 실패한 방은 null로 기록한다.
            // Dictionary는 조회에만 쓰고, 결과 목록은 Rooms 순서대로 만든다.
            Dictionary<RoomSO, RoomBakeData> baked = new Dictionary<RoomSO, RoomBakeData>();
            for (int i = 0; i < sources.Count; i++)
            {
                SetLoadingGauge("Room Bake", i, sources.Count);

                RoomSO room = sources[i];
                if (room == null || baked.ContainsKey(room)) continue;

                baked.Add(room, BakeRoom(room));
                await UniTask.Yield(ct);
            }
            SetLoadingGauge("Room Bake", sources.Count, sources.Count);

            RoomBakeData defaultRoom = mapSetting.DefaultRoom != null ? baked[mapSetting.DefaultRoom] : null;

            List<RoomBakeData> rooms = new List<RoomBakeData>();
            if (mapSetting.Rooms != null)
            {
                foreach (RoomSO room in mapSetting.Rooms.Where(r => r != null).Distinct())
                {
                    if (baked[room] != null)
                        rooms.Add(baked[room]);
                }
            }

            if (rooms.Count == 0)
                FDebug.LogWarning("[MapGenerator] 배치할 수 있는 Rooms가 없습니다.", mapSetting);

            return (defaultRoom, rooms);
        }

        private static RoomBakeData BakeRoom(RoomSO room)
        {
            Room prefab = room.RoomPrefab;
            if (prefab == null)
            {
                FDebug.LogWarning($"[MapGenerator] {room.name} : RoomPrefab이 없어 배치에서 제외합니다.", room);
                return null;
            }

            if (prefab.GetTileMap() == null)
            {
                FDebug.LogWarning($"[MapGenerator] {room.name} : Tilemap이 없어 배치에서 제외합니다.", room);
                return null;
            }

            // links 배열 순서가 굽기와 마감의 링크 인덱스이므로 빈 칸이 있으면 두 순서가 어긋난다.
            RoomLink[] roomLinks = prefab.GetRoomLink();
            if (roomLinks != null && roomLinks.Any(link => link == null))
            {
                FDebug.LogWarning($"[MapGenerator] {room.name} : links 배열에 빈 칸이 있어 배치에서 제외합니다.", room);
                return null;
            }

            // 타일 셀 좌표 기준. 방을 정수 칸 단위로 옮기므로 (0,0) 기준으로 구워 두고 배치할 때 더한다.
            List<RectInt> rects = room.GetBounds(Vector2Int.zero);
            if (rects.Count == 0)
            {
                FDebug.LogWarning($"[MapGenerator] {room.name} : 타일이 없어 배치에서 제외합니다.", room);
                return null;
            }

            // GetLinks는 링크 위치에서 넘긴 위치를 빼서 돌려주므로, 프리팹 루트 위치를 넘겨 루트 기준 로컬 좌표를 얻는다.
            List<RoomLinkData> links = roomLinks != null
                ? room.GetLinks(prefab.transform.position)
                : new List<RoomLinkData>();

            return new RoomBakeData(room, prefab, rects, links);
        }

        #endregion

        #region Data

        // 메인 스레드에서 구운 방 정보. 스레드에서는 이 데이터만 읽는다. Links 순서는 Room.links와 같다.
        private sealed class RoomBakeData
        {
            public readonly Room Prefab;
            public readonly RoomRole Role;
            public readonly float Priority;
            public readonly List<RectInt> Rects;
            public readonly RectInt Bounds;
            public readonly List<RoomLinkData> Links;

            public RoomBakeData(RoomSO room, Room prefab, List<RectInt> rects, List<RoomLinkData> links)
            {
                Prefab = prefab;
                Role = room.Role;
                Priority = room.Priority;
                Rects = rects;
                Links = links;
                Bounds = GetBoundingRect(rects);
            }

            public List<RectInt> GetWorldRects(Vector2Int position)
                => Rects.Select(r => OffsetRect(r, position)).ToList();

            public List<int> GetConnectableLinks(RoomLinkData from)
            {
                List<int> result = new List<int>();
                for (int i = 0; i < Links.Count; i++)
                {
                    if (RoomLinkData.CanLink(from, Links[i]))
                        result.Add(i);
                }
                return result;
            }
        }

        private sealed class PlacedRoom
        {
            public readonly RoomBakeData Data;
            public readonly Vector2Int Position;
            public readonly int ParentIndex;   // 배치 목록 안의 부모 인덱스, 시작방은 -1
            public readonly int ParentLink;    // 부모에서 이 방과 이어진 링크
            public readonly int InLink;        // 이 방에서 부모와 이어진 링크
            public readonly bool[] UsedLinks;  // 들어오는 링크와 자식에 쓴 링크가 true
            public readonly List<RectInt> WorldRects;
            public readonly RectInt WorldBounds;

            public PlacedRoom(RoomBakeData data, Vector2Int position, int parentIndex, int parentLink, int inLink)
            {
                Data = data;
                Position = position;
                ParentIndex = parentIndex;
                ParentLink = parentLink;
                InLink = inLink;
                UsedLinks = new bool[data.Links.Count];
                WorldRects = data.GetWorldRects(position);
                WorldBounds = OffsetRect(data.Bounds, position);
            }

            public List<int> GetFreeLinks()
            {
                List<int> result = new List<int>();
                for (int i = 0; i < UsedLinks.Length; i++)
                {
                    if (!UsedLinks[i])
                        result.Add(i);
                }
                return result;
            }
        }

        // 배치 중인 방 목록. 자식 방은 항상 부모 뒤에 추가되므로 뒤에서부터 빼면 링크 사용 상태를 되돌릴 수 있다.
        private sealed class DeploymentState
        {
            public readonly List<PlacedRoom> Rooms = new List<PlacedRoom>();
            public int Count => Rooms.Count;

            public void Place(RoomBakeData data, Vector2Int position, int parentIndex = -1, int parentLink = -1, int inLink = -1)
            {
                PlacedRoom placed = new PlacedRoom(data, position, parentIndex, parentLink, inLink);
                if (inLink >= 0)
                    placed.UsedLinks[inLink] = true;
                if (parentIndex >= 0)
                    Rooms[parentIndex].UsedLinks[parentLink] = true;

                Rooms.Add(placed);
            }

            public void RemoveLast()
            {
                PlacedRoom last = Rooms[Rooms.Count - 1];
                if (last.ParentIndex >= 0)
                    Rooms[last.ParentIndex].UsedLinks[last.ParentLink] = false;

                Rooms.RemoveAt(Rooms.Count - 1);
            }
        }

        // 스레드 작업 결과. 실패하면 IsFallback이 true이고 Rooms는 비어 있다.
        private sealed class DeploymentResult
        {
            public readonly List<RoomDeploymentData> Rooms;
            public readonly bool IsFallback;
            public readonly int Backtracks;
            public readonly int FullRetries;

            private DeploymentResult(List<RoomDeploymentData> rooms, bool isFallback, int backtracks, int fullRetries)
            {
                Rooms = rooms;
                IsFallback = isFallback;
                Backtracks = backtracks;
                FullRetries = fullRetries;
            }

            public static DeploymentResult Success(List<RoomDeploymentData> rooms, int backtracks, int fullRetries)
                => new DeploymentResult(rooms, false, backtracks, fullRetries);

            public static DeploymentResult Fallback(int backtracks, int fullRetries)
                => new DeploymentResult(new List<RoomDeploymentData>(), true, backtracks, fullRetries);
        }

        #endregion

        #region Helper

        private bool RectListOverlaps(List<RectInt> rect1, List<RectInt> rect2)
            => rect1.Any(i => rect2.Any(i.Overlaps));

        // 타일 칸에 맞지 않는(정수가 아닌) 위치 차이는 연결하지 않는다.
        private static bool TryGetCellOffset(Vector2 delta, out Vector2Int offset)
        {
            offset = Vector2Int.RoundToInt(delta);
            return Mathf.Abs(delta.x - offset.x) < LinkAlignEpsilon
                   && Mathf.Abs(delta.y - offset.y) < LinkAlignEpsilon;
        }

        private static RectInt OffsetRect(RectInt rect, Vector2Int offset)
            => new RectInt(rect.position + offset, rect.size);

        private static RectInt GetBoundingRect(List<RectInt> rects)
        {
            int xMin = rects.Min(r => r.xMin);
            int yMin = rects.Min(r => r.yMin);
            int xMax = rects.Max(r => r.xMax);
            int yMax = rects.Max(r => r.yMax);
            return new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
        }

        private static void Shuffle<T>(List<T> list, Random random)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        // 가중치를 쓴 비복원 추출 순서. 가중치가 모두 같으면 균등 셔플과 같고, 가중치가 남지 않으면 나머지는 균등하게 섞는다.
        private static List<T> GetWeightedOrder<T>(List<T> items, System.Func<T, float> getWeight, Random random)
        {
            List<T> remain = new List<T>(items);
            List<T> order = new List<T>(items.Count);

            float total = remain.Sum(item => Mathf.Max(0f, getWeight(item)));

            while (remain.Count > 0)
            {
                int pick = random.Next(remain.Count);

                if (total > 0f)
                {
                    double roll = random.NextDouble() * total;
                    for (int i = 0; i < remain.Count; i++)
                    {
                        float weight = Mathf.Max(0f, getWeight(remain[i]));
                        if (weight <= 0f) continue;

                        pick = i;
                        roll -= weight;
                        if (roll < 0) break;
                    }
                }

                total -= Mathf.Max(0f, getWeight(remain[pick]));
                order.Add(remain[pick]);
                remain.RemoveAt(pick);
            }

            return order;
        }

        #endregion
    }
}
