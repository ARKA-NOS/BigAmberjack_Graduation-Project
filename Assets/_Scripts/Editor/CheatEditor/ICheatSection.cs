namespace _Scripts.Editor.CheatEditor
{
    // 치트 창의 한 칸. 새 치트는 이 인터페이스를 구현하고 CheatWindow에 등록해 추가한다.
    public interface ICheatSection
    {
        string Title { get; }

        // 플레이 중에만 호출된다.
        void Draw();

        // 플레이 중 주기적으로 호출된다. 계속 유지해야 하는 치트에 쓴다.
        void Tick();

        // 창을 닫을 때 호출된다. 켜 둔 치트를 되돌린다.
        void Release();
    }
}
