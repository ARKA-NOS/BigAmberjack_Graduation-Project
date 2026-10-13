using System;

namespace Agents
{
    public interface IAnimationTrigger
    {
        event Action OnAnimationEnd;
        event Action OnAttackTrigger;
    }
}