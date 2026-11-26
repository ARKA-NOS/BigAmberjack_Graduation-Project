using System.Linq;
using _Scripts.Agent.Enemies;
using _Scripts.DevLib.BattleSystem;
using UnityEditor;
using UnityEngine;

namespace _Scripts.Editor.CheatEditor
{
    public class EnemyCheatSection : ICheatSection
    {
        private const float KillDamage = 1_000_000f;

        public string Title => "적";

        public void Draw()
        {
            EnemyBase[] enemies = Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None)
                .Where(enemy => !enemy.IsDead)
                .ToArray();

            EditorGUILayout.LabelField("살아 있는 적", enemies.Length.ToString());

            if (!GUILayout.Button("모두 처치")) return;

            // 일반 피격 경로를 써서 사망 연출과 보상이 그대로 동작하게 한다.
            foreach (EnemyBase enemy in enemies)
            {
                ((IDamageable)enemy).ApplyDamage(new DamageData { DamageAmount = KillDamage },
                    enemy.transform.position, Vector2.zero, Vector2.zero);
            }
        }

        public void Tick() { }

        public void Release() { }
    }
}
