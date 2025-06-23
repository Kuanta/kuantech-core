using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Utils.Math
{
    public class BSpline
    {
        public List<Vector3> SplinePoints;
        public Vector3 UpVector = Vector3.up;
        public bool Looping = false;
        
        
        private float[] _arcLengths = null;
        private float[] _normalizedArcLengths = null;
        public int RotationLookAhead = 5;
        public bool InvertDirection = false;
        private int _basePointCount;
        public void SetSplinePoints(List<Vector3> controlPoints, int SplineDegree, int SegmentPerSpline, bool looping=false, bool closed=false)
        {
            List<Vector3> extendedControlPoints = new List<Vector3>(controlPoints);
            _basePointCount = controlPoints.Count * SegmentPerSpline;
            Looping = looping;
            if (Looping && closed)
            {
                extendedControlPoints.Add(controlPoints[0]);
            }
    
            SplinePoints = BSpline.GenerateNURBSPath(extendedControlPoints, SplineDegree, null, extendedControlPoints.Count * SegmentPerSpline);
            ComputeArcLengths();
        }

        public float GetTotalDistance()
        {
            return _arcLengths[^1];
        }
        private void ComputeArcLengths()
        {
            int lengthOfArray = SplinePoints.Count;
            _arcLengths = new float[lengthOfArray];
            _normalizedArcLengths = new float[lengthOfArray];
            _arcLengths[0] = 0.0f;
            for (int i = 1; i < lengthOfArray; i++)
            {
                float segmentLength = Vector3.Distance(SplinePoints[i - 1], SplinePoints[i]);
                _arcLengths[i] = _arcLengths[i - 1] + segmentLength;  // Cumulative distance
            }

            float totalLength = GetArcTotalLength();
            for (int i = 0; i < _arcLengths.Length; ++i)
            {
                _normalizedArcLengths[i] = _arcLengths[i] / totalLength;
            }
        }
        
        /// <summary>
        /// Returns the closest point to spline from given point
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public WorldPoint GetClosestPointOnSpline(Vector3 point)
        {
            float t = GetTAtWorldPoint(point);
            return GetPointAtT(t);
        }
        
        #region Queries
        
        /// <summary>
        /// Clamps the given distance 
        /// </summary>
        /// <param name="distance"></param>
        /// <returns></returns>
        public float ClampDistance(float distance)
        {
            float totalDistance = GetTotalDistance();

            if (Looping)
            {
               
                distance = distance % totalDistance;
                if (distance < 0)
                    distance += totalDistance;
                return distance;
            }
            else
            {
                return Mathf.Clamp(distance, 0, totalDistance);
            }
        }
        
        public float GetDistanceAtT(float t)
        {
            float totalDistance = GetTotalDistance();
            // Eğer looping ise, t'yi mod 1 yaparak 0-1 aralığına çekiyoruz.
            if (Looping)
                t = t % 1f;
            return totalDistance * t;
        }

       

       public float GetTAtWorldPoint(Vector3 worldPosition)
        {
            if (SplinePoints == null || SplinePoints.Count < 2)
            {
                Debug.LogWarning("Spline oluşturulmadı veya geçersiz!");
                return 0f;
            }
            
            // Tüm segmentlerde (looping durumunda wrap-around dahil) arama yapıyoruz.
            int segmentCount = SplinePoints.Count;
            int candidateMinIndex = 0;
            int candidateMaxIndex = 1;
            float candidateSegmentT = 0f;
            float minDistance = float.MaxValue;

            // Döngü, her segmenti kontrol eder. Eğer looping ise son segment (son ve ilk) de kontrol edilir.
            for (int i = 0; i < segmentCount; i++)
            {
                int nextIndex = (i + 1) % segmentCount; // wrap-around için
                Vector3 p1 = SplinePoints[i];
                Vector3 p2 = SplinePoints[nextIndex];
                Vector3 closestPoint = GetClosestPointOnSegment(p1, p2, worldPosition);
                float distance = Vector3.Distance(worldPosition, closestPoint);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    candidateMinIndex = i;
                    candidateMaxIndex = nextIndex;
                    float segmentLength = Vector3.Distance(p1, p2);
                    candidateSegmentT = (segmentLength > 0f) ? Vector3.Distance(p1, closestPoint) / segmentLength : 0f;
                }
            }

            // Global t değeri, _normalizedArcLengths dizisine göre hesaplanıyor.
            float globalT = 0f;
            if (!Looping || candidateMinIndex < candidateMaxIndex)
            {
                // Normal lineer segment (non-loop veya looping ama segment aralığı içinde)
                globalT = Mathf.Lerp(_normalizedArcLengths[candidateMinIndex], _normalizedArcLengths[candidateMaxIndex], candidateSegmentT);
            }
            else
            {
                // Looping durumunda, candidateMinIndex son index (yani son nokta) ve candidateMaxIndex 0 (ilk nokta)
                // _normalizedArcLengths dizisinde son değer 1'e yakın olmalıdır (veya toplam mesafe olarak)
                // Bu durumda, arc length aralığını _normalizedArcLengths[last] ile 1 arasında düşünüyoruz.
                float tSegment = Mathf.Lerp(_normalizedArcLengths[segmentCount - 1], 1f, candidateSegmentT);
                // 1f değeri, döngüsel spline'da t=0 ile aynı anlamı taşır.
                globalT = (tSegment >= 1f) ? 0f : tSegment;
            }

            return globalT;
        }
        
        private Vector3 GetClosestPointOnSegment(Vector3 p1, Vector3 p2, Vector3 worldPosition)
        {
            Vector3 segmentVector = p2 - p1;
            Vector3 pointVector = worldPosition - p1;

            float segmentLengthSquared = segmentVector.sqrMagnitude;
            if (segmentLengthSquared == 0f)
                return p1;

            float projection = Vector3.Dot(pointVector, segmentVector) / segmentLengthSquared;
            projection = Mathf.Clamp01(projection);

            return p1 + projection * segmentVector;
        }
        
        private Vector3 ApplyLateralOffset(Vector3 position, Quaternion rotation, float lateralOffset)
        {
            if (Mathf.Abs(lateralOffset) < Mathf.Epsilon)
                return position;

            Vector3 tangent = rotation * Vector3.forward;
            Vector3 right = Vector3.Cross(UpVector, tangent).normalized;
            return position + right * lateralOffset;
        }

        /// <summary>
        /// Returns the world point at given t of the spline
        /// </summary>
        /// <param name="t"></param>
        /// <param name="lateralOffset">Lateral offset magnitude</param>
        /// <returns></returns>
        public WorldPoint GetPointAtT(float t, float lateralOffset = 0f)
        {
            int lengthOfArray = SplinePoints.Count;
            for (int i = 1; i < _normalizedArcLengths.Length; i++)
            {
                if (t <= _normalizedArcLengths[i])
                {
                    int minIndex = i - 1;
                    int maxIndex = i;
                    int rotationTargetIndex = Mathf.Min(maxIndex + RotationLookAhead, lengthOfArray - 1);
                    Vector3 direction = SplinePoints[rotationTargetIndex] - SplinePoints[minIndex];

                    float segmentT = (t - _normalizedArcLengths[minIndex]) / (_normalizedArcLengths[maxIndex] - _normalizedArcLengths[minIndex]);

                    Vector3 floorPos = SplinePoints[minIndex];
                    Vector3 ceilPos = SplinePoints[maxIndex];
                    Vector3 position = Vector3.Lerp(floorPos, ceilPos, segmentT);
                    Quaternion rotation = Quaternion.LookRotation(direction);

                    position = ApplyLateralOffset(position, rotation, lateralOffset);

                    return new WorldPoint()
                    {
                        Position = position,
                        Rotation = rotation,
                    };
                }
            }

            Vector3 finalDir = SplinePoints[^1] - SplinePoints[^2];
            Quaternion finalRot = Quaternion.LookRotation(finalDir);
            return new WorldPoint()
            {
                Position = ApplyLateralOffset(SplinePoints[^1], finalRot, lateralOffset),
                Rotation = finalRot,
            };
        }
        
        /// <summary>
        /// Returns the point at given distance
        /// </summary>
        /// <param name="distance"></param>
        /// <param name="lateralOffset">Lateral offset magnitude</param>
        /// <returns></returns>
        public WorldPoint GetPointAtDistance(float distance, float lateralOffset = 0f)
        {
            int lengthOfArray = SplinePoints.Count;

            if (_arcLengths == null || _arcLengths.Length == 0)
                ComputeArcLengths();

            if (distance >= _arcLengths[^1])
            {
                Vector3 lastDir = SplinePoints[^1] - SplinePoints[^2];
                Quaternion lastRot = Quaternion.LookRotation(InvertDirection ? -lastDir : lastDir);
                return new WorldPoint
                {
                    Position = ApplyLateralOffset(SplinePoints[^1], lastRot, lateralOffset),
                    Rotation = lastRot
                };
            }

            for (int i = 1; i < _arcLengths.Length; i++)
            {
                if (distance <= _arcLengths[i])
                {
                    int minIndex = i - 1;
                    int maxIndex = i;
                    int rotationTargetIndex = Mathf.Min(maxIndex + RotationLookAhead, lengthOfArray - 1);
                    Vector3 direction = SplinePoints[rotationTargetIndex] - SplinePoints[minIndex];

                    float segmentDistance = distance - _arcLengths[minIndex];
                    float segmentLength = _arcLengths[maxIndex] - _arcLengths[minIndex];
                    float segmentT = segmentDistance / segmentLength;

                    Vector3 floorPos = SplinePoints[minIndex];
                    Vector3 ceilPos = SplinePoints[maxIndex];
                    Vector3 position = Vector3.Lerp(floorPos, ceilPos, segmentT);
                    Quaternion rotation = Quaternion.LookRotation(InvertDirection ? -direction : direction);

                    position = ApplyLateralOffset(position, rotation, lateralOffset);

                    return new WorldPoint
                    {
                        Position = position,
                        Rotation = rotation
                    };
                }
            }

            Vector3 finalDirection = SplinePoints[^1] - SplinePoints[^2];
            Quaternion finalRotation = Quaternion.LookRotation(finalDirection);
            return new WorldPoint
            {
                Position = ApplyLateralOffset(SplinePoints[^1], finalRotation, lateralOffset),
                Rotation = finalRotation
            };
        }

        private float GetArcTotalLength()
        {
            return _arcLengths[^1];
        }
        #endregion

        public static List<Vector3> GenerateNURBSPath(List<Vector3> controlPoints, int degree, List<float> weights, int numPoints)
        {
            // Generate the knot vector internally
            List<float> knotVector = GenerateNURBSKnotVector(controlPoints.Count, degree);
    
            // Ensure weights match the number of control points
            if (weights == null || weights.Count != controlPoints.Count)
            {
                weights = GenerateRationalWeights(controlPoints.Count);
            }

            // List to hold the generated NURBS path
            List<Vector3> nurbsPath = new List<Vector3>();

            // Compute the NURBS curve
            for (int i = 0; i < numPoints; i++)
            {
                // Parameter t should go from 0 to almost 1 (to avoid NaN issues)
                float t = Mathf.Clamp01((float)i / (numPoints - 1));
        
                Vector3 pointOnCurve = EvaluateNURBSPoint(controlPoints, degree, knotVector, weights, t);
                nurbsPath.Add(pointOnCurve);
            }

            return nurbsPath;
        }
        private static Vector3 EvaluateNURBSPoint(List<Vector3> controlPoints, int degree, List<float> knotVector, List<float> weights, float t)
        {
            int n = controlPoints.Count - 1;

            // Calculate the basis function values
            float[] basisFunctions = new float[controlPoints.Count];
            for (int i = 0; i < controlPoints.Count; i++)
            {
                basisFunctions[i] = CoxDeBoor(i, degree, t, knotVector);
            }

            // Calculate the weighted sum of control points
            Vector3 numerator = Vector3.zero;
            float denominator = 0f;

            for (int i = 0; i <= n; i++)
            {
                float weightBasis = weights[i] * basisFunctions[i];
                numerator += controlPoints[i] * weightBasis;
                denominator += weightBasis;
            }

            // Prevent division by zero, return a valid last point if denominator is very small
            if (denominator < Mathf.Epsilon)
            {
                return controlPoints[n]; // Return the last control point
            }

            return numerator / denominator;
        }
        
        // Cox-de Boor recursive formula for B-spline basis functions
        private static float CoxDeBoor(int i, int degree, float t, List<float> knotVector)
        {
            if (degree == 0)
            {
                return (knotVector[i] <= t && t < knotVector[i + 1]) ? 1f : 0f;
            }

            float leftTerm = 0f;
            float rightTerm = 0f;

            float leftDenominator = knotVector[i + degree] - knotVector[i];
            if (leftDenominator > 0f)
            {
                leftTerm = ((t - knotVector[i]) / leftDenominator) * CoxDeBoor(i, degree - 1, t, knotVector);
            }

            float rightDenominator = knotVector[i + degree + 1] - knotVector[i + 1];
            if (rightDenominator > 0f)
            {
                rightTerm = ((knotVector[i + degree + 1] - t) / rightDenominator) * CoxDeBoor(i + 1, degree - 1, t, knotVector);
            }

            return leftTerm + rightTerm;
        }
        // Function to generate the knot vector based on the number of control points and degree
        private static List<float> GenerateNURBSKnotVector(int controlPointCount, int degree)
        {
            int knotCount = controlPointCount + degree + 1;
            List<float> knotVector = new List<float>();

            // Create a non-uniform open knot vector
            for (int i = 0; i < knotCount; i++)
            {
                if (i < degree)
                {
                    knotVector.Add(0f); // Clamped start
                }
                else if (i >= knotCount - degree)
                {
                    knotVector.Add(1f); // Clamped end
                }
                else
                {
                    float knotValue = (float)(i - degree) / (knotCount - 2 * degree);
                    knotVector.Add(knotValue);
                }
            }

            return knotVector;
        }

// Optional: You can use this to generate default weights if none are provided
        public static List<float> GenerateRationalWeights(int controlPointCount)
        {
            List<float> weights = new List<float>();
            for (int i = 0; i < controlPointCount; i++)
            {
                weights.Add(1f); // Default uniform weight
            }
            return weights;
        }
    }
}