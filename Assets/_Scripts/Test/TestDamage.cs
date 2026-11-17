using _Scripts.Agent.Players;
using _Scripts.DevLib.BattleSystem;
using UnityEngine;

namespace _Scripts.Test
{
    public class TestDamage : MonoBehaviour
    {
        [SerializeField] private PlayerController playerController;
        [SerializeField] private float value = 5f;
        [SerializeField] private float knockbackForce = 3f;

        [SerializeField] private float delay = 5f;

        private float startTime;
        private void Update()
        {
            if (Time.time - startTime > delay)
            {
                startTime = Time.time;
                OnHealthChanged();
            }
        }
        
        private void OnHealthChanged()
        {
            if (playerController == null) return;
            
            Vector2 direction = (playerController.transform.position - transform.position).normalized;
            DamageData data = new DamageData
            {
                DamageAmount = value,
                Dealer = playerController,
                DirectedKBForce = direction * knockbackForce,
            };
            
            playerController.ApplyDamage(data,Vector2.zero,direction,-direction);
        }
    }
}