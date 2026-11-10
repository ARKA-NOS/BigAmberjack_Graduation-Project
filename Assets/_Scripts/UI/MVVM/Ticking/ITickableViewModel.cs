namespace _Scripts.UI.MVVM.Ticking
{
    // 스킬 쿨타임처럼 이벤트 없이 매 프레임 바뀌는 값을 가진 ViewModel이 구현한다.
    // 연결(Connect)되면 IUITicker에 자동으로 등록되고, Tick에서 Model 값을 읽어 Observable에 넣는다.
    // 지연 체력바·트윈 같은 화면 연출은 여기서 하지 않고 View가 맡는다.
    public interface ITickableViewModel
    {
        void Tick();
    }
}
