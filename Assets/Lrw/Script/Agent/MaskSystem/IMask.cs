using DevLib.ModuleSystem;

namespace Lrw.Script.Agent.MaskSystem
{
    public interface IMask
    {
        MaskSo MaskSo { get; }

        void Init(ModuleOwner owner);
        void Enter();
        void Update();
        void Exit();
    }
}