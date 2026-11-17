using _Scripts._Core._ServiceLocator;
using UnityEngine;

namespace _Scripts.Agent.Players.CharacterSystem
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