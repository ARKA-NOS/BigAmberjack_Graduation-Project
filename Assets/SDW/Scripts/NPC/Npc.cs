using System.Collections;
using Agents.Players;
using DevLib.ModuleSystem;
using Lrw.Script.Player.InteractSystem;
using SDW.Scripts.Maps;
using SDW.Scripts.Maps.Data;
using SDW.Scripts.NPC.Data;
using SDW.Scripts.NPC.Effects;
using SDW.Scripts.NPC.UI;
using UnityEngine;

namespace SDW.Scripts.NPC
{
    // 특수 NPC. 플레이어의 InteractModule이 이 오브젝트를 찾아 상호작용 키를 누르면 대화창을 연다.
    // InteractModule은 MapObject 레이어의 콜라이더에서 이 컴포넌트를 찾으므로, 콜라이더와 같은 오브젝트에 두어야 한다.
    // 전투방 안에 배치된 경우, 방을 클리어하기 전까지는 숨겨져 있다가 클리어 후 나타난다.
    public class Npc : AbstractInteractObject
    {
        [SerializeField] private NpcDataSO data;
        [SerializeField] private NpcInteractionView view;
        [Tooltip("취소 대사를 보여준 뒤 대화창을 닫기까지의 시간")]
        [SerializeField] private float closeDelay = 1.5f;

        public NpcDataSO Data => data;

        private bool CanUse => data != null && (data.Reusable || !_used);

        private ModuleOwner _owner;
        private RoomController _room;
        private Collider2D _col;
        private SpriteRenderer _spriteRenderer;
        private Coroutine _closeRoutine;
        private bool _playerInRange;
        private bool _used;
        private bool _isOpen;

        private void Awake()
        {
            _col = GetComponent<Collider2D>();
            _spriteRenderer = GetComponent<SpriteRenderer>();

            Debug.Assert(_col != null, "NPC 콜라이더가 없습니다.");
            Debug.Assert(view != null, "NPC 상호작용 View가 없습니다.");

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
            {
                _playerInRange = false;
                Close();
                view.HidePrompt();
            }
        }

        // 머리 위 프롬프트 표시용 범위 감지. 실제 상호작용 대상 선정은 InteractModule이 한다.
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerController>() == null)
                return;

            _playerInRange = true;

            if (CanUse && !_isOpen)
                view.ShowPrompt();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerController>() == null)
                return;

            _playerInRange = false;
            Close();
            view.HidePrompt();
        }

        public override void Interact(ModuleOwner owner)
        {
            if (!CanUse || _isOpen)
                return;

            _owner = owner;
            _isOpen = true;
            view.OpenDialogue(data, OnChoiceSelected);
        }

        private void Close()
        {
            if (_closeRoutine != null)
            {
                StopCoroutine(_closeRoutine);
                _closeRoutine = null;
            }

            if (!_isOpen)
                return;

            _isOpen = false;
            view.CloseDialogue();

            if (_playerInRange && CanUse)
                view.ShowPrompt();
        }

        private void OnChoiceSelected(int index)
        {
            NpcChoiceData choice = data.Choices[index];

            if (choice.IsCancel)
            {
                view.ShowLine(data.CancelDialogue);
                _closeRoutine = StartCoroutine(CloseAfterDelay());
                return;
            }

            // TODO: 비용 검사 → InsufficientCostMessage 표시

            Debug.Log($"{data.NpcName} : '{choice.ChoiceText}' 선택");

            foreach (NpcChoiceEffectSO effect in choice.Effects)
            {
                if (effect != null && effect.CanApply(_owner))
                    effect.Apply(_owner);
            }

            _used = true;
            Close();

            // 재이용 불가 NPC는 첫 번째 이용 후 사라진다.
            if (!data.Reusable)
                SetVisible(false);
        }

        private IEnumerator CloseAfterDelay()
        {
            yield return new WaitForSeconds(closeDelay);
            _closeRoutine = null;
            Close();
        }
    }
}
