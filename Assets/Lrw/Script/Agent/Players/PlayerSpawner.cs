using Lrw.Script._Core._ServiceLocator;
using Lrw.Script.Player.CharacterSystem;
using UnityEngine;

namespace Lrw.Script.Player
{
    public class PlayerSpawner : MonoBehaviour
    {
        private void Start()
        {
            ServiceLocator.Get<ICharacterManager>().CreateCharacter(transform.position);
        }
    }
}