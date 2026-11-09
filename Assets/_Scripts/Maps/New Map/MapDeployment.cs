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
     * 방 배치 규칙
     * - MapCount : 시작 방(DefaultRoom)을 포함한 전체 방 개수.
     * - BranchCount : 가지 개수. BranchRange(x ~ y, 양 끝 포함) : 가지 하나의 방 개수.
     * - 메인 경로 방 개수 = MapCount - 가지 방 개수 합. 메인 경로에는 최소 2개(시작, 끝)를 남긴다.
     * - 가지는 메인 경로의 끝 방을 뺀 방에서 뻗어 나간다(끝 방은 막다른 방으로 남긴다).
     * - 두 링크는 RoomLinkData.CanLink면 연결할 수 있고, 연결하면 두 링크의 위치가 겹치게 놓인다.
     * - 방끼리 타일이 겹치면 배치하지 않는다.
     */
    public partial class MapGenerator
    {
        // 배치 전체를 처음부터 다시 시도하는 최대 횟수
        private const int MaxDeploymentAttempts = 30;
        // 경로 하나를 만들 때 방 1개당 허용하는 배치/되돌리기 횟수
        private const int BacktrackBudgetPerRoom = 10;
        // 링크 위치 차이를 정수 칸으로 볼 때 허용 오차
        private const float LinkAlignEpsilon = 0.01f;

        private volatile int deploymentCount;

        private async UniTask<List<RoomDeploymentData>> MapDeployment(MapGenerateSetting mapSetting, CancellationToken ct)
        {
            if(mapSetting == null) return new List<RoomDeploymentData>();

            await UniTask.SwitchToMainThread(ct);

            // Tilemap, Transform은 메인 스레드에서만 쓸 수 있으므로 스레드에 넘기기 전에 순수 데이터로 굽는다.
            (RoomBakeData startRoom, List<RoomBakeData> rooms) = await BakeRooms(mapSetting, ct);
            if (startRoom == null)
            {
                FDebug.LogError("[MapGenerator] DefaultRoom을 사용할 수 없어 맵을 배치하지 못했습니다.", mapSetting);
                return new List<RoomDeploymentData>();
            }

            deploymentCount = 0;
            var work = UniTask.RunOnThreadPool(() => MapDeploymentInThread(mapSetting, startRoom, rooms, ct), cancellationToken: ct);

            while (work.Status == UniTaskStatus.Pending)
            {
                SetLoadingGauge("Map Deployment", deploymentCount, mapSetting.MapCount);
                await UniTask.Yield(ct);
            }

            List<RoomDeploymentData> list = await work;
            SetLoadingGauge("Map Deployment", mapSetting.MapCount, mapSetting.MapCount);
            return list;
        }

        // 여기서부터는 스레드풀에서 실행된다. Unity API(Tilemap, Transform, Object 비교 등)를 호출하지 않는다.
        private List<RoomDeploymentData> MapDeploymentInThread(MapGenerateSetting mapSetting, RoomBakeData startRoom, List<RoomBakeData> rooms, CancellationToken ct)
        {
            List<RoomDeploymentData> result = new List<RoomDeploymentData>();

            Random random = new Random(mapSetting.Seed);

            int totalCount = Mathf.Max(1, mapSetting.MapCount);
            List<int> branchLengths = CreateBranchLengths(mapSetting, totalCount, random);
            int mainCount = totalCount - branchLengths.Sum();

            DeploymentState best = null;
            for (int attempt = 0; attempt < MaxDeploymentAttempts; attempt++)
            {
                ct.ThrowIfCancellationRequested();

                DeploymentState state = new DeploymentState();
                state.Place(startRoom, Vector2Int.zero);
                deploymentCount = state.Count;

                bool success = BuildPath(state, 0, mainCount - 1, rooms, random, ct)
                               && BuildBranches(state, branchLengths, rooms, random, ct);

                if (success)
                {
                    best = state;
                    break;
                }

                if (best == null || state.Count > best.Count)
                    best = state;
            }

            if (best.Count < totalCount)
                FDebug.LogWarning($"[MapGenerator] 방을 {totalCount}개 중 {best.Count}개만 배치했습니다. 방 링크 위치/방향/크기 설정을 확인하세요.");

            foreach (PlacedRoom placed in best.Rooms)
                result.Add(new RoomDeploymentData(placed.Data.Prefab, placed.Position));

            deploymentCount = best.Count;
            return result;
        }

        #region Bake (Main Thread)

        private async UniTask<(RoomBakeData startRoom, List<RoomBakeData> rooms)> BakeRooms(MapGenerateSetting mapSetting, CancellationToken ct)
        {
            List<RoomSO> sources = new List<RoomSO> { mapSetting.DefaultRoom };
            if (mapSetting.Rooms != null)
                sources.AddRange(mapSetting.Rooms);

            // 같은 RoomSO가 여러 번 들어 있어도 한 번만 굽는다. 실패한 방은 null로 기록한다.
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

            RoomBakeData startRoom = mapSetting.DefaultRoom != null ? baked[mapSetting.DefaultRoom] : null;

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

            return (startRoom, rooms);
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

            // 타일 셀 좌표 기준. 방을 정수 칸 단위로 옮기므로 (0,0) 기준으로 구워 두고 배치할 때 더한다.
            List<RectInt> rects = room.GetBounds(Vector2Int.zero);
            if (rects.Count == 0)
            {
                FDebug.LogWarning($"[MapGenerator] {room.name} : 타일이 없어 배치에서 제외합니다.", room);
                return null;
            }

            // GetLinks는 링크 위치에서 넘긴 위치를 빼서 돌려주므로, 프리팹 루트 위치를 넘겨 루트 기준 로컬 좌표를 얻는다.
            List<RoomLinkData> links = prefab.GetRoomLink() != null
                ? room.GetLinks(prefab.transform.position)
                : new List<RoomLinkData>();

            return new RoomBakeData(room, prefab, rects, links);
        }

        #endregion

        #region Deployment (Thread)

        private static List<int> CreateBranchLengths(MapGenerateSetting mapSetting, int totalCount, Random random)
        {
            List<int> lengths = new List<int>();

            Vector2Int range = mapSetting.BranchRange;
            int min = Mathf.Max(0, Mathf.Min(range.x, range.y));
            int max = Mathf.Max(0, Mathf.Max(range.x, range.y));

            // 메인 경로에 시작 방과 끝 방은 남겨야 한다.
            int budget = Mathf.Max(0, totalCount - 2);

            for (int i = 0; i < mapSetting.BranchCount && budget > 0; i++)
            {
                int length = Mathf.Min(random.Next(min, max + 1), budget);
                if (length <= 0) continue;

                lengths.Add(length);
                budget -= length;
            }

            return lengths;
        }

        // anchorIndex 방에서 시작해 방 length개를 한 줄로 잇는다.
        // 막히면 마지막 방을 빼고 그 앞 방에서 다시 시도한다. 실패하면 만든 데까지 남기고 false를 반환한다.
        private bool BuildPath(DeploymentState state, int anchorIndex, int length, List<RoomBakeData> rooms, Random random, CancellationToken ct)
        {
            int baseCount = state.Count;
            int budget = length * BacktrackBudgetPerRoom;
            int tail = anchorIndex;

            while (state.Count - baseCount < length)
            {
                ct.ThrowIfCancellationRequested();
                if (budget-- <= 0) return false;

                if (TryAttachRoom(state, tail, rooms, random))
                {
                    tail = state.Count - 1;
                }
                else
                {
                    // 시작점에서조차 붙일 수 없으면 이 경로는 만들 수 없다.
                    if (state.Count == baseCount) return false;

                    state.RemoveLast();
                    tail = state.Count > baseCount ? state.Count - 1 : anchorIndex;
                }

                deploymentCount = state.Count;
            }

            return true;
        }

        private bool BuildBranches(DeploymentState state, List<int> branchLengths, List<RoomBakeData> rooms, Random random, CancellationToken ct)
        {
            // 메인 경로의 끝 방은 가지 시작점에서 뺀다.
            int mainCount = state.Count;
            List<int> anchors = Enumerable.Range(0, Mathf.Max(1, mainCount - 1)).ToList();

            foreach (int length in branchLengths)
            {
                Shuffle(anchors, random);

                bool built = false;
                foreach (int anchor in anchors)
                {
                    int before = state.Count;
                    if (BuildPath(state, anchor, length, rooms, random, ct))
                    {
                        built = true;
                        break;
                    }

                    state.RollbackTo(before);
                    deploymentCount = state.Count;
                }

                if (!built) return false;
            }

            return true;
        }

        // fromIndex 방의 빈 링크에 새 방을 하나 붙인다. 방은 Priority 가중치 순서로 고른다.
        private bool TryAttachRoom(DeploymentState state, int fromIndex, List<RoomBakeData> rooms, Random random)
        {
            PlacedRoom from = state.Rooms[fromIndex];

            List<int> freeLinks = from.GetFreeLinks();
            if (freeLinks.Count == 0) return false;
            Shuffle(freeLinks, random);

            foreach (RoomBakeData room in GetWeightedOrder(rooms, random))
            {
                foreach (int fromLink in freeLinks)
                {
                    RoomLinkData linkA = from.Data.Links[fromLink];

                    List<int> toLinks = room.GetConnectableLinks(linkA);
                    Shuffle(toLinks, random);

                    foreach (int toLink in toLinks)
                    {
                        // 두 링크 위치가 겹치도록 새 방의 위치를 정한다.
                        if (!TryGetCellOffset(linkA.Position - room.Links[toLink].Position, out Vector2Int offset)) continue;

                        Vector2Int position = from.Position + offset;
                        if (IsOverlapping(state, room, position)) continue;

                        state.Place(room, position, fromIndex, fromLink, toLink);
                        return true;
                    }
                }
            }

            return false;
        }

        private bool IsOverlapping(DeploymentState state, RoomBakeData room, Vector2Int position)
        {
            RectInt bounds = OffsetRect(room.Bounds, position);
            List<RectInt> rects = null;

            foreach (PlacedRoom placed in state.Rooms)
            {
                // 외곽 사각형이 안 겹치면 세부 사각형은 볼 필요가 없다.
                if (!bounds.Overlaps(placed.WorldBounds)) continue;

                rects ??= room.GetWorldRects(position);
                if (RectListOverlaps(rects, placed.WorldRects)) return true;
            }

            return false;
        }

        // Priority를 가중치로 한 비복원 추출 순서. 가중치가 남지 않으면 나머지는 균등하게 섞는다.
        private static List<RoomBakeData> GetWeightedOrder(List<RoomBakeData> rooms, Random random)
        {
            List<RoomBakeData> remain = new List<RoomBakeData>(rooms);
            List<RoomBakeData> order = new List<RoomBakeData>(rooms.Count);

            while (remain.Count > 0)
            {
                float total = remain.Sum(r => Mathf.Max(0f, r.Priority));
                int pick = random.Next(remain.Count);

                if (total > 0f)
                {
                    double roll = random.NextDouble() * total;
                    for (int i = 0; i < remain.Count; i++)
                    {
                        float weight = Mathf.Max(0f, remain[i].Priority);
                        if (weight <= 0f) continue;

                        pick = i;
                        roll -= weight;
                        if (roll < 0) break;
                    }
                }

                order.Add(remain[pick]);
                remain.RemoveAt(pick);
            }

            return order;
        }

        #endregion

        #region Data

        // 메인 스레드에서 구운 방 정보. 스레드에서는 이 데이터만 읽는다.
        private sealed class RoomBakeData
        {
            public readonly Room Prefab;
            public readonly float Priority;
            public readonly List<RectInt> Rects;
            public readonly RectInt Bounds;
            public readonly List<RoomLinkData> Links;

            public RoomBakeData(RoomSO room, Room prefab, List<RectInt> rects, List<RoomLinkData> links)
            {
                Prefab = prefab;
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
            public readonly int ParentIndex;
            public readonly int ParentLink;
            public readonly bool[] UsedLinks;
            public readonly List<RectInt> WorldRects;
            public readonly RectInt WorldBounds;

            public PlacedRoom(RoomBakeData data, Vector2Int position, int parentIndex, int parentLink)
            {
                Data = data;
                Position = position;
                ParentIndex = parentIndex;
                ParentLink = parentLink;
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

            public void Place(RoomBakeData data, Vector2Int position, int parentIndex = -1, int parentLink = -1, int selfLink = -1)
            {
                PlacedRoom placed = new PlacedRoom(data, position, parentIndex, parentLink);
                if (selfLink >= 0)
                    placed.UsedLinks[selfLink] = true;
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

            public void RollbackTo(int count)
            {
                while (Rooms.Count > count)
                    RemoveLast();
            }
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

        #endregion

    }
}
