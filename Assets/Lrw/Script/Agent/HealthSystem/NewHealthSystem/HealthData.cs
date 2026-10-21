namespace Lrw.Script.Agent.HealthSystem.NewHealthSystem
{
    public struct HealthData
    {
        public float Health;
        public float Damage;
        
        public HealthData(float hp,float dmg)
		{
			Health = hp;
			Damage = dmg;
    	}
	}
}