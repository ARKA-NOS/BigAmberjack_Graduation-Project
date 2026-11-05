using _Scripts._Core._ServiceLocator;
using UnityEngine;

namespace _Scripts.Upgrade
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