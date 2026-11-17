using System.Collections.Generic;
using System.Linq;
using _Scripts._Core._Debug;
using _Scripts.Maps.New_Map.MapGenerateData;
using _Scripts.Maps.New_Map.MapSettingAsset;
using UnityEngine;
using Random = System.Random;

namespace _Scripts.Maps.New_Map
{
    /*
     * 1단계 연결 그래프 (스레드풀에서 실행)
     * - 주 경로: 시작 → 전투 1 … 전투 MainCombatCount → 출구
     * - 분기 기준 후보: 주 경로 전투방 중 번호가 BranchAnchorRange.x ~ y인 방(1부터, 양 끝 포함)
     * - 후보를 섞은 뒤 앞에서부터 상점 분기, 그다음 보상 분기에 하나씩 배정한다. 한 방에는 분기가 하나만 붙는다.
     * - 상점 분기: 기준 방 → 상점 / 보상 분기: 기준 방 → 추가 전투 × RewardBranchCombatCount → 보상
     * - 분기에서 다시 갈라지거나 다른 경로와 이어지지 않는 트리다.
     */
    public partial class MapGenerator
    {
        // 그래프를 만든다. 분기 기준 후보가 분기 수보다 적으면 설정 오류로 null을 반환한다.
        private static List<MapNode> BuildGraph(MapSettingData setting, Random random)
        {
            List<MapNode> nodes = new List<MapNode>();

            int start = AddNode(nodes, "Start", RoomRole.Start, -1);

            List<int> mainCombats = new List<int>();
            int previous = start;
            for (int i = 1; i <= setting.MainCombatCount; i++)
            {
                previous = AddNode(nodes, $"Combat{i}", RoomRole.Combat, previous);
                mainCombats.Add(previous);
            }

            AddNode(nodes, "Exit", RoomRole.Exit, previous);

            List<int> anchors = GetBranchAnchorNumbers(setting);
            int branchCount = setting.ShopBranchCount + setting.RewardBranchCount;
            if (anchors.Count < branchCount)
            {
                FDebug.LogError($"[MapGenerator] 분기 기준 후보({anchors.Count}개)가 분기 수({branchCount}개)보다 적습니다. MainCombatCount와 BranchAnchorRange를 확인하세요.");
                return null;
            }

            Shuffle(anchors, random);

            int next = 0;
            for (int i = 0; i < setting.ShopBranchCount; i++)
            {
                int anchor = mainCombats[anchors[next++] - 1];
                AddNode(nodes, GetBranchName("Shop", i, setting.ShopBranchCount), RoomRole.Shop, anchor);
            }

            for (int i = 0; i < setting.RewardBranchCount; i++)
            {
                string prefix = GetBranchName("Reward", i, setting.RewardBranchCount);

                int parent = mainCombats[anchors[next++] - 1];
                for (int c = 1; c <= setting.RewardBranchCombatCount; c++)
                    parent = AddNode(nodes, $"{prefix}Combat{c}", RoomRole.Combat, parent);

                AddNode(nodes, prefix, RoomRole.Reward, parent);
            }

            return nodes;
        }

        // 깊이 우선 배치 순서. 분기가 붙은 전투방을 만나면 그 분기 방들을 바로 뒤에 넣고 주 경로로 돌아간다.
        // 예: 시작, 전투 1, 전투 2, 상점, 전투 3, 전투 4, 추가 전투, 보상, 전투 5, 출구
        private static List<int> BuildPlacementOrder(List<MapNode> nodes)
        {
            List<int> order = new List<int>(nodes.Count);
            Stack<int> stack = new Stack<int>();
            stack.Push(0);

            while (stack.Count > 0)
            {
                int index = stack.Pop();
                order.Add(index);

                // 주 경로 자식이 먼저 추가되어 있으므로 순서대로 넣으면 분기 자식이 먼저 꺼내진다.
                foreach (int child in nodes[index].Children)
                    stack.Push(child);
            }

            return order;
        }

        // 그래프에 필요한 역할의 방이 하나도 없으면 몇 번을 다시 해도 실패하므로 설정 오류로 본다.
        private static bool HasRequiredRooms(List<MapNode> nodes, List<RoomBakeData> rooms, RoomBakeData defaultRoom)
        {
            bool valid = true;

            foreach (RoomRole role in nodes.Select(n => n.Role).Distinct())
            {
                if (rooms.Any(r => r.Role == role)) continue;
                if (role == RoomRole.Start && defaultRoom != null) continue;

                FDebug.LogError($"[MapGenerator] {role} 역할 방이 Rooms에 없습니다.");
                valid = false;
            }

            return valid;
        }

        private static int AddNode(List<MapNode> nodes, string id, RoomRole role, int parent)
        {
            nodes.Add(new MapNode(id, role, parent));
            int index = nodes.Count - 1;

            if (parent >= 0)
                nodes[parent].Children.Add(index);

            return index;
        }

        private static List<int> GetBranchAnchorNumbers(MapSettingData setting)
        {
            Vector2Int range = setting.BranchAnchorRange;
            int min = Mathf.Max(1, Mathf.Min(range.x, range.y));
            int max = Mathf.Min(setting.MainCombatCount, Mathf.Max(range.x, range.y));

            List<int> numbers = new List<int>();
            for (int i = min; i <= max; i++)
                numbers.Add(i);

            return numbers;
        }

        // 분기가 하나면 "Shop", 여러 개면 "Shop1", "Shop2"
        private static string GetBranchName(string name, int index, int count)
            => count > 1 ? $"{name}{index + 1}" : name;
    }
}
