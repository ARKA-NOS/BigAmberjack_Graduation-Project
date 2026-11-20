using System.Collections.Generic;
using _Scripts._Core._Debug;
using _Scripts.Maps.New_Map.MapGenerateData;
using _Scripts.Maps.New_Map.MapSettingAsset;
using UnityEngine;

namespace _Scripts.Maps.New_Map
{
    /*
     * 5단계 전체 경로 검사 (스레드풀에서 배치 직후에 실행)
     * - 모든 노드가 놓였는지 확인한다.
     * - 배치 결과를 "부모–자식 링크 연결" 무방향 그래프로 만들고 시작방에서 BFS로 출구와 모든 방에 닿는지 본다.
     * - 무방향으로 닿으면 분기에서 왔던 길로 돌아올 수 있다.
     */
    public partial class MapGenerator
    {
        private static bool ValidatePaths(PlacementContext context)
        {
            DeploymentState state = context.State;
            if (state.Count != context.Order.Count)
            {
                FDebug.LogWarning($"[MapGenerator] 경로 검사 실패: 방 {context.Order.Count}개 중 {state.Count}개만 놓였습니다.");
                return false;
            }

            List<int>[] adjacency = new List<int>[state.Count];
            for (int i = 0; i < state.Count; i++)
                adjacency[i] = new List<int>();

            for (int i = 1; i < state.Count; i++)
            {
                PlacedRoom child = state.Rooms[i];
                if (child.ParentIndex < 0 || child.ParentIndex >= i) return FailPath(context, i, "부모가 없습니다.");

                PlacedRoom parent = state.Rooms[child.ParentIndex];
                if (!IsLinkConnected(parent, child.ParentLink, child, child.InLink)) return FailPath(context, i, "부모와 링크가 맞물리지 않습니다.");

                adjacency[i].Add(child.ParentIndex);
                adjacency[child.ParentIndex].Add(i);
            }

            bool[] visited = VisitFrom(0, adjacency);
            for (int i = 0; i < visited.Length; i++)
            {
                if (!visited[i]) return FailPath(context, i, "시작방에서 닿지 않습니다.");
            }

            int exitIndex = FindPlacedIndex(context, RoomRole.Exit);
            if (exitIndex < 0) return FailPath(context, 0, "출구방이 없습니다.");

            return true;
        }

        // 두 링크가 실제로 같은 점에서 맞물려 있는지 본다.
        private static bool IsLinkConnected(PlacedRoom parent, int parentLink, PlacedRoom child, int childLink)
        {
            if (parentLink < 0 || childLink < 0) return false;
            if (!parent.UsedLinks[parentLink] || !child.UsedLinks[childLink]) return false;

            RoomLinkData a = parent.Data.Links[parentLink];
            RoomLinkData b = child.Data.Links[childLink];
            if (!RoomLinkData.CanLink(a, b)) return false;

            Vector2 worldA = (Vector2)parent.Position + a.Position;
            Vector2 worldB = (Vector2)child.Position + b.Position;
            return (worldA - worldB).sqrMagnitude < LinkAlignEpsilon * LinkAlignEpsilon;
        }

        private static bool[] VisitFrom(int start, List<int>[] adjacency)
        {
            bool[] visited = new bool[adjacency.Length];
            Queue<int> queue = new Queue<int>();

            visited[start] = true;
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                foreach (int next in adjacency[current])
                {
                    if (visited[next]) continue;

                    visited[next] = true;
                    queue.Enqueue(next);
                }
            }

            return visited;
        }

        private static int FindPlacedIndex(PlacementContext context, RoomRole role)
        {
            for (int k = 0; k < context.Order.Count; k++)
            {
                if (context.Nodes[context.Order[k]].Role == role)
                    return k;
            }
            return -1;
        }

        private static bool FailPath(PlacementContext context, int k, string reason)
        {
            FDebug.LogWarning($"[MapGenerator] 경로 검사 실패: {context.Nodes[context.Order[k]].Id} - {reason}");
            return false;
        }
    }
}
