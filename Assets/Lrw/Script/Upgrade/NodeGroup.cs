using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;

namespace Lrw.Script.Upgrade
{
    public class NodeGroup
    {
        public readonly List<UpgradeNodeSO> List;

        public NodeGroup()
        {
            List = new List<UpgradeNodeSO>();
        }
        
    }
}