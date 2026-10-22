using System;
using Lrw.Script._Core._ServiceLocator;
using UnityEngine;

namespace Lrw.Script.Agent.Players.PlayerPackSystem
{
    public class PlayerPack : MonoBehaviour,IPlayerPack
    {
        private void Awake()
        {
            ServiceLocator.Register<IPlayerPack>(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Register<IPlayerPack>(new NullPlayerPack());
        }

        public bool IsExist() => true;
    }
}