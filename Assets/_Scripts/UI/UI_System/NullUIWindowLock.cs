using _Scripts._Core._ServiceLocator;
using UnityEngine;

namespace _Scripts.UI.UI_System
{
    // UIManager가 없는 씬에서 쓰는 빈 구현. 막을 창이 없다.
    public class NullUIWindowLock : IUIWindowLock
    {
        public void SetLocked(bool locked) { }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            ServiceLocator.Register<IUIWindowLock>(new NullUIWindowLock());
        }
    }
}
