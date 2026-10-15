using Lrw.Script._Core._ServiceLocator;
using UnityEngine;

namespace Lrw.Script.Agent.Players
{
    public class NullPlayerController : IPlayerController
    {
        public void SetInputActive(bool active) {}

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            ServiceLocator.Register<IPlayerController>(new NullPlayerController());
        }
    }
}