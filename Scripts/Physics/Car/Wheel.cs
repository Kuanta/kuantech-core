using System;
using UnityEngine;

namespace Kuantech.Physics.Car
{
    public class Wheel : MonoBehaviour
    {
        public bool DebugEnabled = false;
        public CarBody CarBody;
        public GameObject WheelVisual;
        public float WheelVisualRadius;

        [Header("Properties")] 
        public bool Driver = false;
        public bool Turnable = false;
        public float SuspensionFactor = 1f;
        public float DampingFactor = 1f;
        public float AngleRange = 45;
        public float SuspensionRestDist = 1f;
        public float MaxWheelDistance = 1f;
        public float TireMass = 0.5f;
        
        [Header("Ray")]
        [SerializeField] private LayerMask _layerMask;
        private RaycastHit[] _hits = new RaycastHit[4];
        private bool _isGrounded = false;
        private float _hitDistance;
        private Vector3 _hitPoint;
        private Vector3 _groundNormal;
        private float _targetAngle = 0f;
        public Vector3 WheelTurnIntention;


        private Vector3 _currentIncline = Vector3.zero;
        public bool IsGrounded()
        {
            return _isGrounded;
        }

        private void Update()
        {
            CheckGround();
            if (!CarBody.Aligning)
            {

                float _currentAngle = transform.localEulerAngles.y;

                if (_currentAngle > 180f) _currentAngle -= 360f;
                float angleDiff = _targetAngle - _currentAngle;
 
                // Calculate the angle to turn this frame
                _currentAngle += Math.Sign(angleDiff) * CarBody.TurnSpeed * Time.deltaTime;
                
                //_currentAngle = TurnTowardsIncline(_currentAngle);
                _currentAngle = Mathf.Clamp(_currentAngle, -AngleRange, AngleRange);

                transform.localEulerAngles = new Vector3(0f, _currentAngle, 0f);
            }

            if (IsGrounded())
            {
                Vector3 carVelocity = CarBody.GetRigidbody().velocity;
                float speed = carVelocity.magnitude;
                float angularSpeed = (speed / (2 * Mathf.PI * WheelVisualRadius))*Mathf.Rad2Deg;
                float rightDot = Vector3.Dot(CarBody.transform.right, transform.right);
                float forwardDot = Vector3.Dot(CarBody.transform.forward, carVelocity);
                float angle = angularSpeed * Time.fixedDeltaTime * rightDot * forwardDot;
                WheelVisual.transform.Rotate(angle, 0f, 0f, Space.Self);
            }
            if (WheelVisual == null) return;
            float offset = _hitDistance;
            if (!_isGrounded) offset = MaxWheelDistance;
            WheelVisual.transform.position = transform.position + transform.up * Mathf.Min(-offset + WheelVisualRadius, transform.localPosition.y);
        }

        private float TurnTowardsIncline(float currentAngle)
        {
            if (_currentIncline.sqrMagnitude <= 0.01f) return 0;

            Vector3 relativeIncline = transform.InverseTransformDirection(_currentIncline);
            Quaternion targetRotation = Quaternion.LookRotation(relativeIncline, Vector3.up);
            float rotationSpeedScaled = CarBody.InclineLerpFactor * _currentIncline.magnitude;
            targetRotation = Quaternion.Slerp(Quaternion.Euler(new Vector3(0,currentAngle,0)), 
                targetRotation, Time.deltaTime * rotationSpeedScaled);
            float eulerY = targetRotation.eulerAngles.y;
            if (eulerY > 180.0f)
            {
                eulerY -= 360.0f;
            }
            return Mathf.Clamp(eulerY, -AngleRange, AngleRange);
        }
        private void FixedUpdate()
        {
            Vector3 suspensionForce = SuspensionForce();
            TracktionForce();
            Break(CarBody.WheelFriction);
        }

