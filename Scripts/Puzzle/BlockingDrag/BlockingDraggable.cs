using Kuantech.BlockShuffle;
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

        [Header("Visual")] 
        [SerializeField] private BlockVisual BlockVisual;
        
        private Vector3 _lastPosition;
        [SerializeField] private float MaxDistance = 1;
        
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

        public override void Drag(Vector3 cursorPosition, Vector3 cursorPositionChange)
        {
            base.Drag(cursorPosition, cursorPositionChange);
            Vector3 positionChange = transform.position - _lastPosition;
            positionChange.y = 0f;
            _lastPosition = transform.position;
            float shakeFactor = positionChange.sqrMagnitude / (MaxDistance*MaxDistance);
            positionChange.Normalize();
            shakeFactor = Mathf.Clamp(shakeFactor,-1,1);
            _lastPositionChange = shakeFactor;
            positionChange *= shakeFactor;
            BlockVisual.SetTargetSwayFactor(new Vector2(positionChange.x, positionChange.z));
        }
        private float _lastPositionChange;
        public float GetLastPositionChange()
        {
            return _lastPositionChange;
        }
        
        public override bool DragStart(Vector3 hitPoint)
        {
            if (!base.DragStart(hitPoint)) return false;
            Rigidbody.isKinematic = false;
            Rigidbody.velocity = Vector3.zero;
            _lastPosition = transform.position;
            BlockVisual.ToggleLerpSway(true);
            return true;
        }
        
        public override void DragEnd()
        {
            base.DragEnd();
            Rigidbody.velocity = Vector3.zero;
            Rigidbody.isKinematic = true;
            BlockVisual.ToggleLerpSway(false);
            BlockVisual.SetSwayFactorX(0);
            BlockVisual.SetSwayFactorZ(0);
        }
        
        public Vector3 GetCurrentVelocity()
        {
            return Rigidbody.velocity;
        }
    }
}