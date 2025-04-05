using UnityEngine;

namespace Kuantech.Puzzle.BlockingDrag
{
    public class BlockingDraggable : GridTileDraggable
    {
        [Header("Physics")] 
        [SerializeField] private Rigidbody Rigidbody;
        [SerializeField] private Collider Collider;
        [SerializeField] private float MaxSpeed = 10f;

        protected override void SetPosition(Vector3 position)
        {
            Vector3 direction = position - Rigidbody.position;
            Debug.LogError($"Target pos:{position} - Direction:{direction}");
            float mag = direction.magnitude;
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