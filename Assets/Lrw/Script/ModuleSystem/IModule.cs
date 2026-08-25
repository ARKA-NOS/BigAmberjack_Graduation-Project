namespace Script.ModuleSystem
{
    public interface IModule
    {
        void Initialize(ModuleOwner owner);
        void AfterInitialize() { }
    }
}