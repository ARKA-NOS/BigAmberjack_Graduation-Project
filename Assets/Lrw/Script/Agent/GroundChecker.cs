using System;
using DevLib.ModuleSystem;
using Lrw.Script._Core._Debug;
using Lrw.Script._Core.PhysicsSystem;
using UnityEngine;

namespace Lrw.Script.Agent
{
    public enum CheckerShape
    {
        Square,
        Circle
    }

    public class GroundChecker : Module, IGroundChecker
    {
        [Header("Base")]
        [SerializeField] private OverlapSO checkSO;
        [SerializeField] private LayerMask whatIsGround;
        [SerializeField] private bool gizmosEnabled = true;
        
        private Rigidbody2D _rigid;
        
        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _rigid = _owner.GetComponent<Rigidbody2D>();
            FDebug.Assert(_rigid != null,"[GroundChecker] Rigidbody2D is not found");
            
            FixedIsGround = false;
        }
        
        public bool FixedIsGround { get; private set; }

        private void FixedUpdate()
        {
            FixedIsGround = IsGroundChecking();
        }

        private const float FallSpeed = 0.1f;

        public bool IsGroundChecking()
        {
            if (Mathf.Abs(_rigid.linearVelocityY) >= FallSpeed) return false;
            return checkSO.Overlap(transform.position,whatIsGround);
        }
        
        private void OnDrawGizmos()
        {
            if (!gizmosEnabled || checkSO == null)
                return;

            checkSO.DrawGizmos(transform.position);
        }
        
    }
}