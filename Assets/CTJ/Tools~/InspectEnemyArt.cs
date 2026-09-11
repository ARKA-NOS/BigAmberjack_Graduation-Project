using System;
using System.Linq;
using System.Threading.Tasks;
using CTJ.Enemies;
using CTJ.Testing;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CTJ.Tools
{
    public static class InspectEnemyArt
    {
        private const string SessionKey = "CTJ.EnemyArtTest.PreviousStartScene";
        private const string SessionActive = "CTJ.EnemyArtTest.Active";

        public static string BeginPlayTest()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("A Play session is already running.");
            if (SessionState.GetBool(SessionActive, false))
                throw new InvalidOperationException("Finish the previous CTJ Play test first.");
            SessionState.SetString(SessionKey, AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
            SessionState.SetBool(SessionActive, true);
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(
                "Assets/CTJ/Prototypes/Scenes/EnemyArtTest.unity");
            EditorApplication.isPlaying = true;
            return "Starting CTJ test scene. Original edit-mode scene setup is preserved by Unity.";
        }

        public static object ReadPlayTest()
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Play Mode is not running.");
            return new
            {
                time = Time.time,
                scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,
                enemies = Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None).Select(enemy => new
                {
                    name = enemy.name,
                    position = enemy.transform.position.ToString(),
                    target = enemy.Target != null ? enemy.Target.name : null,
                    attacking = enemy.IsAttackInProgress,
                    healthModuleReady = enemy.HealthModule != null,
                    maxHealth = enemy.HealthModule != null ? enemy.HealthModule.MaxHealth : 0f,
                    moving = enemy.GetComponentInChildren<Animator>().GetBool("IsMoving"),
                    sprite = enemy.GetComponentInChildren<SpriteRenderer>().sprite.name
                }).ToArray(),
                targets = Object.FindObjectsByType<EnemyDamageProbe>(FindObjectsSortMode.None).Select(probe => new
                {
                    name = probe.name, hits = probe.ReceivedHitCount, damage = probe.TotalDamage
                }).ToArray(),
                projectiles = Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None).Length
            };
        }

        public static async Task<object> CheckDodgeAndFacing()
        {
            if (!EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().path !=
                "Assets/CTJ/Prototypes/Scenes/EnemyArtTest.unity")
                throw new InvalidOperationException("Run only in the CTJ EnemyArtTest Play session.");
            MeleeEnemy cat = Object.FindFirstObjectByType<MeleeEnemy>();
            RangedEnemy ghost = Object.FindFirstObjectByType<RangedEnemy>();
            Transform catTarget = cat.Target;
            Transform ghostTarget = ghost.Target;
            EnemyDamageProbe catProbe = catTarget.GetComponent<EnemyDamageProbe>();
            EnemyDamageProbe ghostProbe = ghostTarget.GetComponent<EnemyDamageProbe>();
            Vector3 catOriginal = catTarget.position;
            Vector3 ghostOriginal = ghostTarget.position;
            try
            {
                await WaitUntil(() => !cat.IsAttackInProgress);
                await WaitUntil(() => cat.IsAttackInProgress);
                int beforeDodge = catProbe.ReceivedHitCount;
                catTarget.position += Vector3.up * 3f;
                Physics2D.SyncTransforms();
                await WaitUntil(() => !cat.IsAttackInProgress);
                if (catProbe.ReceivedHitCount != beforeDodge)
                    throw new InvalidOperationException("Catto damaged a target that dodged before the hit event.");

                catTarget.position = new Vector3(cat.transform.position.x - 1f, 0.48f, 0f);
                Physics2D.SyncTransforms();
                await WaitUntil(() => catProbe.ReceivedHitCount > beforeDodge);
                bool catFacesLeft = cat.transform.Find("Visual").localScale.x < 0f;
                int beforeGhost = ghostProbe.ReceivedHitCount;
                ghostTarget.position = new Vector3(2.3f, 0.48f, 0f);
                Physics2D.SyncTransforms();
                await WaitUntil(() => ghostProbe.ReceivedHitCount > beforeGhost);
                bool ghostFacesLeft = ghost.transform.Find("Visual").localScale.x < 0f;
                if (!catFacesLeft || !ghostFacesLeft)
                    throw new InvalidOperationException("An enemy hit without facing the left-side target.");
                return new { dodgeDamage = 0, catLeftHit = true, ghostLeftHit = true };
            }
            finally
            {
                if (catTarget != null) catTarget.position = catOriginal;
                if (ghostTarget != null) ghostTarget.position = ghostOriginal;
                Physics2D.SyncTransforms();
            }
        }

        private static async Task WaitUntil(Func<bool> condition)
        {
            DateTime deadline = DateTime.UtcNow.AddSeconds(6);
            while (!condition())
            {
                if (!EditorApplication.isPlaying || DateTime.UtcNow > deadline)
                    throw new InvalidOperationException("Play test condition timed out.");
                await Task.Yield();
            }
        }

        public static string StopPlayTest()
        {
            if (!SessionState.GetBool(SessionActive, false)) throw new InvalidOperationException("No CTJ test session.");
            EditorApplication.isPlaying = false;
            return "Stopping CTJ Play test.";
        }

        public static string RestoreStartScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Wait for Play Mode to stop.");
            if (!SessionState.GetBool(SessionActive, false)) return "No CTJ session to restore.";
            string previous = SessionState.GetString(SessionKey, "");
            EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(previous) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(previous);
            SessionState.EraseString(SessionKey);
            SessionState.EraseBool(SessionActive);
            return "Restored original Play Mode start scene.";
        }
    }
}
