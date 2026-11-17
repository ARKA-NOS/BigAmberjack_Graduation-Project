namespace _Scripts.Agent.Players.Modules
{
    // 마나(MP)가 바뀌었을 때. 값과 변화량, 최대치를 함께 알린다.
    public delegate void ManaChanged(float newValue, float delta, float max);

    // 플레이어 마나를 읽기 전용으로 내보낸다. HUD 같은 표시 쪽이 쓴다.
    // 마나 모듈이 이 인터페이스를 구현하면 HUD가 자동으로 MP를 보여준다(구현이 없으면 MP는 표시하지 않는다).
    public interface IManaModule
    {
        float CurrentMana { get; }
        float MaxMana { get; }

        // 소비, 회복 등으로 마나가 바뀌었을 때.
        event ManaChanged OnManaChanged;
    }
}
