using System.Collections.Generic;
using UnityEngine;

namespace SDW.Scripts.Maps.Data
{
    // 일직선 스테이지 구성. slots 순서대로 왼쪽에서 오른쪽으로 방이 이어진다.
    [CreateAssetMenu(fileName = "Stage", menuName = "SDW/Map/Stage")]
    public class StageSO : ScriptableObject
    {
        [SerializeField] private List<StageSlot> slots = new();
        [Tooltip("켜두면 한 스테이지 안에서 같은 방 데이터를 다시 뽑지 않는다. 후보가 모자라면 중복을 허용한다.")]
        [SerializeField] private bool avoidDuplicateInStage = true;

        // 슬롯마다 방을 하나씩 골라 순서대로 반환한다.
        public List<RoomDataSO> ResolveRooms()
        {
            List<RoomDataSO> result = new();
            HashSet<RoomDataSO> used = new();

            for (int i = 0; i < slots.Count; i++)
            {
                List<RoomDataSO> validCandidates = GetValidCandidates(slots[i]);
                if (validCandidates.Count == 0)
                {
                    Debug.LogWarning($"{name} : {i}번 슬롯에 후보 방이 없어 건너뜁니다.");
                    continue;
                }

                List<RoomDataSO> pool = validCandidates;

                if (avoidDuplicateInStage)
                {
                    pool = validCandidates.FindAll(candidate => !used.Contains(candidate));

                    if (pool.Count == 0)
                    {
                        Debug.LogWarning($"{name} : {i}번 슬롯의 후보가 모두 사용되어 중복을 허용합니다.");
                        pool = validCandidates;
                    }
                }

                RoomDataSO selected = pool[Random.Range(0, pool.Count)];
                used.Add(selected);
                result.Add(selected);
            }

            return result;
        }

        private static List<RoomDataSO> GetValidCandidates(StageSlot slot)
        {
            List<RoomDataSO> candidates = new();
            if (slot == null)
                return candidates;

            foreach (RoomDataSO candidate in slot.Candidates)
            {
                if (candidate != null)
                    candidates.Add(candidate);
            }

            return candidates;
        }
    }
}
