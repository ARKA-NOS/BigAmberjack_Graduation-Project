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

        public bool pass;
        
        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _collider = owner.GetComponent<Collider2D>();
            FDebug.Assert(_collider != null, "Owner is not have Collider2D");
            
            _wait = new WaitForSeconds(passTime);
            
            playerInput.OnPlatformKeyPressed += OnPlatformKeyPressed;
        }

        private void OnDestroy()
        {
            playerInput.OnPlatformKeyPressed -= OnPlatformKeyPressed;
        }

        private void OnPlatformKeyPressed()
        {
            if(_coroutine != null)StopCoroutine(_coroutine);
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
            pass = isPass;
            if (isPass)
            {
                _collider.excludeLayers |= whatIsPlatform;
            }
            else
            {
                _collider.excludeLayers &= ~whatIsPlatform;
            }
        }
        
    }
}