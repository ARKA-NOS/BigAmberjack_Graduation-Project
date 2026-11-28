using UnityEngine;

namespace _Scripts.UI.UI_System
{
    // 창이 열리고 닫힐 때의 움직임. 투명도와 입력 차단은 창이 맡고, 여기서는 움직임만 담당한다.
    // 창이 시간을 멈추므로 구현은 unscaled 시간으로 움직여야 한다.
    public abstract class WindowTransition : MonoBehaviour
    {
        public abstract void Show();

        public abstract void Hide();

        // 연출 없이 바로 닫힌 상태로 둔다.
        public abstract void SnapHidden();
    }
}
