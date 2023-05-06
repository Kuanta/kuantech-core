using System;
using Kuantech.Core.FX;
using UnityEngine;
using UnityEngine.Serialization;

namespace Kuantech.Physics.Car
{
    [Serializable]
    public struct CarData
    {
        public int CarId;
        public string CarName;
        public GameObject CarPrefab;
        public int Price;
        
        //Preview
        public int AccelerationRank;
        public int TopSpeedRank;
        public int HandlingRank;

        //CenterOfGravity
        public Vector3 CenterOfMass;
        
        //Suspension
        public float Suspension;
        public float DampingFactor;
        public float WheelsRestDistance;
        public float MaxWheelDistance;
        
        //Handling
        public float TurnSpeed;
        public float Handling;
        
        //Acceleration
        public AnimationCurve AccelerationCurve;
        public float MaxSpeed;
        public float MaxAcceleration;
        public float BrakeForce;
    }
    
    public class CarBody : MonoBehaviour
    {
        public CarData CarData;
        
        [SerializeField] private float GroudnAngularDrag = 30;
        [SerializeField] private float WheelWeight = 1f;
        public float WheelFriction = 0.1f;

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
        public Stabilizer Stabilizer;

        [FormerlySerializedAs("_wheels")] public Wheel[] Wheels = new Wheel[4];

        [FormerlySerializedAs("GasSound")] [Header("Effects")]
        public Effect EngineLoopEffect;
        public Effect TurnEngineOnEffect;
        public Effect TurnEngineOffEffect; 
        public float MinPitch = 0.5f;
        public float MaxPitch = 2.5f;

        //States
        private float _forward = 0f;
        private float _side = 0f;
        private bool _engineOn;

        // Start is called before the first frame update
        public void Initialize(Rigidbody rigidbody)
        {
            BodyRigidbody = rigidbody;
            UpdateWheelParameters();
            BodyRigidbody.centerOfMass = CarData.CenterOfMass;
            Stabilizer = GetComponent<Stabilizer>();
            Stabilizer.Rigidbody = rigidbody;
            Stabilizer.CarBody = this;
            //
            // if (BodyCollider == null) return;
            // Collider[] childColliders = transform.GetComponentsInChildren<Collider>();
            // foreach (var childCollider in childColliders)
            // {
            //     UnityEngine.Physics.IgnoreCollision(BodyCollider, childCollider);
            // }
        }

        [SerializeField] private float SideInputDecayRate = 0.9f;

