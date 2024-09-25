using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Utils.Math
{
    public class BSpline
    {
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