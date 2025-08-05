using UnityEngine;
using UnityEngine.Events;


public class TrajectoryFollower : MonoBehaviour
{
   
    public Vector3 OffsetVector = Vector3.up;
    public Vector3 ForwardVector = Vector3.forward;
    public Vector3 UpVector = Vector3.up;
    
    [Header("Curves")]
    public AnimationCurve SpeedCurve;     // Normalized progress -> speed multiplier
    public AnimationCurve HeightCurve;    // Normalized progress -> height offset

    public float FlightTime = 1;
    [Header("Settings")] 
    public float ReachThreshold = 0.1f;
    public float Speed = 1f;
    public float RiseSpeed = 1f;

    [Header("Smoothing")]
    public float PositionSmoothSpeed = 10f;
    public float RotationSmoothSpeed = 10f;

    [Header("Rotation Settings")]
    public bool RotateTowardsTarget = true;

    [Header("Scale")] 
    public float RiseScale = 1.2f;
    public float ScaleLerpFactor = 50;

    private Vector3 _startPosition;
    private Vector3 _endPosition;
    private Vector3 _targetPosition;
    private Vector3 _direction;
    private float _elapsedTime = 0f;
    private float _totalDistance;
    private bool _isMoving = false;

    private Vector3 _targetScale = Vector3.one;

    public UnityAction OnReachedTarget;

    public void GoToPoint(Vector3 target)
    {
        _startPosition = transform.position;
        _endPosition = target;
        _direction = (_endPosition - _startPosition).normalized;
        _targetPosition = _startPosition;
        _elapsedTime = 0f;
        _isMoving = true;
        _totalDistance = Vector3.Distance(_startPosition, _endPosition);
        transform.localScale = Vector3.one;
        _targetScale = Vector3.one;
    }

    public bool IsMoving()
    {
        return _isMoving;
    }
    
    private void Update()
    {
        if (!_isMoving)
        {
            return;
        }
        transform.localScale = Vector3.Lerp(transform.localScale, _targetScale,Time.deltaTime * ScaleLerpFactor);

        _elapsedTime += Time.deltaTime;

        float progress = Mathf.Clamp01(_elapsedTime / FlightTime);

        // Evaluate speed and height from curves
        float speed = SpeedCurve.Evaluate(progress) * Speed;

        float heightCurveValue = HeightCurve.Evaluate(progress);
        float heightSpeed = heightCurveValue * RiseSpeed;
        _targetScale = Vector3.one * (1+(heightCurveValue * RiseScale));
        if (progress >= 1.0f)
        {
            heightSpeed = 0.0f;
        }
        
        Vector3 towardsEndPoint = (_endPosition - transform.position).normalized;
        
        Vector3 finalVelocity = towardsEndPoint * speed + heightSpeed * OffsetVector;
            

        // Smooth position update
        transform.position += finalVelocity * Time.deltaTime;

  
        if (RotateTowardsTarget && finalVelocity.sqrMagnitude > 0.0001f)
        {
            float angle = Vector3.SignedAngle(ForwardVector, finalVelocity, UpVector);
            Quaternion targetRot = Quaternion.AngleAxis(angle, UpVector);
            transform.rotation =Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * RotationSmoothSpeed);
        }
        
        //
        // Check if close enough to final destination
        float remainingDistance = Vector3.Distance(transform.position, _endPosition);
        if (remainingDistance <= ReachThreshold)
        {
            _isMoving = false;
            transform.position = _endPosition;
            OnReachedTarget?.Invoke();
            _targetScale = Vector3.one;
        }
    }

    public void Stop()
    {
        _isMoving = false;
        _targetScale = Vector3.one;
    }
}