using SDW.Scripts.Maps.Conditions;
using UnityEngine;

namespace SDW.Scripts.Maps.Data
{
    // 방 데이터의 부모 SO. 방 종류마다 이 클래스를 상속한 자식 SO를 만든다.
    // RoomController는 이 부모 타입만 알고, 입장 → 출구 조건 시작 → 클리어 처리 순서로 호출한다.
    // 상자/NPC 배치와 전투 구역은 방 프리팹(InteractableSpawnPoint, EncounterZone)이 맡는다.
    public abstract class RoomDataSO : ScriptableObject
    {
        [SerializeField] private RoomDefinition roomPrefab;
        [Tooltip("출구가 열리는 조건. 비워두면 방 종류별 기본 조건(CreateDefaultClearCondition)을 사용한다.")]
        [SerializeField] private RoomClearConditionSO clearCondition;

        public RoomDefinition RoomPrefab => roomPrefab;
        public abstract RoomType RoomType { get; }

        // 방에 처음 입장했을 때 할 일 (보스 생성 등). 기본은 아무것도 하지 않는다.
        public virtual void OnFirstEnter(RoomController room)
        {
        }

        // 출구 조건을 달성했을 때 할 일.
        public virtual void OnCleared(RoomController room)
        {
        }

        public IRoomClearCondition CreateClearCondition(RoomController room)
        {
            return clearCondition != null ? clearCondition.Create(room) : CreateDefaultClearCondition(room);
        }

        // 기본은 입장 즉시 클리어(출구 바로 열림).
        protected virtual IRoomClearCondition CreateDefaultClearCondition(RoomController room)
        {
            return new AlwaysClearedCondition();
        }
    }
}
