using CTJ.Enemies;
using DevLib.BattleSystem;
using DevLib.ModuleSystem;
using UnityEngine;

namespace Lrw.Script.Agent.Players.Skills.SkillBullet
{
    [RequireComponent(typeof(Collider2D),typeof(Rigidbody2D))]
    public class Bullet : MonoBehaviour
    {
        [SerializeField] private float lifeTime = 10f;
        
        private Rigidbody2D _rigidbody;
        
        private Vector2 _moveSpeed;
        
        private ModuleOwner _owner;
        private float _damage;
        private float _force;
        private float _startTime;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _startTime = Time.time;
        }

        public void Init(Vector2 speed, ModuleOwner owner,float damage,float force)
        {
            _owner = owner;
            _damage = damage;
            _force = force;
            _moveSpeed = speed;
        }

        private void FixedUpdate()
        {
            if (_rigidbody != null)
            {
                _rigidbody.linearVelocity = _moveSpeed;
            }
        }

        private void Update()
        {
            if (Time.time - _startTime >= lifeTime)
            {
                Destroy(gameObject);
            }
        }
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if(!other.TryGetComponent(out EnemyBase damageable)) return;    
            
            Vector2 point = other.ClosestPoint(transform.position);
            
            Vector2 direction = other.transform.position - transform.position;
            Vector2 knockbackForce = direction.normalized * _force;
            
            DamageData damageData = new DamageData
            {
                DamageAmount = _damage,
                Dealer = _owner,
                DirectedKBForce = knockbackForce
            };
            
            damageable.ApplyDamage(damageData, point, direction, -direction);
            Destroy(gameObject);
        }
        
    }
}