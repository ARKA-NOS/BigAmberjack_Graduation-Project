using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _Scripts.UI.InventoryUI.CharacterInfo
{
    // 마우스(터치)로 누르고 있는 동안을 알려 준다. 누른 채 영역 밖으로 나가도 뗄 때까지 누름으로 본다.
    public class PointerHoldArea : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public event Action<bool> OnHeldChanged;

        public bool IsHeld { get; private set; }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) SetHeld(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) SetHeld(false);
        }

        private void OnDisable() => SetHeld(false);

        private void SetHeld(bool held)
        {
            if (IsHeld == held) return;

            IsHeld = held;
            OnHeldChanged?.Invoke(held);
        }
    }
}
