using _Scripts._Core._ServiceLocator;
using _Scripts.UI.UI_System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Scripts.UI.SettingsUI
{
    // 공용 Controls 에셋을 건드리지 않기 위해 설정 창 키(기본 ESC)를 여기서 직접 받는다(InventoryWindowInput과 같은 방식).
    // ESC는 인벤토리 닫기에도 쓰이므로, 다른 창이 열려 있거나 같은 프레임에 닫혔으면 설정 창을 열지 않는다.
    // 창 열기·닫기는 UIManager가 처리하므로 씬에 GameManager(UIManager)가 있어야 한다.
    public class SettingsWindowInput : MonoBehaviour
    {
        [SerializeField] private SettingsWindow window;
        [SerializeField] private InputAction toggleAction = new InputAction("Toggle Settings", InputActionType.Button, "<Keyboard>/escape");

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
            if (window.IsOpen)
            {
                // 키 설정 화면에서는 메인 화면으로 한 단계만 돌아간다.
                if (window.IsKeysPageOpen) window.ShowMainPage();
                else window.RequestClose();
                return;
            }

            IUIWindowState windowState = ServiceLocator.Get<IUIWindowState>();
            if (windowState != null && (windowState.HasOpenWindow || windowState.LastCloseFrame == Time.frameCount)) return;

            UIManagerEvents.OpenCloseWindow(window);
        }
    }
}
