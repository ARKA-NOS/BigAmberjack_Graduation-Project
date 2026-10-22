using DevLib.ModuleSystem;

namespace Lrw.Script.Agent.HealthSystem.NewHealthSystem.Healths.HealthClass
{
    public class DefaultHealth : AbstractHealth
    {
        public DefaultHealth(AbstractHealthSo healthSo, ModuleOwner owner, float baseHp = 0) : base(healthSo, owner, baseHp)
        {
            
        }
    }
}