using System;
using System.Collections;
using CoreSystem.Effect;
using CoreSystem.EffectSystem;
using DevLib.BattleSystem;
using DevLib.ModuleSystem;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Agents.Players
{
    public class PlayerMeleeSkillModule : Module
    {
        [SerializeField] private Transform slashTrm;
        [SerializeField] private AssetNameSo[] attackSlashBundle;
        [SerializeField] private float attackCoolTime = 0.4f;
        [SerializeField] private float attackDmg = 2;
        
        private IVfxModule _vfxModule;
        private PlayerController _playerController;
        private Camera _camera;
        private AbstractDamageCaster _damageCaster;

        private int _currentAttackSequence;
        private bool _canAttack = true;
        
        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);

            _playerController = owner as PlayerController;

            if (_playerController != null)
            {
                _playerController.PlayerInput.OnAttackKeyPressed += HandleMeleeUseSkill;
            }

            _vfxModule = owner.GetModule<IVfxModule>();
            Debug.Assert(_vfxModule != null, "VFX 모듈 좀 넣어줍쇼...");

            _camera = Camera.main;
            Debug.Assert(_camera != null, "Main Camera가 없습니다.");
            
            _damageCaster = GetComponentInChildren<AbstractDamageCaster>();
            Debug.Assert(_damageCaster != null, "AbstractDamageCaster가 없습니다.");
            _damageCaster.InitCaster(owner);
        }

        private void OnDestroy()
        {
            if (_playerController != null)
            {
                _playerController.PlayerInput.OnAttackKeyPressed -= HandleMeleeUseSkill;
            }
        }

        private void HandleMeleeUseSkill()
        {
            if (!_canAttack || attackSlashBundle.Length == 0)
                return;

            LookAtMouse();

            _vfxModule.PlayVfx(
                attackSlashBundle[_currentAttackSequence].AssetHash
            );

            _currentAttackSequence =
                (_currentAttackSequence + 1) % attackSlashBundle.Length;

            StartCoroutine(AttackCoolTimeCount());
            HandleDmgCast();
        }

        private void HandleDmgCast()
        {
            _damageCaster.CastDamage(attackDmg, transform.forward, 0);
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

        private IEnumerator AttackCoolTimeCount()
        {
            _canAttack = false;

            yield return new WaitForSeconds(attackCoolTime);

            _canAttack = true;
        }
    }
}