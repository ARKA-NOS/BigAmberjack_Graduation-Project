namespace _Scripts.Settings.Appliers
{
    // 설정 값을 게임에 반영하는 단위. 새 반영 대상은 이 인터페이스를 구현한 클래스를 추가하고 SettingsManager.CreateAppliers에 넣는다.
    public interface ISettingsApplier
    {
        // 처음 불러왔을 때와 값이 바뀔 때마다 호출된다. 자기 값이 그대로면 아무것도 하지 않아도 된다.
        void Apply(SettingsData data);
    }
}
