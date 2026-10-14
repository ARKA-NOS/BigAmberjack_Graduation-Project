namespace Lrw.Script.Agent.Players
{
    public interface IControlMultiJumper
    {
        bool CanDoubleJump();

        void ResetMultiJumpCount();
    }
}