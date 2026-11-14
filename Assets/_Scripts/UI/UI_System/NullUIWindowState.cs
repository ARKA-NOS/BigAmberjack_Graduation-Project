using _Scripts._Core._ServiceLocator;
using UnityEngine;

namespace _Scripts.UI.UI_System
{
    // UIManager가 없는 씬에서 쓰는 빈 구현. 열린 창이 없는 것으로 본다.
    public class NullUIWindowState : IUIWindowState
    {
        public bool HasOpenWindow => false;
        public int LastCloseFrame => -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            ServiceLocator.Register<IUIWindowState>(new NullUIWindowState());
        }
    }
}
