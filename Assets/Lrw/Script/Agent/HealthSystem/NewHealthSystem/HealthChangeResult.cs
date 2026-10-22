namespace Lrw.Script.Agent.HealthSystem.NewHealthSystem
{
    public readonly struct HealthChangeResult
    {
        /// <summary> 실제로 줄거나 찬 체력의 합 </summary>
        public readonly float Applied;
        /// <summary> 어느 체력에도 들어가지 못하고 남은 양 </summary>
        public readonly float Remaining;

        public HealthChangeResult(float applied, float remaining)
        {
            Applied = applied;
            Remaining = remaining;
        }

        public override string ToString()
            => $"Applied : {Applied:0.##}, Remaining : {Remaining:0.##}";
    }
}
