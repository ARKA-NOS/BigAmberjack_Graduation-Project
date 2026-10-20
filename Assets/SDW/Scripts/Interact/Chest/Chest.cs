using DevLib.ModuleSystem;
using Lrw.Script.Agent.Players.InteractSystem;
using SDW.Scripts.Interact.Chest.Data;
using UnityEngine;

namespace SDW.Scripts.Interact.Chest
{
    // 보물상자. 플레이어의 InteractModule이 이 오브젝트를 찾아 상호작용 키를 누르면 열리고 보상 효과를 적용한다.
    // InteractModule은 MapObject 레이어의 콜라이더에서 이 컴포넌트를 찾으므로, 콜라이더와 같은 오브젝트에 두어야 한다.
    // 한 번 열면 열린 스프라이트로 바뀌고 다시 상호작용할 수 없다.
    public class Chest : AbstractInteractObject
    {
        [SerializeField] private ChestDataSO data;

        public ChestDataSO Data => data;
        public bool IsOpened { get; private set; }

        private Collider2D _col;
        private SpriteRenderer _spriteRenderer;

        private void Awake()
        {
            _col = GetComponent<Collider2D>();
            _spriteRenderer = GetComponent<SpriteRenderer>();

            Debug.Assert(_col != null, "상자 콜라이더가 없습니다.");
            Debug.Assert(data != null, $"{name} : ChestDataSO가 없습니다.");

            if (data != null && data.ClosedSprite != null && _spriteRenderer != null)
                _spriteRenderer.sprite = data.ClosedSprite;
        }

        public override void Interact(ModuleOwner owner)
        {
            if (IsOpened || data == null)
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

            if (data.OpenedSprite != null && _spriteRenderer != null)
                _spriteRenderer.sprite = data.OpenedSprite;

            // 열린 상자는 더 이상 상호작용 대상으로 잡히지 않게 한다.
            _col.enabled = false;
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
