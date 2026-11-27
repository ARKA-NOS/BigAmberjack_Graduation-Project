using _Scripts.Agent.Players.InteractSystem;
using _Scripts.DevLib.ModuleSystem;
using _Scripts.UI.PlayerUI.Title_UI.Equip;
using UnityEngine;

namespace _Scripts.Interactable.NPC
{
    // 칭호 NPC. 상호작용하면 칭호 장착 창을 연다. 대화 없이 바로 창이 열린다.
    // 던전의 스폰 테이블로 생성되므로 씬의 창을 직접 참조하지 않고, TitleEquipEvents로 열기를 요청한다.
    // InteractModule은 MapObject 레이어의 콜라이더에서 이 컴포넌트를 찾으므로, 콜라이더와 같은 오브젝트에 두어야 한다.
    // InteractionLock이 붙어 있으면 주변에 살아 있는 적이 있는 동안 콜라이더를 꺼서 상호작용을 막는다.
    public class TitleNpc : AbstractInteractObject
    {
        private Collider2D _col;
        private InteractionLock _lock;

        private void Awake()
        {
            _col = GetComponent<Collider2D>();
            Debug.Assert(_col != null, "칭호 NPC 콜라이더가 없습니다.");

            if (TryGetComponent(out _lock))
                _lock.LockChanged += HandleLockChanged;
        }

        private void OnDestroy()
        {
            if (_lock != null)
                _lock.LockChanged -= HandleLockChanged;
        }

        private void HandleLockChanged(bool locked)
        {
            if (_col != null)
                _col.enabled = !locked;
        }

        public override void Interact(ModuleOwner owner)
        {
            if (_lock != null && _lock.IsLocked)
                return;

            TitleEquipEvents.RequestOpen();
        }
    }
}
