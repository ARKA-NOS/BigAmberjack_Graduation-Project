using _Scripts._Core._EventSystem;

namespace _Scripts.UI.PlayerUI.Title_UI.Equip
{
    // 칭호 장착 창을 열어 달라는 요청. 던전에 생성되는 NPC는 씬의 창을 직접 참조할 수 없어서
    // 이벤트로 요청하고, 플레이어 UI의 TitleEquipWindow가 받아 연다.
    public class TitleEquipOpenRequestEvent : IEvent
    {
    }

    public static class TitleEquipEvents
    {
        private static readonly TitleEquipOpenRequestEvent OpenRequest = new();

        public static void RequestOpen() => EventBus<TitleEquipOpenRequestEvent>.Raise(OpenRequest);
    }
}
