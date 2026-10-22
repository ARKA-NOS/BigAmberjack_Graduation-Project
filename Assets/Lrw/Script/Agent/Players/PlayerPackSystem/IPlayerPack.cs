using UnityEngine;

namespace Lrw.Script.Agent.Players.PlayerPackSystem
{
    public interface IPlayerPack
    {
        bool IsExist();
        void SetPos(Vector3 pos);
        void Destroy();
    }
}