using UnityEngine;

namespace Kuantech.Utils.Math
{
    public class Trajectory
    {
        public static Vector3 GetTrajectoryPoint(Vector3 start, Vector3 end, float maxHeight, float t)
        {
            Vector3 point = Vector3.Lerp(start, end, t);
            point.y += 4 * maxHeight * t * (1 - t); //A parabolic equation
            return point;
        }

        public static Vector3[] CreateTrajectorySample(Vector3 start, Vector3 end, float maxHeight, int sampleCount)
        {
            Vector3[] samples = new Vector3[sampleCount];
            for(int i=0;i<sampleCount;++i)
            {
                samples[i] = GetTrajectoryPoint(start, end, maxHeight, (float)i / (sampleCount - 1));
            }

            return samples;
        }
    }
}