using DevLib.BattleSystem;
using UnityEngine;

namespace CTJ.Testing
{
    // IDamageable이 아직 없는 테스트 타깃에만 붙입니다. 실제 체력 시스템은 아닙니다.
    public sealed class EnemyDamageProbe : MonoBehaviour, IDamageable
    {
        [SerializeField] private int receivedHitCount;
        [SerializeField] private float totalDamage;

        public int ReceivedHitCount => receivedHitCount;
        public float TotalDamage => totalDamage;

        public void ApplyDamage(DamageData damageData, Vector2 hitPoint,
            Vector2 hitDirection, Vector2 hitNormal)
        {
            receivedHitCount++;
            totalDamage += damageData.DamageAmount;
            Debug.Log($"[CTJ 피격 테스트] {name}: {damageData.DamageAmount} 피해, " +
                      $"누적 {receivedHitCount}회 / {totalDamage} 피해", this);
        }
    }
}
