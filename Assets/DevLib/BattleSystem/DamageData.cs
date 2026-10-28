using DevLib.ModuleSystem;
using Lrw.Script.Agent.HealthSystem.NewHealthSystem;
using UnityEngine;

namespace DevLib.BattleSystem
{
    public struct DamageData
    {
        public AttackTypeEnum AttackTypeEnum;
        public float DamageAmount;
        public bool IsCritical;
        public ModuleOwner Dealer;
        public Vector2 DirectedKBForce; //방향성을 부여받은 넉백힘
        public float LifeStealRatio;
        public HealthType TargetType;
    }
}