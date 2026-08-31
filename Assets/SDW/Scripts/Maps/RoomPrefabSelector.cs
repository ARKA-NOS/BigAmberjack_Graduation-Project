using System.Collections.Generic;
using UnityEngine;

namespace SDW.Scripts.Maps
{
    public class RoomPrefabSelector : IRoomPrefabSelector
    {
        private readonly IReadOnlyList<RoomDefinition> _candidates;
        private readonly bool _allowReuseWhenExhausted;
        private readonly HashSet<RoomDefinition> _usedPrefabs = new();

        public RoomPrefabSelector(IReadOnlyList<RoomDefinition> candidates, bool allowReuseWhenExhausted)
        {
            _candidates = candidates;
            _allowReuseWhenExhausted = allowReuseWhenExhausted;
        }

        // 13. 부분집합 규칙(ActualDirections ⊆ SupportedDirections)을 만족하는 미사용 프리팹 중,
        // 12. 불필요한 방향이 가장 적은(정확히 일치하는 것을 우선) 프리팹을 후보로 삼아 랜덤 선택한다.
        public RoomDefinition Select(RoomDirection actualDirections)
        {
            RoomDefinition selected = FindBestCandidate(actualDirections, allowUsed: false);

            // 미사용 후보가 없으면(=프리팹 종류가 부족하면), 옵션이 켜져 있을 때만 재사용을 허용한다.
            if (selected == null && _allowReuseWhenExhausted)
                selected = FindBestCandidate(actualDirections, allowUsed: true);

            if (selected != null)
                _usedPrefabs.Add(selected);

            return selected;
        }

        private RoomDefinition FindBestCandidate(RoomDirection actualDirections, bool allowUsed)
        {
            List<RoomDefinition> bestCandidates = new();
            int bestExtraDirectionCount = int.MaxValue;

            foreach (RoomDefinition candidate in _candidates)
            {
                if (candidate == null)
                    continue;

                if (!allowUsed && _usedPrefabs.Contains(candidate))
                    continue;

                if (!candidate.Supports(actualDirections))
                    continue;

                int extraDirectionCount = CountBits(candidate.SupportedDirections) - CountBits(actualDirections);

                if (extraDirectionCount < bestExtraDirectionCount)
                {
                    bestExtraDirectionCount = extraDirectionCount;
                    bestCandidates.Clear();
                    bestCandidates.Add(candidate);
                }
                else if (extraDirectionCount == bestExtraDirectionCount)
                {
                    bestCandidates.Add(candidate);
                }
            }

            if (bestCandidates.Count == 0)
                return null;

            return bestCandidates[Random.Range(0, bestCandidates.Count)];
        }

        private static int CountBits(RoomDirection direction)
        {
            // Flags 드롭다운에서 "Everything"을 선택하면 -1(모든 비트 1)로 직렬화된다.
            // 정의된 4개 방향 비트만 남기지 않으면 음수 int의 산술 시프트가 끝나지 않아 무한 루프에 빠진다.
            int value = (int)(direction & (RoomDirection.Up | RoomDirection.Down | RoomDirection.Left | RoomDirection.Right));
            int count = 0;

            while (value != 0)
            {
                count += value & 1;
                value >>= 1;
            }

            return count;
        }
    }
}
