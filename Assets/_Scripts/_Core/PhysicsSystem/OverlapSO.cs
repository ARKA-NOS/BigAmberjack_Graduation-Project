using _Scripts.Agent;
using UnityEngine;

namespace _Scripts._Core.PhysicsSystem
{
    [CreateAssetMenu(fileName = "OverlapSO", menuName = "Physics/OverlapSO", order = 0)]
    public class OverlapSO : ScriptableObject
    {
        [Header("Base")]
        [SerializeField] private CheckerShape checkerShape;
        [SerializeField] private Vector2 chackOffset = Vector2.zero;
        
        [Header("Square")]
        [SerializeField] private Vector2 squareSize = Vector2.one;
        
        [Header("Circle")]
        [SerializeField] private float circleRadius = 1f;
        
        public bool Overlap(Vector2 pos, LayerMask targetLayer)
        {
            Vector2 checkPosition = pos + chackOffset;
            return checkerShape switch
            {
                CheckerShape.Square =>
                    Physics2D.OverlapBox(
                        checkPosition,
                        squareSize,
                        0f,
                        targetLayer) != null,

                CheckerShape.Circle =>
                    Physics2D.OverlapCircle(
                        checkPosition,
                        circleRadius,
                        targetLayer) != null,
                
                _ => false
            };
        }

        public void DrawGizmos(Vector2 pos)
        {
            Vector2 checkPosition = pos + chackOffset;
            
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