using _Scripts._Core._ServiceLocator;
using _Scripts.Agent.Players.CharacterSystem;
using _Scripts.Agent.Players.PlayerPackSystem;
using UnityEngine;

namespace _Scripts.Agent.Players
{
    public class PlayerSpawner : MonoBehaviour
    {
        private void Start()
        {
            IPlayerPack playerPack = ServiceLocator.Get<IPlayerPack>();

            if (!playerPack.IsExist())
                CreatePlayer();
            
            playerPack.SetPos(transform.position);
            
            Destroy(gameObject);
        }
        
        private void CreatePlayer()
        {
            ICharacterManager manager = ServiceLocator.Get<ICharacterManager>();
            manager.CreateCharacter(transform.position);
        }
    }
}