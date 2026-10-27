using Lrw.Script.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace KHO.Scripts.UI.InventoryUI
{
    // 공용 Controls 에셋을 건드리지 않기 위해 인벤토리 창 키(기본 V)를 여기서 직접 받는다.
    // 창 열기·닫기는 UIManager가 처리하므로 씬에 GameManager(UIManager)가 있어야 한다.
    public class InventoryWindowInput : MonoBehaviour
    {
        [SerializeField] private AbstractWindow window;
        [SerializeField] private InputAction toggleAction = new InputAction("Toggle Inventory", InputActionType.Button, "<Keyboard>/v");

        private void OnEnable()
        {
            toggleAction.performed += HandleToggle;
            toggleAction.Enable();
        }

        private void OnDisable()
        {
            toggleAction.performed -= HandleToggle;
            toggleAction.Disable();
        }

        private void HandleToggle(InputAction.CallbackContext context)
        {
            UIManagerEvents.OpenCloseWindow(window);
        }
    }
}
