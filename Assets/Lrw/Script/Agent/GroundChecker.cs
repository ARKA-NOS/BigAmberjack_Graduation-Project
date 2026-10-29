using System;
using DevLib.ModuleSystem;
using Lrw.Script._Core._Debug;
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
        [SerializeField] private CheckerShape checkerShape;
        [SerializeField] private bool gizmosEnabled = true;
        [SerializeField] private Vector2 chackOffset = Vector2.zero;
        [SerializeField] private LayerMask whatIsGround;

        [Header("Square")]
        [SerializeField] private Vector2 squareSize = Vector2.one;

        [Header("Circle")]
        [SerializeField] private float circleRadius = 1f;

        private Rigidbody2D _rigid;
        
        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _rigid = _owner.GetComponent<Rigidbody2D>();
            FDebug.Assert(_rigid != null,"[GroundChecker] Rigidbody2D is not found");
        }

        public bool FixedIsGround { get; private set; }

        private void FixedUpdate()
        {
            FixedIsGround = IsGroundChecking();
        }

        public bool IsGroundChecking()
        {
            if (!Mathf.Approximately(_rigid.linearVelocityY, 0f)) return false;
            Vector2 checkPosition = (Vector2)transform.position + chackOffset;
            
            return checkerShape switch
            {
                CheckerShape.Square =>
                    Physics2D.OverlapBox(
                        checkPosition,
                        squareSize,
                        0f,
                        whatIsGround) != null,

                CheckerShape.Circle =>
                    Physics2D.OverlapCircle(
                        checkPosition,
                        circleRadius,
                        whatIsGround) != null,

                _ => false
            };
        }

        private void OnDrawGizmos()
        {
            if (!gizmosEnabled)
                return;

            Vector2 checkPosition = (Vector2)transform.position + chackOffset;

            Gizmos.color = Color.yellow;
            switch (checkerShape)
            {
                case CheckerShape.Square:
                    Gizmos.DrawWireCube(checkPosition, squareSize);
                    break;

                case CheckerShape.Circle:
                    Gizmos.DrawWireSphere(checkPosition, circleRadius);
                    break;
            }
        }
    }
}