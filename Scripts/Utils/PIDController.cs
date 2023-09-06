using System;
using UnityEngine;

namespace Kuantech.Utils
{
    [Serializable]
    public class PIDController
    {
        public float P = 1;
        public float I = 0;
        public float D = 0;
        
        // Consider adding limits for integral windup prevention
        public float IntegralMax = float.MaxValue;
        public float IntegralMin = float.MinValue;

        private float _lastError = 0f;
        private float _totalError = 0f;

        public PIDController() { }

        public PIDController(float p, float i, float d)
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
            
            // Update total error and clamp it to prevent integral windup
            _totalError += error * stepTime;
            _totalError = Mathf.Clamp(_totalError, IntegralMin, IntegralMax);
            
            return output;
        }

        public void Reset()
        {
            _lastError = 0f;
            _totalError = 0f;
        }
    }
}