using _Scripts._Core._ServiceLocator;
using UnityEngine;

namespace _Scripts.UI
{
    // ScreenFader가 없는 씬에서 쓰는 빈 구현. 전환 중이 아닌 것으로 본다.
    public class NullScreenFadeState : IScreenFadeState
    {
        public bool IsFading => false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            ServiceLocator.Register<IScreenFadeState>(new NullScreenFadeState());
        }
    }
}
