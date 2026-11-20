using _Scripts._Core._ServiceLocator;
using UnityEngine;

namespace _Scripts.Agent.Players.PlayerPackSystem
{
    public class NullPlayerPack : IPlayerPack
    {
        public bool IsExist() => false;
        public void SetPos(Vector3 pos) {}
        public void Destroy() {}

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Init()
        {
            ServiceLocator.Register<IPlayerPack>(new NullPlayerPack());
        }
    }
}