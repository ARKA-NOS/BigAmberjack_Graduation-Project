using DevLib.ModuleSystem;
using Lrw.Script.Agent.Players.InteractSystem;
using SDW.Scripts.Interact.Chest.Data;
using UnityEngine;

namespace SDW.Scripts.Interact.Chest
{
    // 보물상자. 플레이어의 InteractModule이 이 오브젝트를 찾아 상호작용 키를 누르면 열리고 보상 효과를 적용한다.
    // InteractModule은 MapObject 레이어의 콜라이더에서 이 컴포넌트를 찾으므로, 콜라이더와 같은 오브젝트에 두어야 한다.
    // 한 번 열면 열린 스프라이트로 바뀌고 다시 상호작용할 수 없다.
    // InteractionLock이 붙어 있으면 주변에 살아 있는 적이 있는 동안 콜라이더를 꺼서 상호작용을 막는다.
    public class Chest : AbstractInteractObject, ICanInteract
    {
        [SerializeField] private ChestDataSO data;
        [Tooltip("상호작용 UI가 따라다니는 기준점. 오브젝트 위치에서 이만큼 떨어진 곳(상자 윗면)에 표시된다.")]
        [SerializeField] private Vector2 uiAnchorOffset;
        [Tooltip("상자가 열릴 때 한 번 재생할 파티클. 자식 파티클도 함께 재생된다.")]
        [SerializeField] private ParticleSystem openEffect;

        // 베이스의 Position 대신 UI 기준점 오프셋을 적용한다. (ICanInteract를 다시 구현해서 인터페이스 조회에도 이 값이 쓰인다)
        public new Vector2 Position => (Vector2)transform.position + uiAnchorOffset;

        public ChestDataSO Data => data;
        public bool IsOpened { get; private set; }

        private Collider2D _col;
        private SpriteRenderer _spriteRenderer;
        private InteractionLock _lock;
        private Animator _animator;

        private static readonly int OpenHash = Animator.StringToHash("Open");

        private bool IsLocked => _lock != null && _lock.IsLocked;

        private void Awake()
        {
            _col = GetComponent<Collider2D>();
            _spriteRenderer = GetComponent<SpriteRenderer>();
            _animator = GetComponent<Animator>();

            if (TryGetComponent(out _lock))
                _lock.LockChanged += HandleLockChanged;

            Debug.Assert(_col != null, "상자 콜라이더가 없습니다.");
            Debug.Assert(data != null, $"{name} : ChestDataSO가 없습니다.");

            if (data != null && data.ClosedSprite != null && _spriteRenderer != null)
                _spriteRenderer.sprite = data.ClosedSprite;
        }

        private void OnDestroy()
        {
            if (_lock != null)
                _lock.LockChanged -= HandleLockChanged;
        }

        private void HandleLockChanged(bool locked)
        {
            RefreshCollider();
        }

        // 열린 상자이거나 잠겨 있으면 InteractModule의 상호작용 대상(과 프롬프트)에서 빠지도록 콜라이더를 끈다.
        private void RefreshCollider()
        {
            _col.enabled = !IsOpened && !IsLocked;
        }

        public override void Interact(ModuleOwner owner)
        {
            if (IsOpened || IsLocked || data == null)
                return;

            // 받을 수 있는 보상이 하나도 없으면(예: 인벤토리 가득 참) 열지 않아서 아이템이 사라지지 않게 한다.
            if (!HasApplicableReward(owner))
            {
                Debug.LogWarning($"[Chest] {data.ChestName} 열 수 없음: 받을 수 있는 보상이 없습니다.");
                return;
            }

            Debug.Log($"[Chest] {data.ChestName} 열림");
            IsOpened = true;

            foreach (ChoiceEffectSO reward in data.Rewards)
            {
                if (reward != null && reward.CanApply(owner))
                    reward.Apply(owner);
            }

            // Animator가 있으면 열림 애니메이션을 재생하고, 없으면 열린 스프라이트로 바꾼다.
            if (_animator != null && _animator.runtimeAnimatorController != null)
                _animator.SetTrigger(OpenHash);
            else if (data.OpenedSprite != null && _spriteRenderer != null)
                _spriteRenderer.sprite = data.OpenedSprite;

            if (openEffect != null)
                openEffect.Play();

            // 열린 상자는 더 이상 상호작용 대상으로 잡히지 않게 한다.
            RefreshCollider();
        }

        private bool HasApplicableReward(ModuleOwner owner)
        {
            foreach (ChoiceEffectSO reward in data.Rewards)
            {
                if (reward != null && reward.CanApply(owner))
                    return true;
            }

            return false;
        }
    }
}
