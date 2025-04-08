using UnityEngine;

namespace Kuantech.Puzzle.BlockingDrag
{
    public class BlockingDraggable : GridTileDraggable
    {
        [Header("Physics")] 
        [SerializeField] private Rigidbody Rigidbody;
        [SerializeField] private Collider Collider;
        [SerializeField] private float SpeedGain = 10f;
        [SerializeField] private float MaxSpeed = 10f;

        public bool DisableRigidbodyMovements = false;
        protected override void SetPosition(Vector3 position)
        {
            if (DisableRigidbodyMovements)
            {
                base.SetPosition(position);
                return;
            }
            
            Vector3 direction = position - Rigidbody.position;
            float mag = direction.magnitude * SpeedGain;
            if (mag > MaxSpeed)
            {
                mag = MaxSpeed;
            }
            direction.Normalize();
            Rigidbody.velocity = direction * mag;
        }

        public override bool DragStart()
        {
            if (!base.DragStart()) return false;
            Rigidbody.isKinematic = false;
            Rigidbody.velocity = Vector3.zero;
            return true;
        }
        
        public override void DragEnd()
        {
            base.DragEnd();
            Rigidbody.isKinematic = true;
            Rigidbody.velocity = Vector3.zero;
        }
    }
}