namespace Lrw.Script.Agent
{
    public interface IGroundChecker
    {
        bool FixedIsGround { get; }
        bool IsGroundChecking();
    }
}