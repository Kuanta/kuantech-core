using System;
using UnityEngine;

namespace Kuantech.Utils
{
    [Serializable]
    public class SphericalCoordinate
    {
        public float Radius;
        public float Yaw; 
        public float Pitch;

        public SphericalCoordinate(float radius, float yaw, float pitch)
        {
            this.Yaw = yaw;
            this.Pitch = pitch;
            this.Radius = radius;
        }
        public static SphericalCoordinate FromDirection(Vector3 direction, float radius)
        {
            Vector3 eulers = Quaternion.LookRotation(direction.normalized, Vector3.up).eulerAngles;
            float yaw = Mathf.Deg2Rad * eulers.y;
            float pitch = Mathf.PI*0.5f;
            return new SphericalCoordinate(radius, yaw, pitch);
        }

        public static Vector3 ToWorld(SphericalCoordinate coord)
        {
            return ToWorld(coord.Radius, coord.Yaw, coord.Pitch);
        }
        public static Vector3 ToWorld(float radius, float yaw, float pitch)
        {
            return new Vector3(radius*Mathf.Cos(yaw)*Mathf.Sin(pitch),
                radius * Mathf.Cos(pitch), radius * Mathf.Sin(yaw) * Mathf.Sin(pitch));
        }
        public Vector3 ToWorld()
        {
            return ToWorld(Radius, Yaw, Pitch);
        }
        
        /// <summary>
        /// Get forward vector from spherical coordinate
        /// </summary>
        /// <returns></returns>
        public Vector3 GetForward()
        {
            return new Vector3(-Mathf.Cos(Yaw), 0, -Mathf.Sin(Yaw));
        }
    }
}