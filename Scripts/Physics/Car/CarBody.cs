using UnityEngine;
using UnityEngine.Serialization;

namespace Kuantech.Physics.Car
{
    public class CarBody : MonoBehaviour
    {
        [Header("Properties")] [SerializeField]
        private Vector3 CenterOfMass = Vector3.zero;
        [Header("Suspension")]
        [SerializeField] private float Suspension = 1f;
        [SerializeField] private float DampingFactor = 1f;
        [SerializeField] private float WheelsRestDistance = 0.5f;
        [SerializeField] private float MaxWheelDistance = 2f;

        [Header("Handling")]
        [SerializeField] private float Handling = 10;
        [SerializeField] private float WheelWeight = 1f;
        public float WheelFriction = 0.1f;

        [Header("Acceleration")] 
        [SerializeField] private AnimationCurve AccelerationCurve;
        [SerializeField] private float MaxSpeed = 10;
        [SerializeField] private float Acceleration = 1;
        [SerializeField] private float BreakForce = 10;
        [SerializeField] private float DragReleaseAcceleration = 50f; //This is for the drag and release

        [Header("Incline")]
        public Transform InclineRayShootPosition;
        public LayerMask InclineRayMask;
        public float InclineLerpFactor = 1f;

        [FormerlySerializedAs("antiFlipForceMultiplier")]
        [Header("Anti-Flip")] 
        public float AntiFlipForceMultiplier = 1;
        
        [Header("Components")]
        private Rigidbody _rigidbody;

        [SerializeField] private Wheel[] _wheels = new Wheel[4];


        private float _forward = 0f;
        private float _side = 0f;
        
        // Start is called before the first frame update
        void Start()
        {
            _rigidbody = GetComponent<Rigidbody>();
            UpdateWheelParameters();
            _rigidbody.centerOfMass = CenterOfMass;
        }

        [SerializeField] private float SideInputDecayRate = 0.9f;

        private bool _impulseApplied = false;
        public void ApplyImpulseForce(float force, Vector2 direction)
        {
            _side = direction.x;
            _rigidbody.velocity = transform.forward * force * MaxSpeed;
            Vector3 globalDireciton = transform.TransformDirection(new Vector3(direction.x, 0, direction.y));
            foreach (var dr in _wheels)
            {
                if (dr.Turnable)
                {
                    dr.AlignToDirection(globalDireciton);
                }
            }
        }

 
        private void UpdateWheelParameters()
        {
            foreach (var wheel in _wheels)
            {
                if(wheel == null) continue;
                wheel.CarBody = this;
                wheel.SuspensionFactor = Suspension;
                wheel.DampingFactor = DampingFactor;
                wheel.TireMass = WheelWeight;
                wheel.SuspensionRestDist = WheelsRestDistance;
                wheel.BaseTracktion = Handling;
                wheel.MaxWheelDistance = MaxWheelDistance;
            } 
        }

        public void SetMovementParameters(float forward, float side)
        {
            _forward = forward;
            if(side != 0) _side = side;
        }
        // Update is called once per frame
        private Vector3 _groundIncline = Vector3.zero;

        private void FixedUpdate()
        {
            float currentSpeed = 0f;
            if (IsGrounded())
            {
                Vector3 horizontalVelocity = new Vector3(_rigidbody.velocity.x, 0, _rigidbody.velocity.z); // ignore the y component of the velocity
                float horizontalSpeed = horizontalVelocity.magnitude;
                // limit the car's on-ground speed
                if (horizontalSpeed > MaxSpeed)
                {
                    float maxSpeedRatio = MaxSpeed / horizontalSpeed;
                    _rigidbody.velocity = new Vector3(_rigidbody.velocity.x * maxSpeedRatio, _rigidbody.velocity.y, _rigidbody.velocity.z * maxSpeedRatio);
                }
            }
            ClampSpeed();
        }

        void ClampSpeed()
        {
            float horizontalSpeed = GetGroundSpeed();

            if (IsGrounded())
            {
                // limit the car's on-ground speed
                if (horizontalSpeed > MaxSpeed)
                {
                    float maxSpeedRatio = MaxSpeed / horizontalSpeed;
                    _rigidbody.velocity = new Vector3(_rigidbody.velocity.x * maxSpeedRatio, _rigidbody.velocity.y, _rigidbody.velocity.z * maxSpeedRatio);
                }
            }
        }

