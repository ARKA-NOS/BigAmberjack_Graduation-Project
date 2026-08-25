using DevLib.ModuleSystem;
using UnityEngine;

namespace Agents
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
        [SerializeField] private Vector2 chackerOffset = Vector2.zero;
        [SerializeField] private LayerMask whatIsGround;

        [Header("Square")]
        [SerializeField] private Vector2 squareSize = Vector2.one;

        [Header("Circle")]
        [SerializeField] private float circleRadius = 1f;

        public bool IsGroundChecking()
        {
            Vector2 checkPosition = (Vector2)transform.position + chackerOffset;

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

            Vector2 checkPosition = (Vector2)transform.position + chackerOffset;

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