namespace Lrw.Script.Agent.Players.Modules
{
    public interface IControlDasher
    {
        bool CanDash();
        void Dash();
        float DashTime { get; }
        void ResetAirDashCount();
    }
}