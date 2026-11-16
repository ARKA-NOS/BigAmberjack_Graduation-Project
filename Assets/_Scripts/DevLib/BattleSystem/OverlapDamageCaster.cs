using UnityEngine;

namespace _Scripts.DevLib.BattleSystem
{
    public class OverlapDamageCaster : AbstractDamageCaster
    {
        public enum CastType
        {
            Circle, Box
        }
        
        [SerializeField] private CastType castType;
        [SerializeField] private float radius; //circle only
        [SerializeField] private Vector2 boxSize; //box only
        [SerializeField] private Vector2 offSet;
        
        public void SetRadius(float value) => radius = value;
        public void SetBoxSize(Vector2 value) => boxSize = value;
        
        public override bool CastDamage(float damage, Vector2 direction, float kbForce)
        {
            Vector3 pos = new Vector3(transform.position.x + offSet.x, transform.position.y + offSet.y, transform.position.z);
            int cnt = castType switch
            {
                CastType.Circle => Physics2D.OverlapCircle(pos, radius, contactFilter, _hitResults),
                CastType.Box => Physics2D.OverlapBox(pos, boxSize, 0, contactFilter, _hitResults),
                _ => 0
            };

            for (int i = 0; i < cnt; i++)
            {
                if (_hitResults[i].TryGetComponent(out IDamageable damageable))
                {
                    Vector2 point = _hitResults[i].ClosestPoint(transform.position);
                    Vector2 knockbackForce = direction.normalized * kbForce;

                    DamageData damageData = new DamageData
                    {
                        DamageAmount = damage,
                        Dealer = Owner,
                        DirectedKBForce = knockbackForce
                    };
                    damageable.ApplyDamage(damageData, point, direction, -direction);
                }
            }

            return cnt > 0; //카운트가 0보다 크면 맞은거니까 true
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Vector3 pos = new Vector3(transform.position.x + offSet.x, transform.position.y + offSet.y, transform.position.z);
            Gizmos.color = Color.red;
            if(castType == CastType.Circle)
                Gizmos.DrawWireSphere(pos, radius);
            else if(castType == CastType.Box)
                Gizmos.DrawWireCube(pos, boxSize);
        }
#endif
    }
}