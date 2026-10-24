using System.Collections.Generic;
using UnityEngine;

namespace SDW.Scripts.Maps.Data
{
    // 일직선 스테이지 구성. rooms에 넣은 순서 그대로 방이 이어진다.
    // 첫 번째 방이 시작 방이고 마지막 방이 최종 보스 방이다. 미니 보스는 중간 어디에든 몇 개든 넣을 수 있다.
    // 이전 방으로는 돌아갈 수 없다.
    [CreateAssetMenu(fileName = "Stage", menuName = "Map/Stage")]
    public class StageSO : ScriptableObject
    {
        [Tooltip("이 순서대로 방을 생성한다. 마지막 방은 출구가 없고, 클리어하면 탐험이 끝난다.")]
        [SerializeField] private List<RoomDataSO> rooms = new();

        public List<RoomDataSO> ResolveRooms()
        {
            List<RoomDataSO> result = new();

            foreach (RoomDataSO room in rooms)
            {
                if (room == null)
                {
                    Debug.LogWarning($"{name} : 비어 있는 방 칸을 건너뜁니다.");
                    continue;
                }

                result.Add(room);
            }

            if (result.Count == 0)
                Debug.LogWarning($"{name} : 방이 하나도 지정되지 않았습니다.");
            else if (result[result.Count - 1].RoomType != RoomType.Boss)
                Debug.LogWarning($"{name} : 마지막 방({result[result.Count - 1].name})이 보스 방이 아닙니다. 이 방을 클리어하면 탐험이 끝납니다.");

            return result;
        }
    }
}
