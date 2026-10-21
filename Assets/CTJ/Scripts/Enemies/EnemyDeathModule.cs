using DevLib.ModuleSystem;
using Lrw.Script._Core._ServiceLocator;
using UnityEngine;

namespace CTJ.Enemies
{
    // 사망 연출/제거는 EnemyBase에 남기고, 이 모듈은 실제 사망을 집계 매니저에 한 번만 알립니다.
    [DisallowMultipleComponent]
    public sealed class EnemyDeathModule : Module
    {
        private EnemyBase _enemy;
        private bool _reported;

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _enemy = owner as EnemyBase;
        }

        internal void ReportDeath()
        {
            if (_reported || !isActiveAndEnabled || _enemy == null || !_enemy.IsDead) return;
            _reported = true;

            // 적 생성 뒤 매니저가 생성되어도 사망 시점에 조회할 수 있습니다.
            EnemyKillCountManager manager = ServiceLocator.Get<EnemyKillCountManager>();
            if (manager == null || !manager.isActiveAndEnabled)
            {
                Debug.LogWarning($"{_enemy.name}: 활성 EnemyKillCountManager가 없어 처치 수를 집계하지 못했습니다.", this);
                return;
            }
            manager.RegisterDeath();
        }
    }
}
