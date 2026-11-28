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

        // 지금 이 칭호를 장착할 때 내는 코인. 칭호별 최초 장착은 0(무료)이고 다시 장착할 때부터 비용이 든다.
        // 해제는 항상 무료다.
        int GetEquipCost(AbstractTitleSo title);

        // 지금 가진 코인으로 이 칭호의 장착 비용을 낼 수 있는지.
        bool CanPayEquipCost(AbstractTitleSo title);

        // 달성한 칭호만 장착/해제할 수 있다. 장착 비용을 못 내면 장착하지 않는다. 바뀌었으면 true.
        bool TrySetEquipped(AbstractTitleSo title, bool equipped);
    }
}
