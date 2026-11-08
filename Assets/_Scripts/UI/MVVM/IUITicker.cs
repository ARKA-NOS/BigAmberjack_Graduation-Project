namespace _Scripts.UI.MVVM
{
    // 등록된 ITickableViewModel의 Tick을 매 프레임 호출한다. UIManager가 구현하고 ServiceLocator에 등록한다.
    public interface IUITicker
    {
        void Register(ITickableViewModel viewModel);
        void Unregister(ITickableViewModel viewModel);
    }
}
