using UnityEngine;

namespace Kuantech.Physics
{
    public class ThrowableRigidbody : MonoBehaviour
    {
        public float Mass;
        public Vector3 Velocity;
        public Vector3 Acceleration;
        public float VelocityDrag;
        
        public bool IsAwake;
        public float TimeScale = 1f;
        private float _lastAwakenTime = 0f;
        private float _secondsToSleep = -1f;

        
        private void Update()
        {
            if (!IsAwake) return;
            if (_secondsToSleep >= 0)
            {
                if (Time.time - _lastAwakenTime >= _secondsToSleep / TimeScale)
                {
                    IsAwake = false;
                    return;
                }
            }
            ApplyKinematicEquations(Time.deltaTime * TimeScale);
        }
        
        public void AddImpulse(Vector3 impulse)
        {
            Velocity = impulse;
        }
        
        private void ApplyKinematicEquations(float deltaTime)
        {
            Vector3 newVelocity = Velocity + Acceleration * deltaTime;
            Vector3 displacement = Velocity * deltaTime + Acceleration * deltaTime * deltaTime * 0.5f;
            Velocity = newVelocity;
            Velocity *= VelocityDrag;
            transform.position += displacement;
        }
        
        /// <summary>
        /// Sets the initial velocity and acceleration for following a trajectory defined by vertical/horizontal distances and direction.
        /// Returns the required seconds in order to complete the trajectory
        /// </summary>
        /// <param name="verticalDistance"></param>
        /// <param name="horizontalDistance"></param>
        /// <param name="direction">Direction on the horizontal plane</param>
        /// <param name="gravity"></param>
        public float SetTrajectory(float verticalDistance, float horizontalDistance, Vector2 direction, float gravity)
        {
            //Calculate initial vertical velocity
            float verticalSpeedSquared = 2 * Mathf.Abs(gravity) * verticalDistance;
            float verticalSpeed = Mathf.Sqrt(verticalSpeedSquared);
            
            //Calculate time pass
            float time = verticalSpeed / Mathf.Abs(gravity) * 2f;
            
            //Calculate vertical speed
            float horizontalSpeed = horizontalDistance / time;

            Velocity = new Vector3(direction.x * horizontalSpeed, verticalSpeed,
                direction.y * horizontalSpeed);
            Acceleration = new Vector3(0f, -Mathf.Abs(gravity), 0f);
            _secondsToSleep = time;
            WakeUp();
            return time;
        }
        
        /// <summary>
        /// Sets trajectory by respecting the horizontal distance and horizontal speed. This trajectory will end at y=0
        /// and vertical speed will be adjusted to achieve that
        /// </summary>
        /// <param name="horizontalDistance">Range of the trajectory in horizontal plane</param>
        /// <param name="initialHorizontalSpeed">Initial horizontal speed</param>
        /// <param name="direction">Direction in horizontal plane</param>
        /// <param name="gravity">Gravitational acceleration force</param>
        /// <param name="initialHeight">Start local y position. For cliff throwaing scenarios</param>
        /// <returns></returns>
        public float SetTrajectoryWithHorizontalSpeed(float horizontalDistance, float initialHorizontalSpeed,
            Vector2 direction, float gravity, float initialHeight = 0f)
        {
            //Calculate time pass
            float totalTime = horizontalDistance / initialHorizontalSpeed;
            
            //Calculate vertical speed
            float horizontalSpeed = horizontalDistance / totalTime;
            
            float tTotal = horizontalDistance / horizontalSpeed;
            float verticalSpeed = -initialHeight / tTotal - 0.5f * (-9.8f) * tTotal;
            Velocity = new Vector3(direction.x * horizontalSpeed, verticalSpeed,
                direction.y * horizontalSpeed);
            Acceleration = new Vector3(0f, -Mathf.Abs(gravity), 0f);
            _secondsToSleep = totalTime;
            WakeUp();
            return totalTime;
        }
        
        /// <summary>
        /// Returns the time of travel given the displacement and initial speed under uniform acceleration
        /// </summary>
        /// <param name="displacement"></param>
        /// <param name="verticalSpeed"></param>
        /// <param name="gravity"></param>
        /// <returns></returns>
        public float GetTimeToDisplace(float displacement, float initialSpeed, float acceleration = -9.8f)
        {
            float a = 0.5f * acceleration;
            float b = initialSpeed;
            float c = -1 * displacement;
            float delta = Mathf.Sqrt(b * b - 4 * a * c);
            float root_1 = (-b + delta) / (2 * a);
            float root_2 = (-b - delta) / (2 * a);
            if (root_1 >= 0) return root_1;
            if (root_2 >= 0) return root_2;
            Debug.LogError("No suitable root found for time to displace calculation");
            return 0f;
        }

        public void Sleep()
        {
            IsAwake = false;
        }
        
        public void WakeUp()
        {
            _lastAwakenTime = Time.time;
            IsAwake = true;
        }
        
        public void Stop()
        {
            Velocity = Vector3.zero;
            Acceleration = Vector3.zero;
        }
    }
}