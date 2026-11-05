using System.Collections.Generic;
using Lrw.Script.Agent.StatSystem;
using Lrw.Script.UI.BindSystem;
using UnityEngine;

namespace KHO.Scripts.UI.StatUI
{
    [CreateAssetMenu(fileName = "Stat List View Module", menuName = "UI/Stat List View Module")]
    public class StatListViewModel : AbstractUIViewModel<IReadOnlyDictionary<StatData, Stat>>
    {
    }
}