        #region Forces
        /// <summary>
        /// Force applied along the up axis of the wheel. Keeps the car afloat.
        /// </summary>
        private Vector3 SuspensionForce()
        {
            if (!_isGrounded || CarBody == null) return Vector3.zero;
            Vector3 wheelPosition = transform.position;
            Rigidbody carRigidbody = CarBody.GetRigidbody();
            Vector3 springDir = transform.up;
            Vector3 worldVel = carRigidbody.GetPointVelocity(wheelPosition);
            float offset = SuspensionRestDist - _hitDistance;
            float vel = Vector3.Dot(springDir, worldVel);
            Vector3 force = springDir * ((offset * SuspensionFactor) - (vel * DampingFactor));
            carRigidbody.AddForceAtPosition(force, wheelPosition);
            return force;
        }
        
        /// <summary>
        /// Force applied along the right axis of the wheel. Affects handling
        /// </summary>
        private void TracktionForce()
        {
            if (!_isGrounded || CarBody == null) return;
            
            //Get velocity on the position of the wheel
            Vector3 wheelPosition = transform.position;
            Vector3 carVelocity = CarBody.GetRigidbody().GetPointVelocity(wheelPosition);
            Vector3 steeringDir = transform.right;
            float steeringVel = Vector3.Dot(steeringDir, carVelocity);
            float desiredVelChange = -steeringVel * GetTracktion();
            float desiredAccel = desiredVelChange / Time.fixedDeltaTime;
            CarBody.GetRigidbody().AddForceAtPosition(steeringDir * TireMass * desiredAccel, wheelPosition);
        }

        public void Break(float breakForce = 1)
        {
            if (!_isGrounded || CarBody == null) return;
            
            //Get velocity on the position of the wheel
            Vector3 wheelPosition = transform.position;
            Vector3 carVelocity = CarBody.GetRigidbody().GetPointVelocity(wheelPosition);
            Vector3 steeringDir = transform.forward;
            float steeringVel = Vector3.Dot(steeringDir, carVelocity);
            float desiredVelChange = -steeringVel * breakForce;
            float desiredAccel = desiredVelChange / Time.fixedDeltaTime;
            CarBody.GetRigidbody().AddForceAtPosition(steeringDir * desiredAccel, wheelPosition);
        }

        public void InclineForce(Vector3 inclineVector)
        {
            _currentIncline = inclineVector;
        }
        #endregion
       
        private void CheckGround()
        {
            _isGrounded = false;
            Vector3 sphereCenter = transform.position;
            Vector3 castDirection = -transform.up;

            int numHits = UnityEngine.Physics.BoxCastNonAlloc(sphereCenter, new Vector3(0.1f,0.1f,0.1f), castDirection, _hits, Quaternion.identity, MaxWheelDistance, _layerMask);
            if (numHits <= 0) return;
            bool found = false;
            int minIndex = 0;
            float minDistance = float.MaxValue;
            for (int i = 0; i < numHits; ++i)
            {
                if(_hits[i].collider == null) continue;
                if(_hits[i].collider.gameObject == CarBody.gameObject) continue;
                found = true;
                if (!(_hits[i].distance < minDistance)) continue;
                minIndex = i;
                minDistance = _hits[i].distance;
            }

            if (!found) return;
            _isGrounded = true;

            _hitDistance = _hits[minIndex].distance;
            _hitPoint = _hits[minIndex].point;
            _groundNormal = _hits[minIndex].normal;
            Vector3 direction = Vector3.Cross(Vector3.up, _groundNormal).normalized;
            WheelTurnIntention = Vector3.Cross(direction, _groundNormal).normalized;
        }


        #region Inputs
        public void ApplyGas(float torque)
        {
            if (!_isGrounded) return;
            
        }

        public void Turn(float turnRate)
        {
            turnRate = Mathf.Clamp(turnRate, -1f, 1f);
            _targetAngle = turnRate * AngleRange;
        }

        public void LookTowardsAngle(Vector3 targetDirection)
        {
            // Calculate the current forward direction in world space
            Vector3 currentForward = transform.forward;

            // Calculate the desired forward direction in world space
            Vector3 desiredForward = Vector3.RotateTowards(currentForward, targetDirection, AngleRange * Mathf.Deg2Rad, 0f);

            // Set the new forward direction
            transform.forward = desiredForward;
        }
        #endregion
 
        [HideInInspector] public float BaseTracktion = 0f;
        //Curves
        private float GetTracktion()
        {
            return BaseTracktion;
        }

   
        public void Reset()
        {
            _currentIncline = Vector3.zero;
        }
    }
    
   

}
