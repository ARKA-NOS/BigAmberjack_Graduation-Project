namespace Lrw.Script.Agent.Players.Modules
{
    public interface IControlMultiJumper
    {
        bool CanDoubleJump();

        void ResetMultiJumpCount();
    }
}