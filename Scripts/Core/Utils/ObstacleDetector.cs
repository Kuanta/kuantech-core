using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Core.Utils
{
    [Serializable]
    public struct ObstacleSensor
    {
        public Vector3 Direction;
        public float DepthOffset;
        public float HeightOffset;
        public float Width;
        public float Height;
        public float Range;
    }
    
    public class ObstacleDetector : MonoBehaviour
    {
        // public List<Transform> SensorPositions;
        // private List<Vector3> _localPositions = new List<Vector3>();
        // private List<Quaternion> _localForwardDirections = new List<Quaternion>();
        [SerializeField] private int SensorCount = 4;
        public List<ObstacleSensor> ObstacleSensors = new List<ObstacleSensor>();
        [SerializeField] private List<LayerMask> LayerMasks = new List<LayerMask>();
        public List<bool> DetectedObstacle;
        [SerializeField] private LayerMask DefaultLayerMask;

        public List<Collider> Collisions;
        
        private void Awake()
        {
            LayerMasks ??= new List<LayerMask>();
            Collisions = new List<Collider>();
            bool fillSensors = false;
            if (LayerMasks.Count != SensorCount)
            {
                fillSensors = true;
                LayerMasks.Clear();
            }
            for (int i = 0; i < SensorCount; ++i)
            {
                if(fillSensors) LayerMasks.Add(DefaultLayerMask);
                ObstacleSensors.Add(new ObstacleSensor
                {
                    Width = 0.5f,
                    Height = 0.5f,
                    Range = 0.3f,
                });
                Collisions.Add(null);
            }
        }
        
        public void SetSensorDirection(int sensorId, Vector3 globalDirection, float depthOffset, float heightOffset)
        {
            ObstacleSensor sensor = ObstacleSensors[sensorId];
            sensor.Direction = globalDirection;
            sensor.DepthOffset = depthOffset;
            sensor.HeightOffset = heightOffset;
            ObstacleSensors[sensorId] = sensor;
        }

        public void SetSensorDimensions(int sensorId, float width, float height)
        {
            ObstacleSensor sensor = ObstacleSensors[sensorId];
            sensor.Width = width;
            sensor.Height = height;
            ObstacleSensors[sensorId] = sensor;
        }

        public void SetSensorRange(int sensorId, float range)
        {
            ObstacleSensor sensor = ObstacleSensors[sensorId];
            sensor.Range = range;
            ObstacleSensors[sensorId] = sensor;
        }
        
        /// <summary>
        /// Checks for obstacles around given sensor positions
        /// </summary>
        /// <returns></returns>
        public List<bool> CheckObstacles()
        {
            DetectedObstacle = new List<bool>();
            for (int i = 0; i < ObstacleSensors.Count; ++i)
            {
                ObstacleSensor sensor = ObstacleSensors[i];
                DetectedObstacle.Add(false);
                RaycastHit hit;
                Vector3 sensorOrigin = transform.position + Vector3.up * sensor.HeightOffset +
                                       sensor.Direction * sensor.DepthOffset; 
                DetectedObstacle[i] = CheckObstacleForSensor(sensorOrigin, new Vector3(sensor.Width*0.5f, sensor.Height*0.5f,0.1f),
                    sensor.Direction, LayerMasks[i], sensor.Range, out hit);
                if (DetectedObstacle[i])
                {
                    Collisions[i] = hit.collider;
                }
                else
                {
                    Collisions[i] = null;
                }
            }
            return DetectedObstacle;
        }

        private bool CheckObstacleForSensor(Vector3 center, Vector3 halfExtends, Vector3 direction, int layerMask, float range, out RaycastHit hit)
        {
            bool result =  UnityEngine.Physics.BoxCast(center, halfExtends, direction, out hit, Quaternion.LookRotation(direction),
                range, layerMask, QueryTriggerInteraction.Collide);
            
            return result;
        }
    }
}