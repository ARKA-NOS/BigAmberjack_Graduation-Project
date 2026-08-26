using DevLib.BattleSystem;
using UnityEngine;

namespace Agents.Enemies
{
    /// <summary>
    /// Test용 Enemy입니다.
    /// 이후에 다른 적을 만들 때는 무시하고 만들어도 됩니다.
    /// </summary>
    public class TestEnemy : Agent, IDamageable
    {
        [SerializeField] private Transform vfxTrm;
        
        public void ApplyDamage(DamageData damageData, Vector2 hitPoint, Vector2 hitDirection, Vector2 hitNormal)
        {
            OnHit?.Invoke();
            vfxTrm.right = hitDirection;
        }
    }
}