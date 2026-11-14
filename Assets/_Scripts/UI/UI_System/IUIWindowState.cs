namespace _Scripts.UI.UI_System
{
    // UIManager가 관리하는 창 상태. 같은 키(Esc 등)를 여러 창이 쓸 때 서로 겹치지 않게 확인하는 데 쓴다.
    public interface IUIWindowState
    {
        bool HasOpenWindow { get; }

        // 마지막으로 창이 닫힌 프레임(Time.frameCount). 같은 프레임에 다른 입력이 창을 닫았는지 확인할 때 쓴다.
        int LastCloseFrame { get; }
    }
}