        public float GetGroundSpeed()
        {
            Vector3 velocity = _rigidbody.velocity;
            Vector3 horizontalVelocity = new Vector3(velocity.x, 0, velocity.z); // ignore the y component of the velocity
            return horizontalVelocity.magnitude;
        }
        public bool IsGrounded()
        {
            foreach (Wheel wheel in _wheels)
            {
                if (wheel.IsGrounded()) return true;
            }
            return false;
        }
        void Update()
        {
            UpdateWheelParameters();
          
            Vector3 newGroundIncline = GetGroundIncline();
            foreach (Wheel wheel in _wheels)
            {
                if(wheel == null) continue;
                if (wheel.Turnable)
                {
                    wheel.Turn(_side);
                    wheel.InclineForce(_groundIncline);
                    // if (wheel.IsGrounded())
                    // {
                    //     newGroundIncline += wheel.WheelTurnIntention;
                    // }
                }

                if (wheel.Driver && wheel.IsGrounded())
                {
                    float availableTorque = GetAvailableTorque() * _forward;
                    Vector3 wheelPosition = transform.position;
                    Vector3 localWheelPosition = wheel.transform.localPosition;
                    localWheelPosition.y = 0;
                    wheelPosition += Utils.Helpers.ProjectVector(localWheelPosition, transform.forward);
                    _rigidbody.AddForceAtPosition(wheel.transform.forward * availableTorque, wheel.transform.position);
                }
            }

            newGroundIncline = Vector3.ProjectOnPlane(newGroundIncline, transform.up);
            _groundIncline = Vector3.Lerp(_groundIncline, newGroundIncline, Time.deltaTime * 20f);
            SetWheelTurnIntentionDebug(_groundIncline);

            _side *= SideInputDecayRate;
        }

        private RaycastHit[] _groundHits = new RaycastHit[4];
        private Vector3 GetGroundIncline()
        {
            Vector3 originPoint = transform.position;
            if (InclineRayShootPosition != null) originPoint = InclineRayShootPosition.position;
            int numHits = UnityEngine.Physics.RaycastNonAlloc(originPoint, -transform.up, _groundHits, 2f, InclineRayMask);
            if (numHits <= 0) return Vector3.zero;
            bool found = false;
            int minIndex = 0;
            float minDistance = float.MaxValue;
            for (int i = 0; i < numHits; ++i)
            {
                if(_groundHits[i].collider == null) continue;
                if(_groundHits[i].collider.gameObject == gameObject) continue;
                found = true;
                if (!(_groundHits[i].distance < minDistance)) continue;
                minIndex = i;
                minDistance = _groundHits[i].distance;
            }

            if (!found) return Vector3.zero;

            Vector3 groundNormal = _groundHits[minIndex].normal;
            Vector3 direction = Vector3.Cross(Vector3.up, groundNormal).normalized;
            return Vector3.Cross(direction, groundNormal).normalized;

        }

        [SerializeField] private float StillVelocityThreshold = 0.1f;
        public bool IsStandingStill()
        {
            if (_rigidbody.velocity.sqrMagnitude <= StillVelocityThreshold * StillVelocityThreshold) return true;
            return false;
        }
        [SerializeField] private GameObject TurnDebugger;
        private void SetWheelTurnIntentionDebug(Vector3 turnIntention)
        {
            if (TurnDebugger == null) return;
            float mag = turnIntention.magnitude;
            TurnDebugger.transform.localScale = new Vector3(1,1,mag);
            if (Mathf.Abs(mag) <= 0.01f) return;
            TurnDebugger.transform.rotation = Quaternion.LookRotation(turnIntention);
        }
        public void Stop()
        {
            if(_rigidbody == null) return;
            _rigidbody.velocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _impulseApplied = false;
        }
        public Rigidbody GetRigidbody()
        {
            return _rigidbody;
        }


        public void Brake()
        {
            foreach (Wheel wheel in _wheels)
            {
                if(wheel == null) continue;
                wheel.Break(BreakForce);
            }
        }

        private float GetAvailableTorque()
        {
            float carSpeed = Vector3.Dot(transform.forward, _rigidbody.velocity);
            float normalizedSpeed = Mathf.Clamp01(carSpeed / MaxSpeed);
            //todo: Implement a power curve here
            return normalizedSpeed >= 1 ? 0f : AccelerationCurve.Evaluate(normalizedSpeed) * Acceleration;
        }

        public void Reset()
        {
            Stop();
            foreach (var wheel in _wheels)
            {
                wheel.Reset();
            }
        }
    }

}
