using _Scripts._Core._ServiceLocator;
using _Scripts.Agent.Players;
using _Scripts.DevLib.ModuleSystem;
using _Scripts.Inventory;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace _Scripts.UI.InventoryUI
{
    // 칸 드래그 앤 드롭. 끄는 동안 아이콘(ghost)을 마우스에 붙이고, 다른 칸에 놓으면 인벤토리 모듈에 맞바꾸기를 요청한다.
    // 화면 갱신은 모듈 → ViewModel → Binder 흐름으로 따로 일어난다.
    public class InventoryDragController : MonoBehaviour
    {
        [Tooltip("창의 CanvasGroup. 닫혀 있으면(interactable false) 드래그를 받지 않는다.")]
        [SerializeField] private CanvasGroup windowGroup;
        [Tooltip("끄는 동안 마우스를 따라다니는 아이콘. 레이캐스트를 받지 않아야 아래 칸에 놓을 수 있다.")]
        [SerializeField] private Image ghost;

        private InventorySlotUI _dragging;

        private void Awake()
        {
            ghost.raycastTarget = false;
            ghost.enabled = false;
        }

        public bool BeginDrag(InventorySlotUI slot, PointerEventData eventData)
        {
            if (windowGroup != null && !windowGroup.interactable) return false;
            if (slot.Item == null || !slot.IconImage.enabled) return false;

            _dragging = slot;
            _dragging.SetDragging(true);

            ghost.sprite = slot.IconImage.sprite;
            ghost.rectTransform.sizeDelta = slot.IconImage.rectTransform.rect.size;
            ghost.transform.SetAsLastSibling();
            ghost.enabled = true;
            MoveGhost(eventData);
            return true;
        }

        public void Drag(PointerEventData eventData)
        {
            if (_dragging != null) MoveGhost(eventData);
        }

        // OnDrop(놓은 칸) 다음에 OnEndDrag(끈 칸)가 불린다.
        public void EndDrag()
        {
            if (_dragging != null) _dragging.SetDragging(false);
            _dragging = null;
            ghost.enabled = false;
        }

        public void Drop(InventorySlotUI target)
        {
            if (_dragging == null || target == _dragging) return;
            if (ServiceLocator.Get<IPlayerController>() is not ModuleOwner player || player == null) return;
            if (!player.TryGetModule(out IInventoryModule inventory)) return;

            inventory.TryMoveItem(_dragging.Type, _dragging.Index, target.Type, target.Index);
        }

        private void MoveGhost(PointerEventData eventData)
        {
            RectTransform parent = (RectTransform)ghost.transform.parent;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, eventData.position, eventData.pressEventCamera, out Vector2 local))
                ghost.rectTransform.localPosition = local;
        }
    }
}
