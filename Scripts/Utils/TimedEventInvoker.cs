using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class TimedEventInvoker
{
    public float FireRate = 1;
    public UnityAction EventToFire = null;
    private float _lastFiredTime;

    public void Update()
    {
        if (Time.time - _lastFiredTime > FireRate)
        {
            EventToFire?.Invoke();
            _lastFiredTime = Time.time;
        }
    }
}