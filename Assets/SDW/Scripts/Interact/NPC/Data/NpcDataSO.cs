using System.Collections.Generic;
using UnityEngine;

namespace SDW.Scripts.Interact.NPC.Data
{
    [CreateAssetMenu(fileName = "Npc", menuName = "NPC/Npc Data")]
    public class NpcDataSO : ScriptableObject
    {
        [field: Header("기본 정보")]
        [field: Tooltip("플레이어에게 표시되는 이름")]
        [field: SerializeField] public string NpcName { get; private set; }
        [field: Tooltip("데이터 식별용 고유 ID")]
        [field: SerializeField] public string NpcId { get; private set; }
        [field: Tooltip("플레이어에게 제공하는 선택 또는 기능")]
        [field: SerializeField, TextArea] public string Role { get; private set; }

        [field: Header("출현 조건")]
        [field: Tooltip("등장하는 챕터. 비워두면 전체 챕터")]
        [field: SerializeField] public List<int> AppearChapters { get; private set; } = new();
        [field: Tooltip("등장하는 방. 전용 RoomType이 생기면 교체예정.")]
        [field: SerializeField] public string AppearRoomName { get; private set; }
        [field: Tooltip("선행 조건: 해금, 클리어, 보유 조건 등")]
        [field: SerializeField] public List<string> Preconditions { get; private set; } = new();

        [field: Header("선택과 결과")]
        [field: Tooltip("선택지마다 비용(Costs)과 효과(Effects)를 따로 설정한다.")]
        [field: SerializeField] public List<NpcChoiceData> Choices { get; private set; } = new();

        [field: Header("예외 처리와 UI")]
        [field: Tooltip("선택지의 비용을 치를 수 없을 때 표시할 이용 불가 안내")]
        [field: SerializeField] public string InsufficientCostMessage { get; private set; }
        [field: Tooltip("받을 수 있는 효과가 하나도 없을 때(예: 인벤토리 가득 참) 표시할 안내. 비용은 소모하지 않는다.")]
        [field: SerializeField] public string RewardBlockedMessage { get; private set; }
        [field: Tooltip("취소 선택지를 골랐을 때 출력할 대사")]
        [field: SerializeField, TextArea] public string CancelDialogue { get; private set; }
        [field: Tooltip("체크하면 여러 번 사용 가능, 해제하면 첫 번째 이용 후 사라진다.")]
        [field: SerializeField] public bool Reusable { get; private set; }
        [field: Tooltip("비용·위험·예상 결과 표시 방식")]
        [field: SerializeField, TextArea] public string UiFeedback { get; private set; }
        [field: Tooltip("상호작용을 시작했을 때 출력할 첫 대사")]
        [field: SerializeField, TextArea] public string FirstDialogue { get; private set; }

        [field: Header("프로필 이미지")]
        [field: Tooltip("NPC 외형. 비워두면 프리팹의 기본 스프라이트를 사용한다.")]
        [field: SerializeField] public Sprite Sprite { get; private set; }
    }
}
