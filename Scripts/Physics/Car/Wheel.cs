using UnityEngine;

namespace Kuantech.Physics.Car
{
    public class Wheel : MonoBehaviour
    {
        public bool DebugEnabled = false;
        public CarBody CarBody;
        public GameObject WheelVisual;

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
            AlignStep();
            if (!_aligning)
            {
                float _currentAngle = transform.localEulerAngles.y;
                _currentAngle = Mathf.Lerp(_currentAngle, _targetAngle, 1);
                _currentAngle = TurnTowardsIncline(_currentAngle);
                _currentAngle = Mathf.Clamp(_currentAngle, -AngleRange, AngleRange);
         
                transform.localEulerAngles = new Vector3(0f, _currentAngle, 0f);
            }

            if (WheelVisual == null) return;
            float offset = _hitDistance;
            if (!_isGrounded) offset = MaxWheelDistance;
            WheelVisual.transform.position = transform.position - transform.up * offset;
        }

        private float TurnTowardsIncline(float currentAngle)
        {
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
            CarBody.GetRigidbody().AddForceAtPosition(steeringDir * TireMass * desiredAccel, wheelPosition);
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
        #endregion
 
        [HideInInspector] public float BaseTracktion = 0f;
        //Curves
        private float GetTracktion()
        {
            return BaseTracktion;
        }

        private Vector3 _targetDirection;
        private bool _aligning = false;
        public void AlignToDirection(Vector3 targetDirection)
        {
            if(_aligning) return;
            _targetDirection = targetDirection;
            _aligning = true;
        }

        private void AlignStep()
        {
            if (!_aligning) return;
            float angle = Vector3.SignedAngle(transform.forward, _targetDirection, transform.up);
            
            // Calculate the rotation to apply this frame
            Quaternion deltaRotation = Quaternion.AngleAxis(angle, transform.up);
            
            // Apply the rotation to the wheel's current rotation
            transform.rotation = deltaRotation * transform.rotation;
            
            // Check if the car body's forward vector is aligned with the wheel's forward vector
            if (Vector3.Dot(transform.forward, CarBody.transform.forward) > 0.9f) {
                // Stop aligning
                _aligning = false;
            }
        }
        public void Reset()
        {
            _aligning = false;
            _currentIncline = Vector3.zero;
        }
    }
    
   

}
