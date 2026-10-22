namespace Lrw.Script.Agent.HealthSystem.NewHealthSystem
{
    public struct HealthChangeData
    {
        public float Amount;
        public bool IsCritical;
        public HealthType TargetType;
        
        public HealthChangeData(float amount,HealthType type = HealthType.Normal,bool isCritical = false)
        {
            Amount = amount;
            TargetType = type;
            IsCritical = isCritical;
        }
        
    }
}