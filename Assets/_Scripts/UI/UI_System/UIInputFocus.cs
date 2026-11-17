using TMPro;
using UnityEngine.EventSystems;

namespace _Scripts.UI.UI_System
{
    // 입력창에 글을 쓰는 중인지 알려 준다. 창 열기·닫기 키(C, V, ESC 등)가 글자 입력과 겹치지 않게 할 때 쓴다.
    public static class UIInputFocus
    {
        public static bool IsTyping
        {
            get
            {
                EventSystem eventSystem = EventSystem.current;
                if (eventSystem == null || eventSystem.currentSelectedGameObject == null) return false;

                return eventSystem.currentSelectedGameObject.TryGetComponent(out TMP_InputField input) && input.isFocused;
            }
        }
    }
}
