using CTJ.Enemies;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SDW.Scripts.Testing
{
    public class DebugKillAllEnemies : MonoBehaviour
    {
        private void Update()
        {
            if (Keyboard.current == null || !Keyboard.current.pKey.wasPressedThisFrame)
                return;

            foreach (EnemyBase enemy in FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))
                Destroy(enemy.gameObject);
        }
    }
}
