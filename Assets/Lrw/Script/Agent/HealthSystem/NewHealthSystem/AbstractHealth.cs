using System;
using DevLib.ModuleSystem;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace Lrw.Script.Agent.HealthSystem.NewHealthSystem
{
    /// <summary>
    /// 체력 한 층. 값과 자기 규칙만 가지고, 데미지/회복 분배와 외부 알림은 NewHealthModule이 맡는다.
    /// </summary>
    public abstract class AbstractHealth
    {
        public readonly int Priority;

        protected readonly ModuleOwner Owner;

        protected AbstractHealth(int priority, ModuleOwner owner)
        {
            Priority = priority;
            Owner = owner;
        }
        
        public virtual void Update() {}
        
        
    }
}
