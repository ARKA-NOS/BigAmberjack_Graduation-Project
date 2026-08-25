using UnityEngine;

namespace Agents.Players
{
    public interface IControlJumper
    {
        float CoyoteTime { get; }
        bool IsJumpFall { get; set; }
        void Jump();
        void CancelJump();
    }
}