using CTJ.Enemies.Bosses;
using DevLib.BattleSystem;
using Lrw.Script.Agent.HealthSystem;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CTJ.Testing
{
    // BossPatternTest 전용 조작 타깃. 실제 PlayerPack이나 게임 UI를 대체하지 않습니다.
    public sealed class BossTestControls : MonoBehaviour
    {
        [SerializeField] private RobotEyeBoss boss;
        [SerializeField] private EnemyDamageProbe probe;
        [SerializeField] private Rigidbody2D targetBody;
        private bool _jumpRequested;

        private void Update()
        {
            if (Keyboard.current?.spaceKey.wasPressedThisFrame == true) _jumpRequested = true;
        }

        private void FixedUpdate()
        {
            if (targetBody == null) return;
            Keyboard keyboard = Keyboard.current;
            float direction = 0f;
            if (keyboard != null)
            {
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) direction--;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) direction++;
            }
            targetBody.linearVelocityX = direction * 7f;
            if (_jumpRequested && Physics2D.Raycast(targetBody.position, Vector2.down, 0.7f, 1 << LayerMask.NameToLayer("Ground")))
                targetBody.linearVelocityY = 10f;
            _jumpRequested = false;
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(12f, 12f, 410f, 210f), GUI.skin.box);
            GUILayout.Label("CTJ Robot Eye - pattern test (dummy target)");
            GUILayout.Label("A/D or arrows: move | Space: jump | Restart: stop/play");
            GUILayout.Label($"Target hits: {probe?.ReceivedHitCount ?? 0} / damage: {probe?.TotalDamage ?? 0}");
            if (boss != null)
            {
                GUILayout.Label($"Boss HP: {boss.GetModule<IHealthModule>()?.CurrentHealth} / 2500 | {boss.CurrentState}");
                GUILayout.Label($"Invulnerable: {boss.IsInvulnerable} | Laser used: {boss.HasUsedLaserPhase} | Passes: {boss.CompletedTraversals}/3");
                if (GUILayout.Button("Deal 250 damage")) Hit(250f);
                if (GUILayout.Button("Deal 1500 damage (2500 -> 1000)")) Hit(1500f);
                if (GUILayout.Button("Deal 3000 damage (ignored during invulnerability)")) Hit(3000f);
            }
            else GUILayout.Label("Boss destroyed. Stop/play to restart.");
            GUILayout.EndArea();
        }

        private void Hit(float amount) => boss.ApplyDamage(new DamageData { DamageAmount = amount },
            boss.transform.position, Vector2.right, Vector2.left);
    }
}
