using System;

namespace Lrw.Script.Agent.HealthSystem.NewHealthSystem
{
    public interface IHealth
    {
        float HP { get; }
        float ApplyDamage(float damage);
        float ApplyHeal(float heal);
        void SetHp(float hp);
        bool HealthUpdate();
    }
}