        private bool _impulseApplied = false;
        public void ApplyImpulseForce(float force, Vector2 direction)
        {
            _side = direction.x;
            BodyRigidbody.velocity = transform.forward * force * CarData.MaxSpeed;
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
            foreach (var wheel in Wheels)
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
            foreach (var wheel in Wheels)
            {
                if(wheel == null) continue;
                wheel.CarBody = this;
                wheel.SuspensionFactor = CarData.Suspension;
                wheel.DampingFactor = CarData.DampingFactor;
                wheel.TireMass = WheelWeight;
                wheel.SuspensionRestDist = CarData.WheelsRestDistance;
                wheel.BaseTracktion = CarData.Handling;
                wheel.MaxWheelDistance = CarData.MaxWheelDistance;
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
            if (BodyRigidbody == null) return;
            float currentSpeed = 0f;
            if (IsGrounded())
            {
                ClampSpeed();
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
            if (!IsGrounded()) return;
            float horizontalSpeed = GetGroundSpeed();
            float maxSpeed = CarData.MaxSpeed;
            if (_boosted) maxSpeed *= _boostMaxSpeedMultiplier;

            // limit the car's on-ground speed
            if (horizontalSpeed > maxSpeed)
            {
                float maxSpeedRatio = maxSpeed / horizontalSpeed;
                BodyRigidbody.velocity = new Vector3(BodyRigidbody.velocity.x * maxSpeedRatio, BodyRigidbody.velocity.y, BodyRigidbody.velocity.z * maxSpeedRatio);
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
            foreach (Wheel wheel in Wheels)
            {
                if (wheel.IsGrounded()) return true;
            }
            return false;
        }
        void Update()
        {
            if (BodyRigidbody == null) return;
            bool grounded = IsGrounded();
            if (grounded) _lastGroundedTime = Time.time;
            UpdateWheelParameters();
            BodyRigidbody.angularDrag = grounded ? GroudnAngularDrag : AirAngularDrag;
            AlignStep();
            
            Vector3 newGroundIncline = GetGroundIncline();
            float normalizedSpeed = GetNormalizedSpeed();

            if (_boosted)
            {
                float torque = _boostAcceleration;
                BodyRigidbody.AddForce(transform.forward * torque, ForceMode.Acceleration);
                _remainingBoostTime -= Time.deltaTime;
                if (_remainingBoostTime <= 0f)
                {
                    _boosted = false;
                    Debug.LogError("Boost Ended");
                }
            }
            foreach (Wheel wheel in Wheels)
            {
                if(wheel == null) continue;
                if (wheel.Turnable && !Aligning)
                {
                    wheel.Turn(_side);
                    wheel.InclineForce(_groundIncline);
                }
                
                if (!wheel.Driver || !wheel.IsGrounded() || _boosted) continue;
                float availableTorque = GetAvailableTorque(normalizedSpeed) * _forward;
                BodyRigidbody.AddForceAtPosition(wheel.transform.forward * availableTorque, wheel.transform.position);
            }

            newGroundIncline = Vector3.ProjectOnPlane(newGroundIncline, transform.up);
            _groundIncline = Vector3.Lerp(_groundIncline, newGroundIncline, Time.deltaTime * 20f);
            SetWheelTurnIntentionDebug(_groundIncline);

            _side *= SideInputDecayRate;
            
            //Sound
            if (EngineLoopEffect != null)
            {
                float pitch = Mathf.Min(MinPitch + (MaxPitch - MinPitch) * Mathf.Max(normalizedSpeed, 0), 3);
                EngineLoopEffect.SetAudioPitch(pitch);
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

        public void SetRigidbody(Rigidbody rigidbody)
        {
            
        }

        public void Brake()
        {
            foreach (Wheel wheel in Wheels)
            {
                if(wheel == null) continue;
                wheel.Brake(CarData.BrakeForce);
            }
        }

        private float GetNormalizedSpeed()
        {
            float carSpeed = Vector3.Dot(transform.forward, BodyRigidbody.velocity);
            float normalizedSpeed = carSpeed / CarData.MaxSpeed;
            return normalizedSpeed;
        }
        private float GetAvailableTorque(float normalizedSpeed)
        {
            //todo: Implement a power curve here
            return normalizedSpeed >= 1 ? 0f : CarData.AccelerationCurve.Evaluate(normalizedSpeed) * CarData.MaxAcceleration;
        }

        public void Reset()
        {
            Stop();
            _engineOn = false;
            _boosted = false;
            Aligning = false;
            foreach (var wheel in Wheels)
            {
                wheel.Reset();
            }
        }
        
        #region EngineOnOff

        public void TurnEngineOn()
        {
            if(TurnEngineOnEffect != null) TurnEngineOnEffect.Play();
            if(EngineLoopEffect != null) EngineLoopEffect.Play();
            _engineOn = true;
        }

        public void TurnEngineOff()
        {
            if(EngineLoopEffect != null) EngineLoopEffect.Stop();
            if(TurnEngineOnEffect != null) TurnEngineOffEffect.Play();
            _engineOn = false;
        }
        #endregion

        [Header("Boost")] 
        private float _boostAcceleration = 1.5f;
        private float _boostMaxSpeedMultiplier = 1.5f;

        private bool _boosted = false;
        private float _remainingBoostTime = 0;
        #region Boost

        public void Boost(float duration, float boostAcceleration, float boostMaxSpeedMultiplier)
        {
            _remainingBoostTime = Mathf.Max(duration, _remainingBoostTime);
            _boostAcceleration = boostAcceleration;
            _boostMaxSpeedMultiplier = boostMaxSpeedMultiplier;
            _boosted = true;
        }
        #endregion
    }

}
