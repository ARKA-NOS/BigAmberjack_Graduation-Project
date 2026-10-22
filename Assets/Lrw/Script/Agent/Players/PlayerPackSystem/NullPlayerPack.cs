using Lrw.Script._Core._ServiceLocator;
using UnityEngine;

namespace Lrw.Script.Agent.Players.PlayerPackSystem
{
    public class NullPlayerPack : IPlayerPack
    {
        public bool IsExist() => false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Init()
        {
            ServiceLocator.Register<IPlayerPack>(new NullPlayerPack());
        }
    }
}