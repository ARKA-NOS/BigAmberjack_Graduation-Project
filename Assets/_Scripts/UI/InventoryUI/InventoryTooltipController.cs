using _Scripts.Item;
using _Scripts.Item.ItemTooltip;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Scripts.UI.InventoryUI
{
    // 칸에 마우스를 올리면 그 칸 아이템의 툴팁을 마우스 옆에 띄운다.
    // 올려 둔 동안 칸의 아이템이 바뀌면(드래그로 맞바꿈 등) 내용도 따라 바뀐다.
    public class InventoryTooltipController : MonoBehaviour
    {
        [Tooltip("창의 CanvasGroup. 닫혀 있으면(interactable false) 툴팁을 띄우지 않는다.")]
        [SerializeField] private CanvasGroup windowGroup;
        [SerializeField] private ItemTooltipView view;
        [SerializeField] private Vector2 mouseOffset = new(24f, -24f);

        private InventorySlotUI _hovered;
        private ItemSO _shownItem;
        private Camera _canvasCamera;

        private void Awake()
        {
            Canvas canvas = view.GetComponentInParent<Canvas>().rootCanvas;
            _canvasCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            view.Hide();
        }

        private void OnDisable()
        {
            Hide();
        }

        public void Enter(InventorySlotUI slot)
        {
            _hovered = slot;
            Refresh();
        }

        public void Exit(InventorySlotUI slot)
        {
            if (_hovered == slot) Hide();
        }

        public void Hide()
        {
            _hovered = null;
            _shownItem = null;
            view.Hide();
        }

        private void Update()
        {
            if (_hovered == null) return;

            if (windowGroup != null && !windowGroup.interactable)
            {
                Hide();
                return;
            }

            Refresh();
        }

        private void Refresh()
        {
            ItemSO item = _hovered.Item?.Data;
            if (item != _shownItem)
            {
                _shownItem = item;
                view.Show(item);
            }

            if (item != null) FollowMouse();
        }

        private void FollowMouse()
        {
            if (Mouse.current == null) return;

            Vector2 screenPoint = Mouse.current.position.ReadValue();
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(view.Area, screenPoint, _canvasCamera, out Vector2 local))
                view.MoveTo(local + mouseOffset);
        }
    }
}
