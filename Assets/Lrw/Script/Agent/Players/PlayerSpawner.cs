using Lrw.Script._Core._ServiceLocator;
using Lrw.Script.Agent.Players.CharacterSystem;
using UnityEngine;

namespace Lrw.Script.Agent.Players
{
    public class PlayerSpawner : MonoBehaviour
    {
        private void Start()
        {
            ServiceLocator.Get<ICharacterManager>().CreateCharacter(transform.position);
        }
    }
}