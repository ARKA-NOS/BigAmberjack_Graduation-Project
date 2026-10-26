using System;
using Lrw.Script.DataStructure;
using UnityEngine;

namespace Lrw.Script.Agent.HealthSystem.NewHealthSystem
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