using System.Collections;
using System.Linq;
using DevLib.ModuleSystem;
using Lrw.Script.Agent.Players;
using Lrw.Script.Agent.Players.InteractSystem;
using SDW.Scripts.Interact.NPC.Data;
using SDW.Scripts.UI.Dialogue;
using UnityEngine;

namespace SDW.Scripts.Interact.NPC
{
    // 특수 NPC. 플레이어의 InteractModule이 이 오브젝트를 찾아 상호작용 키를 누르면 대화창을 연다.
    // InteractModule은 MapObject 레이어의 콜라이더에서 이 컴포넌트를 찾으므로, 콜라이더와 같은 오브젝트에 두어야 한다.
    // InteractionLock이 붙어 있으면 주변에 살아 있는 적이 있는 동안 콜라이더를 꺼서 상호작용을 막는다.
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
        private Collider2D _col;
        private SpriteRenderer _spriteRenderer;
        private InteractionLock _lock;
        private Coroutine _closeRoutine;
        private bool _used;
        private bool _isOpen;
        private bool _visible = true;

        private bool IsLocked => _lock != null && _lock.IsLocked;

        private void Awake()
        {
            _col = GetComponent<Collider2D>();
            _spriteRenderer = GetComponent<SpriteRenderer>();

            Debug.Assert(_col != null, "NPC 콜라이더가 없습니다.");
            Debug.Assert(dialogueViewModel != null, $"{name} : DialogueViewModel이 없습니다.");

            if (data != null && data.Sprite != null && _spriteRenderer != null)
                _spriteRenderer.sprite = data.Sprite;

            if (TryGetComponent(out _lock))
                _lock.LockChanged += HandleLockChanged;
        }

        // 방 이동 등으로 꺼질 때 열어둔 대화창이 화면에 남지 않게 닫는다.
        private void OnDisable()
        {
            Close();
        }

        private void OnDestroy()
        {
            if (_lock != null)
                _lock.LockChanged -= HandleLockChanged;
        }

        // 잠기는 순간 열려 있던 대화창도 닫는다.
        private void HandleLockChanged(bool locked)
        {
            RefreshCollider();

            if (locked)
                Close();
        }

        private void SetVisible(bool visible)
        {
            _visible = visible;
            RefreshCollider();

            if (_spriteRenderer != null)
                _spriteRenderer.enabled = visible;

            if (!visible)
                Close();
        }

        // 숨겨졌거나 잠겨 있으면 InteractModule의 상호작용 대상(과 프롬프트)에서 빠지도록 콜라이더를 끈다.
        private void RefreshCollider()
        {
            _col.enabled = _visible && !IsLocked;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerController>() == null)
                return;

            Close();
        }

        public override void Interact(ModuleOwner owner)
        {
            if (!CanUse || _isOpen || IsLocked)
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
                // 선택지 없이 취소 대사만 보여준다.
                BindDialogue(new DialogueData(data.NpcName, data.CancelDialogue));
                _closeRoutine = StartCoroutine(CloseAfterDelay());
                return;
            }

            // TODO: 비용 검사 → InsufficientCostMessage 표시

            Debug.Log($"{data.NpcName} : '{choice.ChoiceText}' 선택");

            foreach (ChoiceEffectSO effect in choice.Effects)
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
