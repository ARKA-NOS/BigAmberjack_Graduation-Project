using _Scripts.Agent.HealthSystem.NewHealthSystem;
using _Scripts.Agent.Players;
using UnityEditor;
using UnityEngine;

namespace _Scripts.Editor.CheatEditor
{
    public class PlayerCheatSection : ICheatSection
    {
        private PlayerController _player;
        private bool _isInvincible;

        public string Title => "플레이어";

        public void Draw()
        {
            if (!TryGetPlayer(out PlayerController player))
            {
                EditorGUILayout.LabelField("씬에 플레이어가 없습니다.");
                return;
            }

            INewHealthModule health = player.GetModule<INewHealthModule>();
            EditorGUILayout.LabelField("체력", $"{health.CurrentHealth:0.#} (코어 최대 {health.MaxHealth:0.#})");

            bool isInvincible = EditorGUILayout.Toggle("무적", _isInvincible);
            if (isInvincible != _isInvincible)
            {
                _isInvincible = isInvincible;
                player.SetInvincibility(this, isInvincible);
            }

            using (new EditorGUI.DisabledScope(health.IsDead))
            {
                // 코어 체력만 채운다. 상한이 없는 추가 체력에 남은 회복량이 쌓이지 않게 기본 대상을 쓴다.
                if (GUILayout.Button("체력 모두 회복"))
                    health.ApplyHeal(health.MaxHealth);
            }
        }

        public void Tick()
        {
            // 씬이 바뀌어 플레이어가 새로 생겨도 무적이 유지되게 한다.
            if (_isInvincible && TryGetPlayer(out PlayerController player))
                player.SetInvincibility(this, true);
        }

        public void Release()
        {
            if (_isInvincible && TryGetPlayer(out PlayerController player))
                player.SetInvincibility(this, false);
        }

        private bool TryGetPlayer(out PlayerController player)
        {
            if (_player == null)
                _player = Object.FindFirstObjectByType<PlayerController>();

            player = _player;
            return player != null;
        }
    }
}
