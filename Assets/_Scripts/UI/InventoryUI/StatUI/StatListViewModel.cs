using System.Collections.Generic;
using _Scripts.Agent.StatSystem;
using _Scripts.UI.BindSystem;
using UnityEngine;

namespace _Scripts.UI.InventoryUI.StatUI
{
    [CreateAssetMenu(fileName = "Stat List View Module", menuName = "UI/Stat List View Module")]
    public class StatListViewModel : AbstractUIViewModel<IReadOnlyDictionary<StatData, Stat>>
    {
    }
}
