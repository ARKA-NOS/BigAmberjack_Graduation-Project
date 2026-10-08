using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace Lrw.Script.Upgrade
{
    [CreateAssetMenu(fileName = "Tree", menuName = "Upgrade/Tree", order = 0)]
    public class UpgradeTree : ScriptableObject
    {
        [SerializeField] private UpgradeNodeSO[] nodes;
        
        
        
    }
}