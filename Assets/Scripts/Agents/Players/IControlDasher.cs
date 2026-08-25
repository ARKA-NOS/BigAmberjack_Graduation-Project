namespace Agents.Players
{
    public interface IControlDasher
    {
        bool CanDash();
        void Dash();
        float DashTime { get; }
        void ResetAirDashCount();
    }
}