namespace Agents.Players
{
    public interface IControlMultiJumper
    {
        bool CanDoubleJump();

        void ResetMultiJumpCount();
    }
}