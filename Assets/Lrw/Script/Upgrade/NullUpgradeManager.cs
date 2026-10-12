using Lrw.Script._Core._ServiceLocator;
using UnityEngine;

namespace Lrw.Script.Upgrade
{
    public class NullUpgradeManager : IUpgradeManager
    {
        public UpgradeTree GetUpgradeTree(string treeName) => null;


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            ServiceLocator.Register<IUpgradeManager>(new NullUpgradeManager());
        }
        
    }
}