namespace _Scripts.UI.UI_System
{
    // 창을 열면 안 되는 화면(결과창 등)에서 UIManager의 창 열기를 막는다.
    public interface IUIWindowLock
    {
        // 잠그면 열려 있는 창을 닫고, 풀 때까지 창 열기 요청을 무시한다.
        void SetLocked(bool locked);
    }
}
