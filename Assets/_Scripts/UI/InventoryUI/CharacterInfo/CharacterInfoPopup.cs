using UnityEngine;
using UnityEngine.InputSystem;

namespace _Scripts.UI.InventoryUI.CharacterInfo
{
    // 인벤토리 창이 열려 있을 때 A(또는 간략 스탯의 힌트)를 누르고 있는 동안만 캐릭터 상세 스탯 팝업을 보여 준다.
    // 공용 Controls 에셋을 건드리지 않기 위해 키를 여기서 직접 받는다(InventoryWindowInput과 같은 방식).
    // 창이 열려 있는 동안에는 플레이어 입력이 꺼지므로 A가 이동과 겹치지 않는다.
    public class CharacterInfoPopup : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private InventoryWindow window;
        [Tooltip("누르고 있으면 팝업을 보여 줄 힌트. 없으면 비워 둔다.")]
        [SerializeField] private PointerHoldArea hintHold;
        [SerializeField] private InputAction holdAction = new InputAction("Hold Character Info", InputActionType.Button, "<Keyboard>/a");

        private bool _keyHeld;
        private bool _visible;

        private void Awake()
        {
            SetVisible(false);
        }

        private void OnEnable()
        {
            holdAction.started += HandleKeyPressed;
            holdAction.canceled += HandleKeyReleased;
            holdAction.Enable();
            if (hintHold != null) hintHold.OnHeldChanged += HandleHintHeld;
        }

        private void OnDisable()
        {
            holdAction.started -= HandleKeyPressed;
            holdAction.canceled -= HandleKeyReleased;
            holdAction.Disable();
            if (hintHold != null) hintHold.OnHeldChanged -= HandleHintHeld;

            _keyHeld = false;
            SetVisible(false);
        }

        // 보이는 중에 인벤토리 창이 닫히면 바로 숨긴다.
        private void Update()
        {
            if (_visible && !window.IsOpen) Refresh();
        }

        private void HandleKeyPressed(InputAction.CallbackContext context)
        {
            _keyHeld = true;
            Refresh();
        }

        private void HandleKeyReleased(InputAction.CallbackContext context)
        {
            _keyHeld = false;
            Refresh();
        }

        private void HandleHintHeld(bool held) => Refresh();

        private void Refresh()
        {
            bool held = _keyHeld || (hintHold != null && hintHold.IsHeld);
            SetVisible(held && window.IsOpen);
        }

        private void SetVisible(bool visible)
        {
            _visible = visible;
            canvasGroup.alpha = visible ? 1f : 0f;
            // 보기만 하는 팝업이라 아래 창의 클릭(힌트 누름 유지 등)을 막지 않는다.
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }
}
