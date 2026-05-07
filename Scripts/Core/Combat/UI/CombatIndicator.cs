using System;
using System.Collections.Generic;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core.Combat
{
    public class CombatIndicator : MonoBehaviour
    {
        public enum CombatIndicatorType
        {
            NONE,
            ARC,
            LINEAR,
            CIRCLE,
        }

        public virtual CombatIndicatorType Type => CombatIndicatorType.NONE;

        [Serializable]
        public struct CombatIndicatorData
        {
            public float Duration;
            public float Range;
            public float Width;
            public float Angle;
            public WorldPoint PlayPoint;
            public Vector3 Direction;
            public bool SetPosition;
            public bool SetDirection;
            public Actor Owner;
        }

        public bool DespawnAfterEnd = true;
        private float _startShowTime;
        private bool _started;
        private float _duration;
        private Actor _owner;

        public void Show(CombatIndicatorData indicatorData)
        {
            gameObject.SetActive(true);
            _owner = indicatorData.Owner;
            _started = true;
            _startShowTime = Time.time;
            _duration = Mathf.Max(0.01f, indicatorData.Duration);

            if(indicatorData.PlayPoint != null)
            {
                if (indicatorData.PlayPoint.Target != null)
                {
                    gameObject.AttachToParent(indicatorData.PlayPoint.Target);
                }
                if (indicatorData.SetPosition)
                {
                    transform.position = indicatorData.PlayPoint.GetTargetPosition();
                }
            }

            if(indicatorData.SetDirection)
            {
                Vector3 dir = indicatorData.Direction;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            }

            Setup(indicatorData);
        }

        protected virtual void Setup(CombatIndicatorData data) { }

        private void Update()
        {
            if(!_started) return;

            float normalizedTime = GetNormalizedTime();
            SetFill(normalizedTime);
            if(normalizedTime >= 1)
            {
                EndIndicator();
            }
        }

        public void EndIndicator()
        {
            _started = false;
            if (DespawnAfterEnd)
            {
                PoolManager.PoolObject(gameObject);
            }
            else //Simply disable
            {
                gameObject.SetActive(false);
            }
        }

        protected virtual void SetFill(float fill)
        {
            
        }

        private float GetNormalizedTime()
        {
            return Mathf.Clamp01((Time.time - _startShowTime) / _duration);
        }
    }
}