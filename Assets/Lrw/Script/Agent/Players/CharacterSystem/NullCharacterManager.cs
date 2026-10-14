using Lrw.Script._Core._ServiceLocator;
using UnityEngine;

namespace Lrw.Script.Agent.Players.CharacterSystem
{
    public class NullCharacterManager : ICharacterManager
    {
        public void CreateCharacter(Vector2 pos) {}

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Register()
        {
            ServiceLocator.Register<ICharacterManager>(new NullCharacterManager());
        }
    }
}