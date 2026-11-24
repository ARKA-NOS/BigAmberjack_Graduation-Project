namespace _Scripts.UI
{
    // 화면 전환(Fade) 진행 여부만 알려주는 읽기 전용 인터페이스.
    // 설정 창처럼 전환 도중에 열리면 안 되는 쪽이 ScreenFader를 직접 참조하지 않고 ServiceLocator로 조회한다.
    public interface IScreenFadeState
    {
        bool IsFading { get; }
    }
}
