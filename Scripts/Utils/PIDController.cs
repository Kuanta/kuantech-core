using System;

namespace Kuantech.Utils
{
    [Serializable]
    public class PIDController
    {
        public float P = 1;
        public float I = 0;
        public float D = 0;
        private float _lastError = 0f;
        private float _totalError = 0f;

        public PIDController()
        {
        }
        public PIDController(float p,float i, float d, float timeStep)
        {
            P = p;
            I = i;
            D = d;
        }

        public float Step(float error, float stepTime)
        {
            float errorDer = (error - _lastError) / stepTime;
            float output = P * error + I * _totalError + D * errorDer;
            _lastError = error;
            _totalError += error * stepTime;
            return output;
        }
    }
}