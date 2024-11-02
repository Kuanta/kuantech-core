using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Utils.Math
{
    public class BSpline
    {
        public List<Vector3> SplinePoints;
        
        private float[] _arcLengths = null;
        private float[] _normalizedArcLengths = null;
        public int RotationLookAhead = 5;
        public bool InvertDirection = false;
        public void SetSplinePoints(List<Vector3> controlPoints, int SplineDegree, int SegmentPerSpline)
        {
            SplinePoints = new List<Vector3>();
            SplinePoints = BSpline.GenerateNURBSPath(controlPoints, SplineDegree, null, controlPoints.Count*SegmentPerSpline);
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
        
        public WorldPoint GetPointAtT(float t)
        {
            int lengthOfArray = SplinePoints.Count;
            // Use the normalized arc lengths to map _splineT to the correct segment
            for (int i = 1; i < _normalizedArcLengths.Length; i++)
            {
                if (t <= _normalizedArcLengths[i])
                {
                    int minIndex = i - 1;
                    int maxIndex = i;
                    int rotationTargetIndex = maxIndex + RotationLookAhead;
                    rotationTargetIndex = Mathf.Min(rotationTargetIndex, lengthOfArray - 1);
                    Vector3 direction = SplinePoints[rotationTargetIndex] - SplinePoints[minIndex];
                    // Find the local t value between the two points
                    float segmentT = (t - _normalizedArcLengths[minIndex]) / (_normalizedArcLengths[maxIndex] - _normalizedArcLengths[minIndex]);

                    // Interpolate the position
                    Vector3 floorPos = SplinePoints[minIndex];
                    Vector3 ceilPos = SplinePoints[maxIndex];
                    Vector3 poisition = Vector3.Lerp(floorPos, ceilPos, segmentT);
                    return new WorldPoint()
                    {
                        Position = poisition,
                        Rotation = Quaternion.LookRotation(direction),
                    };
                }
            }

            Vector3 diff = SplinePoints[lengthOfArray - 1] - SplinePoints[lengthOfArray - 2];
            return new WorldPoint()
            {
                Position = SplinePoints[lengthOfArray - 1],
                Rotation = Quaternion.LookRotation(diff),
            };
        }
        
        /// <summary>
        /// Returns the point at given distance
        /// </summary>
        /// <param name="distance"></param>
        /// <returns></returns>
        public WorldPoint GetPointAtDistance(float distance)
        {
            int lengthOfArray = SplinePoints.Count;
            
            if (_arcLengths == null || _arcLengths.Length == 0)
            {
                ComputeArcLengths();
            }
            
            // Eğer distance, toplam spline uzunluğunu aşarsa son noktayı döndürüyoruz
            if (distance >= _arcLengths[lengthOfArray - 1])
            {
                Vector3 lastDirection = SplinePoints[lengthOfArray - 1] - SplinePoints[lengthOfArray - 2];
                return new WorldPoint
                {
                    Position = SplinePoints[lengthOfArray - 1],
                    Rotation = Quaternion.LookRotation(InvertDirection ? -lastDirection : lastDirection)
                };
            }

            // Mesafeye göre segment bulma
            for (int i = 1; i < _arcLengths.Length; i++)
            {
                if (distance <= _arcLengths[i])
                {
                    int minIndex = i - 1;
                    int maxIndex = i;
                    int rotationTargetIndex = Mathf.Min(maxIndex + RotationLookAhead, lengthOfArray - 1);

                    Vector3 direction = SplinePoints[rotationTargetIndex] - SplinePoints[minIndex];
                    
                    // İki segment arasında kalan t değerini hesaplama
                    float segmentDistance = distance - _arcLengths[minIndex];
                    float segmentLength = _arcLengths[maxIndex] - _arcLengths[minIndex];
                    float segmentT = segmentDistance / segmentLength;

                    // Pozisyonu interpolasyonla bulma
                    Vector3 floorPos = SplinePoints[minIndex];
                    Vector3 ceilPos = SplinePoints[maxIndex];
                    Vector3 position = Vector3.Lerp(floorPos, ceilPos, segmentT);

                    return new WorldPoint
                    {
                        Position = position,
                        Rotation = Quaternion.LookRotation(InvertDirection ? -direction : direction)
                    };
                }
            }

            // Mesafe spline'ın sonuna denk geliyorsa son pozisyonu döndürme
            Vector3 finalDirection = SplinePoints[lengthOfArray - 1] - SplinePoints[lengthOfArray - 2];
            Quaternion rotation = InvertDirection
                ? Quaternion.LookRotation(finalDirection * -1)
                : Quaternion.LookRotation(finalDirection);
            return new WorldPoint
            {
                Position = SplinePoints[lengthOfArray - 1],
                Rotation = rotation,
            };
        }

        private float GetArcTotalLength()
        {
            return _arcLengths[^1];
        }
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