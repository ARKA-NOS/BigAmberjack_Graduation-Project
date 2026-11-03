using System;
using System.Collections;
using DevLib.ModuleSystem;
using Lrw.Script._Core._Debug;
using Lrw.Script._Core.PhysicsSystem;
using UnityEngine;

namespace Lrw.Script.Agent.Players.Modules
{
    public class PlatformModule : Module
    {
        [SerializeField] private PlayerInputSo playerInput;
        [SerializeField] private LayerMask whatIsPlatform;
        [SerializeField] private float passTime = 0.1f;
        
        private WaitForSeconds _wait;
        private Collider2D _collider;
        private Coroutine _coroutine;
        
        private IGroundChecker _groundChecker;
        private bool _isPlatformPass;
        
        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _collider = owner.GetComponent<Collider2D>();
            FDebug.Assert(_collider != null, "Owner is not have Collider2D");
            
            FDebug.Assert(owner.TryGetModule(out _groundChecker), "Owner is not found IGroundChecker");
            
            _wait = new WaitForSeconds(passTime);

            _isPlatformPass = false;
            
            playerInput.OnJumpKeyPressed += OnPlatformMoveKeyPressed;
        }
        
        private void OnDestroy()
        {
            playerInput.OnJumpKeyPressed -= OnPlatformMoveKeyPressed;
        }
        
        private void OnPlatformMoveKeyPressed()
        {
            if(_isPlatformPass || !playerInput.PlatformKey || !_groundChecker.FixedIsGround) return;
            if(_coroutine != null) StopCoroutine(_coroutine);
            _coroutine = StartCoroutine(PlatformPassTime());
        }
        
        private IEnumerator PlatformPassTime()
        {
            SetPassPlatform(true);
            yield return _wait;
            SetPassPlatform(false);
        }
        
        private void SetPassPlatform(bool isPass)
        {
            _isPlatformPass = isPass;
            if (isPass)
            {
                _collider.excludeLayers |= whatIsPlatform;
            }
            else
            {
                _collider.excludeLayers &= ~whatIsPlatform;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _wait = new WaitForSeconds(passTime);
        }
#endif
        
    }
}