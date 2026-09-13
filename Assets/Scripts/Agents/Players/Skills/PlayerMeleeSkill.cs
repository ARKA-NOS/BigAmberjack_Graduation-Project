using CoreSystem.Effect;
using CoreSystem.EffectSystem;
using DevLib.BattleSystem;
using DevLib.ModuleSystem;
using Lrw.Script.Agent.SkillSystem.NormalSkill;
using Lrw.Script.Agent.StatSystem;
using UnityEngine;
using UnityEngine.InputSystem;
namespace Agents.Players.Skills
{
    public class PlayerMeleeSkill : AbstractNormalSkill
    {
        [SerializeField] private Transform slashTrm;
        [SerializeField] private AssetNameSo[] attackSlashBundle;
        [SerializeField] private float attackCoolTime = 0.4f;
        [SerializeField] private StatData damageStatSo;
        
        private IVfxModule _vfxModule;
        private Camera _camera;
        private AbstractDamageCaster _damageCaster;
        private IStatModule _statModule;
        private Stat _damageStat;
        

        private int _currentAttackSequence;

        public override float GetMaxCooldown()
            => SkillSo.BaseCooldown;
        
        public override void InitSkill(ModuleOwner owner)
        {
            _vfxModule = owner.GetModule<IVfxModule>();
            FDebug.Assert(_vfxModule != null, "VFX 모듈 좀 넣어줍쇼...");

            _camera = Camera.main;
            FDebug.Assert(_camera != null, "Main Camera가 없습니다.");
            
            _statModule = owner.GetModule<IStatModule>();
            
            FDebug.Assert(_statModule != null,"StatModule is not found");
            
            _damageStat = _statModule.GetStat(damageStatSo,1f);
            
            _damageCaster = GetComponentInChildren<AbstractDamageCaster>();
            Debug.Assert(_damageCaster != null, "AbstractDamageCaster가 없습니다.");
            _damageCaster.InitCaster(owner);
        }

        public override bool CanUseSkill()
        {
            return base.CanUseSkill() && attackSlashBundle.Length != 0;
        }

        public override void UseSkill()
        {
            base.UseSkill();

            LookAtMouse();

            _vfxModule.PlayVfx(
                attackSlashBundle[_currentAttackSequence].AssetHash
            );

            _currentAttackSequence =
                (_currentAttackSequence + 1) % attackSlashBundle.Length;
            
            HandleDmgCast();
        }

        private void HandleDmgCast()
        {
            _damageCaster.CastDamage(_damageStat.Value, transform.forward, 0);
        }

        private void LookAtMouse()
        {
            Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();

            Vector3 mouseWorldPosition = _camera.ScreenToWorldPoint(
                new Vector3(
                    mouseScreenPosition.x,
                    mouseScreenPosition.y,
                    Mathf.Abs(_camera.transform.position.z - slashTrm.position.z)
                )
            );

            Vector2 direction = mouseWorldPosition - slashTrm.position;

            slashTrm.right = direction;
            transform.right = direction;
        }
        

        

        
    }
}