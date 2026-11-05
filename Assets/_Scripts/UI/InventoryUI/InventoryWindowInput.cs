using Lrw.Script.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KHO.Scripts.UI.InventoryUI
{
    // 공용 Controls 에셋을 건드리지 않기 위해 인벤토리 창 키(기본 V: 열기·닫기, ESC: 닫기만)를 여기서 직접 받는다.
    // 창 열기·닫기는 UIManager가 처리하므로 씬에 GameManager(UIManager)가 있어야 한다.
    public class InventoryWindowInput : MonoBehaviour
    {
        [SerializeField] private InventoryWindow window;
        [SerializeField] private InputAction toggleAction = new InputAction("Toggle Inventory", InputActionType.Button, "<Keyboard>/v");
        [SerializeField] private InputAction closeAction = new InputAction("Close Inventory", InputActionType.Button, "<Keyboard>/escape");

        private void OnEnable()
        {
            toggleAction.performed += HandleToggle;
            closeAction.performed += HandleClose;
            toggleAction.Enable();
            closeAction.Enable();
        }

        private void OnDisable()
        {
            toggleAction.performed -= HandleToggle;
            closeAction.performed -= HandleClose;
            toggleAction.Disable();
            closeAction.Disable();
        }

        private void HandleToggle(InputAction.CallbackContext context)
        {
            UIManagerEvents.OpenCloseWindow(window);
        }

        // UIManager는 같은 창을 다시 보내면 닫으므로, 열려 있을 때만 보낸다.
        private void HandleClose(InputAction.CallbackContext context)
        {
            if (window.IsOpen)
                UIManagerEvents.OpenCloseWindow(window);
        }
    }
}
