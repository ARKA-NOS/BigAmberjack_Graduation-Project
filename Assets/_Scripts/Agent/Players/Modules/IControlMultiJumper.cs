namespace _Scripts.Agent.Players.Modules
{
    public interface IControlMultiJumper
    {
        bool CanDoubleJump();

        void ResetMultiJumpCount();
    }
}