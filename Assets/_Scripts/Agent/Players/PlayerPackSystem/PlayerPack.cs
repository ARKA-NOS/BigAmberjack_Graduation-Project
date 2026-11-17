using _Scripts._Core._ServiceLocator;
using UnityEngine;

namespace _Scripts.Agent.Players.PlayerPackSystem
{
    public class PlayerPack : MonoBehaviour,IPlayerPack
    {
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            ServiceLocator.Register<IPlayerPack>(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Register<IPlayerPack>(new NullPlayerPack());
        }

        public bool IsExist() 
            => true;
        
        public void SetPos(Vector3 pos)
        {
            PlayerController player = GetComponentInChildren<PlayerController>();
            Vector3 anchor = player != null ? player.transform.position : transform.position;

            transform.position += pos - anchor;
        }
        public void Destroy() 
            => Destroy(gameObject);
        
        
    }
}