using System;

namespace Lrw.Script.Agent
{
    public interface IAnimationTrigger
    {
        event Action OnAnimationEnd;
        event Action OnAttackTrigger;
    }
}