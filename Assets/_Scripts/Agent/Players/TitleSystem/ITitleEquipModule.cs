using System;

namespace _Scripts.Agent.Players.TitleSystem
{
    // 달성한 칭호를 장착/해제하는 인터페이스. 장착한 칭호만 효과가 적용된다.
    // 장착은 달성한 칭호만 할 수 있고, 달성만 하고 장착하지 않은 칭호는 효과가 없다.
    public interface ITitleEquipModule
    {
        // 장착 상태가 바뀌었을 때.
        event Action OnEquipChanged;

        bool IsEquipped(AbstractTitleSo title);

        // 달성한 칭호만 장착/해제할 수 있다. 바뀌었으면 true.
        bool TrySetEquipped(AbstractTitleSo title, bool equipped);
    }
}
