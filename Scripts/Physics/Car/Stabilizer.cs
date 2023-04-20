using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Physics.Car
{
    public class Stabilizer : MonoBehaviour
    {
        [SerializeField] private CarBody CarBody;
        [SerializeField] private Rigidbody Rigidbody;
        public PIDController YawPIDController = new PIDController();
        public PIDController RollPIDController = new PIDController();
      

        private void FixedUpdate()
        {
           Stabilize();
        }

        private void Stabilize()
        {
            if (CarBody.IsGrounded()) return;
            Vector3 currentForward = transform.forward.normalized;
            Vector3 currentRight = transform.right.normalized;
            Vector3 refForward = currentForward;
            refForward.y = 0;
            refForward.Normalize();

            float yawError = 0 - Vector3.SignedAngle(refForward, currentForward, currentRight);

            Vector3 refRight = currentRight;
            refRight.y = 0;
            refRight.Normalize();

            float rollError = 0 - Vector3.SignedAngle(refRight, currentRight, currentForward);

            float rollControlSignal = RollPIDController.Step(rollError, Time.fixedDeltaTime);
            Rigidbody.AddTorque(currentRight * YawPIDController.Step(yawError, Time.fixedDeltaTime));
            Rigidbody.AddTorque(currentForward * rollControlSignal);

        }
    }
}