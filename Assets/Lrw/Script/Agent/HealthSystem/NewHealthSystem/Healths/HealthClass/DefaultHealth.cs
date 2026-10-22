using DevLib.ModuleSystem;
using Lrw.Script.Agent.HealthSystem.NewHealthSystem.Healths.HealthSO;

namespace Lrw.Script.Agent.HealthSystem.NewHealthSystem.Healths.HealthClass
{
    /// <summary>
    /// 특수 동작이 없는 체력. 기본 체력, 일반 보호막, 흡혈 초과 체력(HealSources = LifeSteal) 등은 데이터 설정만으로 만든다.
    /// </summary>
    public class DefaultHealth : AbstractHealth
    {
        public DefaultHealth(DefaultHealthSo data, ModuleOwner owner) : base(data, owner)
        {
        }
    }
}
