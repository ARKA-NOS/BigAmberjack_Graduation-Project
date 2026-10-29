using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SDW.Scripts.Maps.Data
{
    // 스테이지의 한 구간. roomPool에서 minCount~maxCount개를 균등 확률로 뽑는다(중복 허용).
    // 후보를 하나만 넣고 개수를 1~1로 두면 고정 방(미니 보스, 최종 보스 등)이 된다.
    [Serializable]
    public class StageSegment
    {
        [Tooltip("이 구간에서 뽑을 방 후보. 하나만 넣고 개수를 1~1로 두면 고정 방이 된다.")]
        [SerializeField] private List<RoomDataSO> roomPool = new();
        [Tooltip("이 구간에서 나올 최소 방 개수.")]
        [SerializeField, Min(0)] private int minCount = 1;
        [Tooltip("이 구간에서 나올 최대 방 개수.")]
        [SerializeField, Min(0)] private int maxCount = 1;

        // 뽑은 방을 result 뒤에 이어 붙인다.
        public void Resolve(List<RoomDataSO> result, string stageName)
        {
            List<RoomDataSO> candidates = new();

            foreach (RoomDataSO room in roomPool)
            {
                if (room != null)
                    candidates.Add(room);
            }

            if (candidates.Count == 0)
            {
                Debug.LogWarning($"{stageName} : 방 후보가 비어 있는 구간을 건너뜁니다.");
                return;
            }

            int count = Random.Range(minCount, maxCount + 1);

            for (int i = 0; i < count; i++)
                result.Add(candidates[Random.Range(0, candidates.Count)]);
        }

        public void Validate()
        {
            if (maxCount < minCount)
                maxCount = minCount;
        }
    }
}
