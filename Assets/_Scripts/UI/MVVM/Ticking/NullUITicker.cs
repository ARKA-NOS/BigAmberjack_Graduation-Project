using _Scripts._Core._ServiceLocator;
using UnityEngine;

namespace _Scripts.UI.MVVM.Ticking
{
    // UIManager가 없을 때(GameManager가 없는 테스트 씬 등) 쓰는 빈 Ticker. 이때 Tick 기반 값은 갱신되지 않는다.
    public class NullUITicker : IUITicker
    {
        public void Register(ITickableViewModel viewModel) { }
        public void Unregister(ITickableViewModel viewModel) { }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            ServiceLocator.Register<IUITicker>(new NullUITicker());
        }
    }
}
