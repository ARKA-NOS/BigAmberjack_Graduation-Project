using System;

namespace _Scripts.Agent
{
    public interface IAnimationTrigger
    {
        event Action OnAnimationEnd;
        event Action OnAttackTrigger;
    }
}