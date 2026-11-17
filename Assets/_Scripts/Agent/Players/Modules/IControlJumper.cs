namespace _Scripts.Agent.Players.Modules
{
    public interface IControlJumper
    {
        bool IsJumpFall { get; set; }
        bool CanCoyoteJump();
        void ResetCoyote();
        void Jump();
        void CancelJump();
    }
}