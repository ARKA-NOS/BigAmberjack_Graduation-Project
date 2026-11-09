using UnityEngine;

namespace _Scripts.Agent.Players.PlayerPackSystem
{
    public interface IPlayerPack
    {
        bool IsExist();
        void SetPos(Vector3 pos);
        void Destroy();
    }
}