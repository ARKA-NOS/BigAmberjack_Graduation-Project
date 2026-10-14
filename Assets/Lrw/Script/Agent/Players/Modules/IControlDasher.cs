namespace Lrw.Script.Agent.Players
{
    public interface IControlDasher
    {
        bool CanDash();
        void Dash();
        float DashTime { get; }
        void ResetAirDashCount();
    }
}