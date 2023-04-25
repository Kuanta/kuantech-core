using Kuantech.Core.FX;
using UnityEngine;
using UnityEngine.Serialization;

namespace Kuantech.Physics.Car
{
    public class CarBody : MonoBehaviour
    {
        [Header("Properties")] 
        [SerializeField] private Vector3 CenterOfMass = Vector3.zero;
        [SerializeField] private float GravityScale = 1;
        
        [Header("Suspension")]
        [SerializeField] private float Suspension = 1f;
        [SerializeField] private float DampingFactor = 1f;
        [SerializeField] private float WheelsRestDistance = 0.5f;
        [SerializeField] private float MaxWheelDistance = 2f;

        [FormerlySerializedAs("GroundAirDrag")] [Header("Handling")] [SerializeField] private float GroudnAngularDrag = 30;
        public float TurnSpeed = 10;
        [SerializeField] private float Handling = 10;
        [SerializeField] private float WheelWeight = 1f;
        public float WheelFriction = 0.1f;

        [Header("Acceleration")] 
        [SerializeField] private AnimationCurve AccelerationCurve;
        [SerializeField] private float MaxSpeed = 10;
        [SerializeField] private float Acceleration = 1;
        [SerializeField] private float BreakForce = 10;
        [SerializeField] private float DragReleaseAcceleration = 50f; //This is for the drag and release

        [FormerlySerializedAs("AirAngleDrag")]
        [Header("In Air Control")] 
        [SerializeField] private float AirAngularDrag = 1;
        [SerializeField] private float AirControlFactor = 1;
        
        [Header("Incline")]
        public Transform InclineRayShootPosition;
        public LayerMask InclineRayMask;
        public float InclineLerpFactor = 1f;

        [FormerlySerializedAs("antiFlipForceMultiplier")]
        [Header("Anti-Flip")] 
        public float AntiFlipForceMultiplier = 1;
        
        [Header("Components")]
        [SerializeField] private Rigidbody BodyRigidbody;
        [SerializeField] private Collider BodyCollider;

        [SerializeField] private Wheel[] _wheels = new Wheel[4];

        [Header("Audio")]
        public Effect GasSound;
        public Effect TurnEngineOnEffect;
        public Effect TurnEngineOffEffect; 
        public float MinPitch = 0.5f;
        public float MaxPitch = 2.5f;

        //States
        private float _forward = 0f;
        private float _side = 0f;
        private bool _engineOn;

        // Start is called before the first frame update
        void Start()
        {
            BodyRigidbody = GetComponent<Rigidbody>();
            UpdateWheelParameters();
            BodyRigidbody.centerOfMass = CenterOfMass;

            if (BodyCollider == null) return;
            Collider[] childColliders = transform.GetComponentsInChildren<Collider>();
            foreach (var childCollider in childColliders)
            {
                UnityEngine.Physics.IgnoreCollision(BodyCollider, childCollider);
            }
        }

        [SerializeField] private float SideInputDecayRate = 0.9f;

        private bool _impulseApplied = false;
        public void ApplyImpulseForce(float force, Vector2 direction)
        {
            _side = direction.x;
            BodyRigidbody.velocity = transform.forward * force * MaxSpeed;
            Vector3 globalDireciton = transform.TransformDirection(new Vector3(direction.x, 0, direction.y));
            AlignToDirection(globalDireciton);
        }
        
        #region Aligning
        public bool Aligning { get; private set;}
        private Vector3 _targetDirection ;
        private void AlignToDirection(Vector3 targetDirection)
        {
            if(Aligning) return;
            _targetDirection = targetDirection;
            Aligning = true;
            foreach (var wheel in _wheels)
            {
                if (wheel.Turnable)
                {
                    wheel.LookTowardsAngle(targetDirection);
                }
            }
        }
        private void AlignStep()
        {
            if (!Aligning) return;
            
            // Check if the car body's forward vector is aligned with the wheel's forward vector
            if (Vector3.Dot(_targetDirection, transform.forward) > 0.9f) {
                // Stop aligning
                Aligning = false;
            }
        }
        #endregion
 
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

