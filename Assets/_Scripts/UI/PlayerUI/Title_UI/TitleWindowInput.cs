using _Scripts.UI.UI_System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace _Scripts.UI.PlayerUI.Title_UI
{
    // 공용 Controls 에셋을 건드리지 않기 위해 칭호 창 키(기본 C: 열기·닫기, ESC: 닫기만)를 여기서 직접 받는다(InventoryWindowInput과 같은 방식).
    // 창 열기·닫기는 UIManager가 처리하므로 씬에 GameManager(UIManager)가 있어야 한다.
    public class TitleWindowInput : MonoBehaviour
    {
        [SerializeField] private TitleWindow window;
        [SerializeField] private InputAction toggleAction = new InputAction("Toggle Title", InputActionType.Button, "<Keyboard>/c");
        [SerializeField] private InputAction closeAction = new InputAction("Close Title", InputActionType.Button, "<Keyboard>/escape");

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
            // 검색창 등에 글을 쓰는 중이면 C는 글자 입력으로 둔다.
            if (UIInputFocus.IsTyping) return;

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
