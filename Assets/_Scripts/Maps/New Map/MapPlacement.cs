using System.Collections.Generic;
using System.Threading;
using _Scripts.Maps.New_Map.MapGenerateData;
using _Scripts.Maps.New_Map.MapSettingAsset;
using UnityEngine;
using Random = System.Random;

namespace _Scripts.Maps.New_Map
{
    /*
     * 2~4단계 방 선택·배치·검사 (스레드풀에서 실행)
     * - 배치 순서대로 방을 하나씩 붙이고, 후보가 바닥나면 바로 앞 방을 빼서 그 방의 다음 후보를 시도한다(시간 순 백트래킹).
     * - 되돌아간 방은 후보를 새로 만들지 않고 남은 후보로 이어서 시도한다.
     * - steps는 후보 하나를 검사할 때마다 1 오르고, MaxPlacementSteps를 넘으면 이번 시도는 실패다.
     */
    public partial class MapGenerator
    {
        private bool TryPlaceRooms(PlacementContext context, Random random, CancellationToken ct, ref int backtracks)
        {
            int count = context.Order.Count;
            List<PlacementCandidate>[] candidates = new List<PlacementCandidate>[count];
            int[] cursors = new int[count];
            int steps = 0;
            int k = 0;

            while (k < count)
            {
                ct.ThrowIfCancellationRequested();

                if (candidates[k] == null)
                {
                    candidates[k] = MakeCandidates(context, k, random);
                    cursors[k] = 0;
                }

                bool placed = false;
                while (cursors[k] < candidates[k].Count)
                {
                    if (++steps > context.Setting.MaxPlacementSteps) return false;

                    PlacementCandidate candidate = candidates[k][cursors[k]++];
                    if (!Fits(context, candidate)) continue;

                    Place(context, k, candidate);
                    placed = true;
                    break;
                }

                if (placed)
                {
                    k++;
                    // 다음 방은 후보를 새로 만든다.
                    if (k < count)
                        candidates[k] = null;
                }
                else
                {
                    candidates[k] = null;
                    k--;
                    if (k < 0) return false;

                    // 부모 링크 사용 표시도 함께 되돌린다.
                    context.State.RemoveLast();
                    backtracks++;
                }

                deploymentCount = context.State.Count;
            }

            return true;
        }

        #region Candidates

        // 배치 순서 k번째 노드의 후보 목록. Priority를 가중치로 한 비복원 추출 순서로 정렬한다.
        private static List<PlacementCandidate> MakeCandidates(PlacementContext context, int k, Random random)
        {
            MapNode node = context.Nodes[context.Order[k]];
            if (node.Parent < 0)
                return MakeStartCandidates(context, node, random);

            PlacedRoom parent = context.State.Rooms[context.OrderIndexOfNode[node.Parent]];
            List<int> freeLinks = parent.GetFreeLinks();

            List<PlacementCandidate> candidates = new List<PlacementCandidate>();
            foreach (RoomBakeData room in context.Rooms)
            {
                // 들어오는 링크 하나를 빼고도 자식 수만큼 링크가 남아야 한다.
                if (room.Role != node.Role) continue;
                if (room.Links.Count - 1 < node.Children.Count) continue;

                foreach (int parentLink in freeLinks)
                {
                    RoomLinkData from = parent.Data.Links[parentLink];

                    foreach (int inLink in room.GetConnectableLinks(from))
                    {
                        // 두 링크의 위치가 같은 점에 놓이도록 새 방의 위치를 정한다.
                        if (!TryGetCellOffset(from.Position - room.Links[inLink].Position, out Vector2Int offset)) continue;

                        candidates.Add(new PlacementCandidate(room, parent.Position + offset, inLink, parentLink));
                    }
                }
            }

            return GetWeightedOrder(candidates, c => c.Room.Priority, random);
        }

