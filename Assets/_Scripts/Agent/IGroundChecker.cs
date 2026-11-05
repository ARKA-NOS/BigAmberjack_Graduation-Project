namespace _Scripts.Agent
{
    public interface IGroundChecker
    {
        bool FixedIsGround { get; }
        bool IsGroundChecking();
    }
}