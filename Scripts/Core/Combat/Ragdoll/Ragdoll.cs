using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Kuantech.Core
{
    public class Ragdoll : MonoBehaviour
    {
        [Header("Refs")]
        public Animator Animator;
        public Rigidbody PelvisRigidbody;     // hips/pelvis RB
        public Transform ParentActor;         // logic root (Actor)
        public List<Rigidbody> Rigidbodies = new();
        public List<Collider>  RagdollColliders = new();
        
        [Header("Animation Settings")]
        [SerializeField] private string standingUpStateName = "GetUp";
        
        [Header("Snap Modes")]
        public Vector3 UpAxis = Vector3.up;

        public enum PositionSnap { Full, HorizontalOnly /*XZ*/, None }
        public enum RotationSnap { YawOnly, Full, None }

        [SerializeField] PositionSnap positionSnap = PositionSnap.HorizontalOnly;
        [SerializeField] RotationSnap rotationSnap = RotationSnap.YawOnly;

        [Header("Snap Smoothing")]
        [SerializeField] float snapBlendTime = 0.0f;

        [Header("RB Defaults")]
        public CollisionDetectionMode RagdollCollisionMode = CollisionDetectionMode.ContinuousSpeculative;
        public RigidbodyInterpolation RagdollInterpolation = RigidbodyInterpolation.Interpolate;

        bool _ragdollEnabled;
        private static readonly int GetUp = Animator.StringToHash("GetUp");

        [Button("Enable Ragdoll")]
        public void EnableRagdoll()
        {
            ToggleRagdoll();
            _ragdollEnabled = true;
        }
        
        [Button("GetUp From Ragdoll")]
        public void GetUpFromRagdoll()
        {
            TurnoffRagdollState();

            if (ParentActor && PelvisRigidbody)
            {
                Vector3 targetPos = ComputeSnappedPosition(ParentActor.position, PelvisRigidbody.position);
                Quaternion targetRot = ComputeSnappedRotation(ParentActor.rotation, PelvisRigidbody.rotation);

                if (snapBlendTime <= 0f)
                {
                    ParentActor.SetPositionAndRotation(targetPos, targetRot);
                }
                else
                {
                    StopAllCoroutines();
                    StartCoroutine(SmoothSnap(ParentActor, targetPos, targetRot, snapBlendTime));
                }
            }
            
            //Play standing up animation
            if (Animator)
            {
                Animator.Play(standingUpStateName);
            }

            _ragdollEnabled = false;
        }
        
        /// <summary>
        /// Toggles ragdoll state on.
        /// </summary>
        public void ToggleRagdoll()
        {
            if (Animator) {
            
                Animator.enabled = false;
            }
            
            foreach (var c in RagdollColliders) if (c) c.enabled = true;
            foreach (var rb in Rigidbodies) if (rb)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.detectCollisions = true;
                rb.collisionDetectionMode = RagdollCollisionMode;
                rb.interpolation = RagdollInterpolation;
            }
        }
        
        [Button("Disable Ragdoll")]
        public void TurnoffRagdollState()
        {
            // 1) Turn off physics
            foreach (var c in RagdollColliders) if (c) c.enabled = false;
            foreach (var rb in Rigidbodies) if (rb)
            {
                rb.isKinematic = true;
                rb.useGravity  = false;
                rb.detectCollisions = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
                rb.interpolation = RigidbodyInterpolation.None;
            }
        
            if (Animator)
            {
                Animator.Rebind();
                Animator.enabled = true;
            }
        }
        
        Vector3 ComputeSnappedPosition(Vector3 current, Vector3 pelvisWorld)
        {
            switch (positionSnap)
            {
                case PositionSnap.Full:
                    //Raycast bottom and find y pos
                    if (UnityEngine.Physics.Raycast(pelvisWorld, Vector3.down, out RaycastHit hitInfo))
                    {
                        pelvisWorld = new Vector3(pelvisWorld.x, hitInfo.point.y, pelvisWorld.z);
                    }
                    return pelvisWorld;
                case PositionSnap.HorizontalOnly:
                    return new Vector3(pelvisWorld.x, current.y, pelvisWorld.z);
                case PositionSnap.None:
                default:
                    return current;
            }
        }

        Quaternion ComputeSnappedRotation(Quaternion current, Quaternion pelvisWorld)
        {
            switch (rotationSnap)
            {
                case RotationSnap.Full:
                    return pelvisWorld;
                case RotationSnap.YawOnly:
                    {
                        var yaw = ProjectYaw(pelvisWorld, UpAxis);
      
                        return yaw;
                    }
                case RotationSnap.None:
                default:
                    return current;
            }
        }

        System.Collections.IEnumerator SmoothSnap(Transform t, Vector3 targetPos, Quaternion targetRot, float time)
        {
            Vector3 startPos = t.position;
            Quaternion startRot = t.rotation;
            float elapsed = 0f;

            while (elapsed < time)
            {
                elapsed += Time.deltaTime;
                float k = Mathf.Clamp01(elapsed / time);
                t.SetPositionAndRotation(
                    Vector3.Lerp(startPos, targetPos, k),
                    Quaternion.Slerp(startRot, targetRot, k));
                yield return null;
            }
            t.SetPositionAndRotation(targetPos, targetRot);
        }

        static Quaternion ProjectYaw(Quaternion worldRot, Vector3 up)
        {
            if (up == Vector3.zero) up = Vector3.up;
            Vector3 fwd = worldRot * Vector3.forward;
            Vector3 planar = Vector3.ProjectOnPlane(fwd, up);
            if (planar.sqrMagnitude < 1e-6f)
                planar = Vector3.ProjectOnPlane(worldRot * Vector3.right, up);
            return Quaternion.LookRotation(planar.normalized, up.normalized);
        }

        // ---- Helpers (Odin butonları için) ----
        [Button("Detect Colliders (from this)")]
        public void DetectCollidersFromThis()
        {
            DetectColliders(transform);
        }
        [Button("Detect Rigidbodies (from this)")]
        public void DetectRigidbodiesFromThis()
        {
            DetectRigidbodies(transform);
        }
        public void DetectColliders(Transform root)
        {
            RagdollColliders = new List<Collider>();
            RagdollColliders.AddRange(root.GetComponentsInChildren<BoxCollider>(true));
            RagdollColliders.AddRange(root.GetComponentsInChildren<SphereCollider>(true));
            RagdollColliders.AddRange(root.GetComponentsInChildren<CapsuleCollider>(true));
        }
        public void DetectRigidbodies(Transform root)
        {
            Rigidbodies = root.GetComponentsInChildren<Rigidbody>(true).ToList();
        }
    }
}
