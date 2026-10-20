using System.Collections.Generic;
using UnityEngine;

namespace SDW.Scripts.Maps.Data
{
    // 일직선 스테이지 구성. 시작 방 → 랜덤 방 N개 → 보스 방 순서로 왼쪽에서 오른쪽으로 이어진다.
    [CreateAssetMenu(fileName = "Stage", menuName = "Map/Stage")]
    public class StageSO : ScriptableObject
    {
        [Header("Fixed")]
        [Tooltip("맨 앞에 고정되는 시작 방")]
        [SerializeField] private RoomDataSO startRoom;
        [Tooltip("맨 끝에 고정되는 보스 방")]
        [SerializeField] private RoomDataSO bossRoom;

        [Header("Random")]
        [Tooltip("시작 방과 보스 방 사이에 넣을 방 개수")]
        [SerializeField, Min(0)] private int randomRoomCount = 3;
        [Tooltip("중간 방 후보. 시작/보스 방은 제외된다.")]
        [SerializeField] private List<RoomDataSO> randomRoomPool = new();
        [Tooltip("켜두면 한 스테이지 안에서 같은 방 데이터를 다시 뽑지 않는다. 후보가 모자라면 연속만 피해서 중복을 허용한다.")]
        [SerializeField] private bool avoidDuplicateInStage = true;

        // 시작 방, 랜덤 방들, 보스 방 순서로 반환한다.
        public List<RoomDataSO> ResolveRooms()
        {
            List<RoomDataSO> result = new();

            if (startRoom != null)
                result.Add(startRoom);
            else
                Debug.LogWarning($"{name} : 시작 방이 지정되지 않았습니다.");

            List<RoomDataSO> pool = GetValidPool();
            if (pool.Count == 0 && randomRoomCount > 0)
                Debug.LogWarning($"{name} : 랜덤 방 후보가 없어 중간 방을 생성하지 않습니다.");

            HashSet<RoomDataSO> used = new();

            for (int i = 0; i < randomRoomCount && pool.Count > 0; i++)
            {
                RoomDataSO previous = result.Count > 0 ? result[result.Count - 1] : null;
                RoomDataSO selected = PickRandom(pool, used, previous);
                used.Add(selected);
                result.Add(selected);
            }

            if (bossRoom != null)
                result.Add(bossRoom);
            else
                Debug.LogWarning($"{name} : 보스 방이 지정되지 않았습니다.");

            return result;
        }

        // null과 시작/보스 방은 후보에서 뺀다.
        private List<RoomDataSO> GetValidPool()
        {
            List<RoomDataSO> pool = new();

            foreach (RoomDataSO candidate in randomRoomPool)
            {
                if (candidate == null)
                    continue;

                if (candidate.RoomType is RoomType.Start or RoomType.Boss)
                {
                    Debug.LogWarning($"{name} : {candidate.name}은(는) {candidate.RoomType} 방이라 랜덤 후보에서 제외합니다.");
                    continue;
                }

                pool.Add(candidate);
            }

            return pool;
        }

        // 아직 안 쓴 방 중에서 고른다. 다 썼으면 바로 앞 방만 피해서 고르고, 그것도 안 되면 아무거나 고른다.
        private RoomDataSO PickRandom(List<RoomDataSO> pool, HashSet<RoomDataSO> used, RoomDataSO previous)
        {
            List<RoomDataSO> candidates = pool;

            if (avoidDuplicateInStage)
            {
                candidates = pool.FindAll(candidate => !used.Contains(candidate));

                if (candidates.Count == 0)
                {
                    Debug.LogWarning($"{name} : 랜덤 방 후보가 모두 사용되어 중복을 허용합니다. (연속 중복만 피함)");
                    candidates = pool.FindAll(candidate => candidate != previous);

                    if (candidates.Count == 0)
                        candidates = pool;
                }
            }

            return candidates[Random.Range(0, candidates.Count)];
        }
    }
}