        public void ToggleRigidbody(bool toggle)
        {
            BodyRigidbody.constraints = toggle ? RigidbodyConstraints.None : RigidbodyConstraints.FreezeAll;
        }
        public void SetMovementParameters(float forward, float side)
        {
            _forward = forward;
            if(side != 0) _side = side;
        }
        // Update is called once per frame
        private Vector3 _groundIncline = Vector3.zero;
        private float _lastGroundedTime;
        private void FixedUpdate()
        {
            float currentSpeed = 0f;
            if (IsGrounded())
            {
                Vector3 horizontalVelocity = new Vector3(BodyRigidbody.velocity.x, 0, BodyRigidbody.velocity.z); // ignore the y component of the velocity
                float horizontalSpeed = horizontalVelocity.magnitude;
                // limit the car's on-ground speed
                if (horizontalSpeed > MaxSpeed)
                {
                    float maxSpeedRatio = MaxSpeed / horizontalSpeed;
                    BodyRigidbody.velocity = new Vector3(BodyRigidbody.velocity.x * maxSpeedRatio, BodyRigidbody.velocity.y, BodyRigidbody.velocity.z * maxSpeedRatio);
                }
            }
            else if(Time.time - _lastGroundedTime > 1f)
            {
                //In air controls
                // Calculate torque based on input
                Vector3 torque = new Vector3(_forward, _side, 0) * AirControlFactor * BodyRigidbody.mass;

                // Add torque to the rigidbody
                BodyRigidbody.AddRelativeTorque(torque);
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
                    BodyRigidbody.velocity = new Vector3(BodyRigidbody.velocity.x * maxSpeedRatio, BodyRigidbody.velocity.y, BodyRigidbody.velocity.z * maxSpeedRatio);
                }
            }
        }

        public float GetGroundSpeed()
        {
            Vector3 velocity = BodyRigidbody.velocity;
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
            bool grounded = IsGrounded();
            if (grounded) _lastGroundedTime = Time.time;
            UpdateWheelParameters();
            BodyRigidbody.angularDrag = grounded ? GroudnAngularDrag : AirAngularDrag;
            AlignStep();
            
            Vector3 newGroundIncline = GetGroundIncline();
            float normalizedSpeed = GetNormalizedSpeed();
            foreach (Wheel wheel in _wheels)
            {
                if(wheel == null) continue;
                if (wheel.Turnable && !Aligning)
                {
                    wheel.Turn(_side);
                    wheel.InclineForce(_groundIncline);
                }

                if (!wheel.Driver || !wheel.IsGrounded()) continue;
                float availableTorque = GetAvailableTorque(normalizedSpeed) * _forward;
                BodyRigidbody.AddForceAtPosition(wheel.transform.forward * availableTorque, wheel.transform.position);
            }

            newGroundIncline = Vector3.ProjectOnPlane(newGroundIncline, transform.up);
            _groundIncline = Vector3.Lerp(_groundIncline, newGroundIncline, Time.deltaTime * 20f);
            SetWheelTurnIntentionDebug(_groundIncline);

            _side *= SideInputDecayRate;
            
            //Sound
            if (GasSound != null)
            {
                GasSound.SetAudioPitch(Mathf.Lerp(MinPitch, MaxPitch,normalizedSpeed));
            }
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
            if (BodyRigidbody.velocity.sqrMagnitude <= StillVelocityThreshold * StillVelocityThreshold) return true;
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
            if(BodyRigidbody == null) return;
            BodyRigidbody.velocity = Vector3.zero;
            BodyRigidbody.angularVelocity = Vector3.zero;
            _impulseApplied = false;
        }
        public Rigidbody GetRigidbody()
        {
            return BodyRigidbody;
        }


        public void Brake()
        {
            foreach (Wheel wheel in _wheels)
            {
                if(wheel == null) continue;
                wheel.Break(BreakForce);
            }
        }

        private float GetNormalizedSpeed()
        {
            float carSpeed = Vector3.Dot(transform.forward, BodyRigidbody.velocity);
            float normalizedSpeed = Mathf.Clamp01(carSpeed / MaxSpeed);
            return normalizedSpeed;
        }
        private float GetAvailableTorque(float normalizedSpeed)
        {
            
            //todo: Implement a power curve here
            return normalizedSpeed >= 1 ? 0f : AccelerationCurve.Evaluate(normalizedSpeed) * Acceleration;
        }

        public void Reset()
        {
            Stop();
            _engineOn = false;
            Aligning = false;
            foreach (var wheel in _wheels)
            {
                wheel.Reset();
            }
        }
        
        #region EngineOnOff

        public void TurnEngineOn()
        {
            TurnEngineOnEffect.Play();
            GasSound.Play();
            _engineOn = true;
        }

        public void TurnEngineOff()
        {
            GasSound.Stop();
            TurnEngineOffEffect.Play();
            _engineOn = false;
        }
        #endregion
    }

}
