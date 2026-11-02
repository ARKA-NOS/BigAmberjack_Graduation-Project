using System;

namespace Lrw.Script.Agent.Players.Modules
{
    [Obsolete]
    public interface IControlDasher
    {
        bool CanDash();
        void Dash();
        float DashTime { get; }
        void ResetAirDashCount();
    }
}