namespace Lrw.Script.Agent.Players.Modules
{
    public interface IControlJumper
    {
        float CoyoteTime { get; }
        bool IsJumpFall { get; set; }
        void Jump();
        void CancelJump();
    }
}