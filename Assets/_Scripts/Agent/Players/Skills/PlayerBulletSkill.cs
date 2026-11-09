using _Scripts._Core._Debug;
using _Scripts.Agent.Players.Skills.SkillBullet;
using _Scripts.Agent.SkillSystem.NormalSkill;
using _Scripts.Agent.StatSystem;
using _Scripts.DevLib.ModuleSystem;
using UnityEngine;

namespace _Scripts.Agent.Players.Skills
{
    public class PlayerBulletSkill : AbstractCoolTimeSkill
    {
        [Header("Stat Data")]
        [SerializeField] private StatData physicalDamage;
        [SerializeField] private StatData coolDownSpeed;

        [Header("Bullet Setting")] 
        [SerializeField] private GameObject prefab;
        [SerializeField] private float bulletSpeed = 5f;
        [SerializeField] private float force = 3f;
        
        private PlayerController _playerController;
        
        private Stat damageStat;
        private Stat coolDownSpeedStat;
        
        public override float CoolTime => BaseCoolTime / coolDownSpeedStat.Value;

        public override void InitSkill(ModuleOwner owner)
        {
            base.InitSkill(owner);
            
            _playerController = owner as PlayerController;
            
            FDebug.Assert(_playerController != null,
                $"[PlayerBulletSkill : {owner.name}] owner is not PlayerController");
            
            FDebug.Assert(owner.TryGetModule(out IStatModule statModule),
                $"[PlayerBulletSkill : {owner.name}] IStatModule is not found");

            damageStat = statModule.GetStat(physicalDamage);
            coolDownSpeedStat = statModule.GetStat(coolDownSpeed);
        }


        protected override void OnUseSkill()
        {
            base.OnUseSkill();
            
            Vector2 direction = (_playerController.PlayerInput.GetMouseWorldPosition() - (Vector2)transform.position).normalized;
            
            GameObject go = Instantiate(prefab);
            go.transform.position = transform.position;
            if (go.TryGetComponent(out Bullet bullet))
            {
                bullet.Init(direction * bulletSpeed, Owner, GetDamage(), force);
            }
            EndSkill();
        }

        private float GetDamage()
        {
            return damageStat.Value;
        }
        
    }
}