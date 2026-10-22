using System.Collections;
using System.Linq;
using DevLib.ModuleSystem;
using Lrw.Script.Agent.Players;
using Lrw.Script.Agent.Players.InteractSystem;
using SDW.Scripts.Interact.NPC.Data;
using SDW.Scripts.Maps;
using SDW.Scripts.Maps.Data;
using SDW.Scripts.UI.Dialogue;
using UnityEngine;

namespace SDW.Scripts.Interact.NPC
{
    // 특수 NPC. 플레이어의 InteractModule이 이 오브젝트를 찾아 상호작용 키를 누르면 대화창을 연다.
    // InteractModule은 MapObject 레이어의 콜라이더에서 이 컴포넌트를 찾으므로, 콜라이더와 같은 오브젝트에 두어야 한다.
    // 전투방 안에 배치된 경우, 방을 클리어하기 전까지는 숨겨져 있다가 클리어 후 나타난다.
    // 대화창은 직접 참조하지 않고 DialogueViewModel에 대화 내용을 Bind만 한다. (화면 표시는 씬의 DialogueUIBinder 담당)
    public class Npc : AbstractInteractObject
    {
        [SerializeField] private NpcDataSO data;
        [SerializeField] private DialogueViewModel dialogueViewModel;
        [Tooltip("취소 대사를 보여준 뒤 대화창을 닫기까지의 시간")]
        [SerializeField] private float closeDelay = 1.5f;

        public NpcDataSO Data => data;

        private bool CanUse => data != null && (data.Reusable || !_used);

        private ModuleOwner _owner;
        private RoomController _room;
        private Collider2D _col;
        private SpriteRenderer _spriteRenderer;
        private Coroutine _closeRoutine;
        private bool _used;
        private bool _isOpen;

        private void Awake()
        {
            _col = GetComponent<Collider2D>();
            _spriteRenderer = GetComponent<SpriteRenderer>();

            Debug.Assert(_col != null, "NPC 콜라이더가 없습니다.");
            Debug.Assert(dialogueViewModel != null, $"{name} : DialogueViewModel이 없습니다.");

            if (data != null && data.Sprite != null && _spriteRenderer != null)
                _spriteRenderer.sprite = data.Sprite;
        }

        private void Start()
        {
            _room = GetComponentInParent<RoomController>();

            bool waitForClear = _room != null
                                && _room.Data != null
                                && _room.Data.RoomType == RoomType.Combat
                                && !_room.IsCleared;

            if (!waitForClear)
                return;

            SetVisible(false);
            _room.Cleared += HandleRoomCleared;
        }

        // 방 이동 등으로 꺼질 때 열어둔 대화창이 화면에 남지 않게 닫는다.
        private void OnDisable()
        {
            Close();
        }

        private void OnDestroy()
        {
            if (_room != null)
                _room.Cleared -= HandleRoomCleared;
        }

        private void HandleRoomCleared()
        {
            _room.Cleared -= HandleRoomCleared;
            SetVisible(true);
        }

        private void SetVisible(bool visible)
        {
            _col.enabled = visible;

            if (_spriteRenderer != null)
                _spriteRenderer.enabled = visible;

            if (!visible)
                Close();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerController>() == null)
                return;

            Close();
        }

        public override void Interact(ModuleOwner owner)
        {
            if (!CanUse || _isOpen)
                return;

            _owner = owner;
            _isOpen = true;

            string[] choiceTexts = data.Choices.Select(choice => choice.ChoiceText).ToArray();
            BindDialogue(new DialogueData(data.NpcName, data.FirstDialogue, choiceTexts, OnChoiceSelected));
        }

        private void Close()
        {
            if (_closeRoutine != null)
            {
                StopCoroutine(_closeRoutine);
                _closeRoutine = null;
            }

            // 이 NPC가 연 대화창만 닫는다. (다른 NPC가 연 대화창을 닫지 않도록)
            if (!_isOpen)
                return;

            _isOpen = false;
            BindDialogue(null);
        }

        private void OnChoiceSelected(int index)
        {
            if (!_isOpen)
                return;

            NpcChoiceData choice = data.Choices[index];

            if (choice.IsCancel)
            {
                ShowLineAndClose(data.CancelDialogue);
                return;
            }

            if (!ChoiceCostSO.CanPayAll(choice.Costs, _owner))
            {
                ShowLineAndClose(data.InsufficientCostMessage);
                return;
            }

            // 받을 수 있는 효과가 하나도 없으면 비용을 걷지 않고 막는다. (상자와 같은 원칙)
            if (choice.Effects.Count > 0 && !ChoiceEffectSO.AnyApplicable(choice.Effects, _owner))
            {
                ShowLineAndClose(data.RewardBlockedMessage);
                return;
            }

            Debug.Log($"{data.NpcName} : '{choice.ChoiceText}' 선택");

            ChoiceCostSO.PayAll(choice.Costs, _owner);
            ChoiceEffectSO.ApplyAll(choice.Effects, _owner);

            _used = true;
            Close();

            // 재이용 불가 NPC는 첫 번째 이용 후 사라진다.
            if (!data.Reusable)
                SetVisible(false);
        }

        // 선택지 없이 대사 한 줄만 보여준 뒤 잠시 후 대화창을 닫는다. (취소, 비용 부족, 보상 불가 안내)
        private void ShowLineAndClose(string line)
        {
            BindDialogue(new DialogueData(data.NpcName, line));
            _closeRoutine = StartCoroutine(CloseAfterDelay());
        }

        private void BindDialogue(DialogueData dialogue)
        {
            if (dialogueViewModel != null)
                dialogueViewModel.Bind(dialogue);
        }

        private IEnumerator CloseAfterDelay()
        {
            yield return new WaitForSeconds(closeDelay);
            _closeRoutine = null;
            Close();
        }
    }
}
