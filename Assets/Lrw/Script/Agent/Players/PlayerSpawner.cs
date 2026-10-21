using Lrw.Script._Core._ServiceLocator;
using Lrw.Script.Agent.Players.CharacterSystem;
using Lrw.Script.Agent.Players.PlayerPackSystem;
using UnityEngine;

namespace Lrw.Script.Agent.Players
{
    public class PlayerSpawner : MonoBehaviour
    {
        private void Awake()
        {
            IPlayerPack playerPack = ServiceLocator.Get<IPlayerPack>();

            if (!playerPack.IsExist())
                CreatePlayer();
            
            Destroy(gameObject);
        }
        
        private void CreatePlayer()
            => ServiceLocator.Get<ICharacterManager>().CreateCharacter(transform.position);
    }
}