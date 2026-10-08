using System;

namespace Lrw.Script.Agent.ControlSystem
{
    public interface IPlayable
    {
        event Action<PlayContext> OnPlay;
    }
}