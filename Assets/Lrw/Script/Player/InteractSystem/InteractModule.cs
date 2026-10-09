using Agents.Players;
using DevLib.ModuleSystem;
using UnityEngine;

namespace Lrw.Script.Player.InteractSystem
{
    public class InteractModule : AbstractPlayerModule
    {
        [SerializeField] private LayerMask interactLayer;
        [SerializeField] private float interactDistance = 5;
        
        [SerializeField] private int resultSize = 10;
        
        private ContactFilter2D contactFilter;
        
        private ICanInteract _interact;
        
        private Collider2D[] result;
        
        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            result = new Collider2D[resultSize];

            contactFilter.useTriggers = true;
            contactFilter.useLayerMask = true;
            contactFilter.layerMask = interactLayer;
            
            PlayerInput.OnInteractKeyPressed += InteractKeyPressed;
        }

        private void OnDestroy()
        {
            PlayerInput.OnInteractKeyPressed -= InteractKeyPressed;
        }

        private void InteractKeyPressed()
        {
            _interact?.Interact(_owner);
        }
        
        private void FixedUpdate()
        {
            _interact = null;

            int count = Physics2D.OverlapCircle(transform.position, interactDistance, contactFilter, result);

            float minDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                Collider2D currentCollider = result[i];
                float currentDistance = Vector2.Distance(transform.position, currentCollider.transform.position);

                if (minDistance > currentDistance && currentCollider.TryGetComponent(out ICanInteract interact))
                {
                    minDistance = currentDistance;
                    _interact =  interact;
                }
            }

            SetInteractUI();
        }

        private void SetInteractUI()
        {
            if (_interact != null)
            {
                //UI 구현 필요
            }
            else
            {
                
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, interactDistance);

            if (_interact != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(_interact.Position, 0.3f);
            }
        }
    }
}