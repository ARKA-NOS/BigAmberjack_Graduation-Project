using Lrw.Script.Currency;
using UnityEngine;

namespace Lrw.Script.Upgrade
{
    [CreateAssetMenu(fileName = "Upgrade Node SO", menuName = "Upgrade/Upgrade Node SO", order = 0)]
    public class UpgradeNodeSO : ScriptableObject
    {
        [field:SerializeField] public string NodeName {get; private set;}
        [field:SerializeField] public Sprite NodeIcon { get; private set; }
        [field:SerializeField,TextArea] public string NodeDescription {get; private set;}
        
        [field:SerializeField] public UpgradeNodeSO[] PrevNodes { get; private set; }
        [field:SerializeField] public Cost[] UpgradeCost { get; private set; }
        [field:SerializeField] public AbstractUpgradeData UpgradeData { get; private set; }
    }
}