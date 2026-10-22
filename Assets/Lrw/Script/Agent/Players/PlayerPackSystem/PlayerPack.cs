using Lrw.Script._Core._ServiceLocator;
using UnityEngine;

namespace Lrw.Script.Agent.Players.PlayerPackSystem
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
            => transform.position = pos;
        public void Destroy() 
            => Destroy(gameObject);
        
        
    }
}