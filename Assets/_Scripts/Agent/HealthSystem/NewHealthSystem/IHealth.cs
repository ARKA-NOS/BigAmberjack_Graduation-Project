using UnityEngine;

namespace _Scripts.Agent.HealthSystem.NewHealthSystem
{
    public interface IHealth
    {
        int Priority { get; }
        float HP { get; }
        float ApplyDamage(float damage);
        float ApplyHeal(float heal);
        void SetHp(float hp);
        void HealthUpdate();
        
        Color GetHealthColor();
        HealthType GetHealthType();
    }
}