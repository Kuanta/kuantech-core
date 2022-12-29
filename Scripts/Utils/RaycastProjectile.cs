using System;
using UnityEngine;

namespace Kuantech.Core.Utils
{
    struct RaycastResultInfo
    {
        public Vector3 position;
    }
    
    public class RaycastProjectile
    {
        private Ray _ray;
        public RaycastHit HitInfo;

        public bool Impacted;
        public Vector3 _initialVelocity;
        public Vector3 _initialPosition;
        private float _flyTime;
        private float _bulletDrop;
        private float _range;
        private float _traveledDistance;

        public delegate void ProjectHitDelegate(RaycastHit hitInfo);

        private ProjectHitDelegate _hitHandler;
        public void Shoot(Vector3 initialPosition, Vector3 aimDireciton, float bulletSpeed, float range, ProjectHitDelegate projectileHitHandler, float bulletDrop = 0.0f)
        {
            _initialVelocity = aimDireciton.normalized * bulletSpeed;
            _initialPosition = initialPosition;
            _bulletDrop = bulletDrop;
            _flyTime = 0f;
            _hitHandler = projectileHitHandler;
            _range = range;
            Impacted = false;
        }

        public void Update(float deltaTime)
        {
            SimulateBullet(deltaTime);
        }

        private void SimulateBullet(float deltaTime)
        {
            Vector3 rayInitialPosition = GetPosition();
            _flyTime += deltaTime;
            Vector3 rayNextPosition = GetPosition();
            Vector3 segment = rayNextPosition - rayInitialPosition;
            float segmentDist = segment.magnitude;
            _traveledDistance += segmentDist;
            if (_traveledDistance > _range)
            {
                Impacted = true;
                return;
            }
            Ray ray = new Ray
            {
                origin = rayInitialPosition,
                direction = (segment).normalized
            };
            if (!UnityEngine.Physics.Raycast(ray, out HitInfo, segmentDist)) return;
            Impacted = true;
            _hitHandler?.Invoke(HitInfo);
        }

        private Vector3 GetPosition()
        {
            Vector3 drop = Vector3.down * _bulletDrop;
            return _initialPosition + (_initialVelocity * _flyTime) + 0.5f * drop * _flyTime * _flyTime;
        }

    }
}