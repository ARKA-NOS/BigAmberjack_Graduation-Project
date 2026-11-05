using _Scripts.UI.BindSystem;
using UnityEngine;

namespace _Scripts.Item.ItemTooltip
{
    // 지금 상호작용 대상인 바닥 아이템. 대상이 없으면 null.
    [CreateAssetMenu(fileName = "Item Tooltip View Module", menuName = "UI/Item Tooltip View Module")]
    public class ItemTooltipViewModel : AbstractUIViewModel<IItemTooltipSource>
    {
    }
}
