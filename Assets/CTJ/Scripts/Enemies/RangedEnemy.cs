using UnityEngine;

namespace CTJ.Enemies
{
    // 기존 프리팹의 GUID/타입 참조를 유지합니다. 공격 종류는 Skills 목록이 결정합니다.
    [RequireComponent(typeof(EnemySkillModule))]
    public sealed class RangedEnemy : EnemyBase
    {
        private void Reset()
        {
            detectionRange = 10f;
            attackRange = 7f;
        }
    }
}
