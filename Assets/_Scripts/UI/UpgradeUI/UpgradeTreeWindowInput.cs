using _Scripts.UI.UI_System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Scripts.UI.UpgradeUI
{
    // 공용 Controls 에셋을 건드리지 않기 위해 테크트리 창 닫기 키(기본 ESC)를 여기서 직접 받는다(InventoryWindowInput과 같은 방식).
    // 여는 것은 상호작용 오브젝트(UpgradeBuilding)가 한다. 설정 창은 같은 ESC를 쓰지만 열린 창이 있으면 열리지 않는다.
    public class UpgradeTreeWindowInput : MonoBehaviour
    {
        [SerializeField] private UpgradeTreeWindow window;
        [SerializeField] private InputAction closeAction = new InputAction("Close Tech Tree", InputActionType.Button, "<Keyboard>/escape");

        private void OnEnable()
        {
            closeAction.performed += HandleClose;
            closeAction.Enable();
        }

        private void OnDisable()
        {
            closeAction.performed -= HandleClose;
            closeAction.Disable();
        }

        // UIManager는 같은 창을 다시 보내면 닫으므로, 열려 있을 때만 보낸다.
        private void HandleClose(InputAction.CallbackContext context)
        {
            if (window.IsOpen)
                UIManagerEvents.OpenCloseWindow(window);
        }
    }
}
