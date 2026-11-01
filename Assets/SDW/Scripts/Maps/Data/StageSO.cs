using System.Collections.Generic;
using UnityEngine;

namespace SDW.Scripts.Maps.Data
{
    // 일직선 스테이지 구성. segments에 넣은 구간 순서대로, 각 구간에서 뽑은 방이 이어진다.
    // 예: 기본 맵 2~4 → 미니 보스 1 → 기본 맵 2~4 → 최종 보스 1
    // 첫 번째 방이 시작 방이고 마지막 방이 최종 보스 방이다. 이전 방으로는 돌아갈 수 없다.
    [CreateAssetMenu(fileName = "Stage", menuName = "Map/Stage")]
    public class StageSO : ScriptableObject
    {
        [Tooltip("이 순서대로 구간을 생성한다. 마지막 방은 출구가 없고, 클리어하면 탐험이 끝난다.")]
        [SerializeField] private List<StageSegment> segments = new();

        [Header("Presentation")]
        [Tooltip("적이 등장할 때 쓰는 포탈 연출. 비워두면 적이 바로 생성된다.")]
        [SerializeField] private EnemySpawnPortal enemySpawnPortal;

        public EnemySpawnPortal EnemySpawnPortal => enemySpawnPortal;

        // 호출할 때마다 구간별로 다시 뽑으므로 런마다 방 구성이 달라진다.
        public List<RoomDataSO> ResolveRooms()
        {
            List<RoomDataSO> result = new();

            foreach (StageSegment segment in segments)
            {
                if (segment == null)
                    continue;

                segment.Resolve(result, name);
            }

            if (result.Count == 0)
                Debug.LogWarning($"{name} : 방이 하나도 지정되지 않았습니다.");
            else if (result[result.Count - 1].RoomType != RoomType.Boss)
                Debug.LogWarning($"{name} : 마지막 방({result[result.Count - 1].name})이 보스 방이 아닙니다. 이 방을 클리어하면 탐험이 끝납니다.");

            return result;
        }

        private void OnValidate()
        {
            foreach (StageSegment segment in segments)
                segment?.Validate();
        }
    }
}
