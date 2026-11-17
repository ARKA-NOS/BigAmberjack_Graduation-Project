using UnityEngine;

namespace _Scripts.Upgrade.SO
{
    [CreateAssetMenu(fileName = "Tree", menuName = "Upgrade/Tree", order = 0)]
    public class UpgradeTreeSO : ScriptableObject
    {
        [field:SerializeField] public string TreeName { get; private set; }
        [field:SerializeField] public UpgradeNodeSO[] Nodes { get; private set; }
        
    }
}