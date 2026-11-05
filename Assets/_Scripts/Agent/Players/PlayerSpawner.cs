using _Scripts._Core._ServiceLocator;
using _Scripts.Agent.Players.CharacterSystem;
using _Scripts.Agent.Players.PlayerPackSystem;
using UnityEngine;

namespace _Scripts.Agent.Players
{
    public class PlayerSpawner : MonoBehaviour
    {
        private void Awake()
        {
            IPlayerPack playerPack = ServiceLocator.Get<IPlayerPack>();

            if (!playerPack.IsExist())
                CreatePlayer();
            
            playerPack.SetPos(transform.position);
            
            Destroy(gameObject);
        }
        
        private void CreatePlayer()
            => ServiceLocator.Get<ICharacterManager>().CreateCharacter(transform.position);
    }
}