        // 시작방은 Start 역할 방을 모두 (0,0)에 놓는 후보로 만든다. 하나도 없으면 DefaultRoom 하나만 쓴다.
        private static List<PlacementCandidate> MakeStartCandidates(PlacementContext context, MapNode node, Random random)
        {
            List<PlacementCandidate> candidates = new List<PlacementCandidate>();
            foreach (RoomBakeData room in context.Rooms)
            {
                if (room.Role != RoomRole.Start) continue;
                if (room.Links.Count < node.Children.Count) continue;

                candidates.Add(new PlacementCandidate(room, Vector2Int.zero, -1, -1));
            }

            if (candidates.Count == 0 && context.DefaultRoom != null)
                return new List<PlacementCandidate> { new PlacementCandidate(context.DefaultRoom, Vector2Int.zero, -1, -1) };

            return GetWeightedOrder(candidates, c => c.Room.Priority, random);
        }

        #endregion

        #region Check

        private bool Fits(PlacementContext context, PlacementCandidate candidate)
            => IsInsideMapBounds(context.Setting, candidate.Room, candidate.Position)
               && !IsOverlapping(context.State, candidate.Room, candidate.Position);

        // 이미 놓인 방과 타일이 겹치면 탈락한다. 맞닿는 것은 허용한다.
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

        // MapBounds의 x, y가 모두 0보다 크면 방의 모든 타일이 시작방 원점 중심의 범위 안에 있어야 한다.
        private static bool IsInsideMapBounds(MapSettingData setting, RoomBakeData room, Vector2Int position)
        {
            Vector2Int size = setting.MapBounds;
            if (size.x <= 0 || size.y <= 0) return true;

            RectInt limit = new RectInt(-size.x / 2, -size.y / 2, size.x, size.y);
            RectInt bounds = OffsetRect(room.Bounds, position);

            return bounds.xMin >= limit.xMin && bounds.yMin >= limit.yMin
                   && bounds.xMax <= limit.xMax && bounds.yMax <= limit.yMax;
        }

        #endregion

        private static void Place(PlacementContext context, int k, PlacementCandidate candidate)
        {
            MapNode node = context.Nodes[context.Order[k]];
            int parentIndex = node.Parent >= 0 ? context.OrderIndexOfNode[node.Parent] : -1;

            context.State.Place(candidate.Room, candidate.Position, parentIndex, candidate.ParentLink, candidate.InLink);
        }

        #region Data

        // 배치 순서 k번째 노드에 놓을 수 있는 방 하나
        private readonly struct PlacementCandidate
        {
            public readonly RoomBakeData Room;
            public readonly Vector2Int Position;
            public readonly int InLink;      // 새 방에서 부모와 이어지는 링크, 시작방은 -1
            public readonly int ParentLink;  // 부모에서 새 방과 이어지는 링크, 시작방은 -1

            public PlacementCandidate(RoomBakeData room, Vector2Int position, int inLink, int parentLink)
            {
                Room = room;
                Position = position;
                InLink = inLink;
                ParentLink = parentLink;
            }
        }

        // 한 번의 배치 시도에 쓰는 그래프·배치 순서·방 목록·배치 상태
        private sealed class PlacementContext
        {
            public readonly List<MapNode> Nodes;
            public readonly List<int> Order;
            // 그래프 노드 → 배치 인덱스. 부모는 배치 목록의 바로 앞 방이 아닐 수 있다.
            public readonly int[] OrderIndexOfNode;
            public readonly List<RoomBakeData> Rooms;
            public readonly RoomBakeData DefaultRoom;
            public readonly MapSettingData Setting;
            public readonly DeploymentState State = new DeploymentState();

            public PlacementContext(List<MapNode> nodes, List<int> order, List<RoomBakeData> rooms, RoomBakeData defaultRoom, MapSettingData setting)
            {
                Nodes = nodes;
                Order = order;
                Rooms = rooms;
                DefaultRoom = defaultRoom;
                Setting = setting;

                OrderIndexOfNode = new int[nodes.Count];
                for (int i = 0; i < order.Count; i++)
                    OrderIndexOfNode[order[i]] = i;
            }
        }

        #endregion
    }
}
