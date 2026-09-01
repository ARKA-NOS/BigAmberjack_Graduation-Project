using DevLib.BattleSystem;
using UnityEngine;

namespace CTJ.Enemies
{
    public class EnemyPrototype : EnemyBase
    {
        protected override void Attack()
        {
            Debug.Log($"{name}: Attack");
        }

        public override void ApplyDamage(DamageData damageData, Vector2 hitPoint, Vector2 hitDirection, Vector2 hitNormal)
        {
            Debug.Log($"{name}: Damage {damageData.DamageAmount}");
        }
    }
}