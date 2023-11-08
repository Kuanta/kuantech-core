using System;
using DG.Tweening;
using UnityEngine;

namespace Kuantech.Core.HyperCasual.Runner
{
    public class CircularFormation : CrowdFormation
    {

        private float _runnerWidth;
        private float _widthOffset;
        private float CurrentAgentCountForRing;
        private float CurrentDeltaAngle;
        private int CurrentRingIndex;
        private int CurrentMaxAgentForRing;

        public CircularFormation(CrowdFormationData data) : base(data)
        {
        }

        public override void SetCrowdFormation(Transform parent)
        {
            CurrentRingIndex = 0;
            CurrentMaxAgentForRing = 0;
            CurrentAgentCountForRing = 0;
            CurrentDeltaAngle = 0f;
            base.SetCrowdFormation(parent);
        }

        private Vector3 GetCartesianPositionFromPolar(float radius, float angle)
        {
            return new Vector3(Mathf.Cos(Mathf.Deg2Rad * angle) * radius, 0, Mathf.Sin(Mathf.Deg2Rad * angle) * radius);
        }


        private float GetRingPerimeter(int ringIndex)
        {
            return 2 * (ringIndex * FormationData.Radius * 2) * Mathf.PI; // R = ringIndex*RadiusPerRing*2
        }

        private int GetMaxAgentCountForRing(int ringIndex)
        {
            return Mathf.FloorToInt(GetRingPerimeter(ringIndex) / FormationData.AgentRadius);
        }


        /// <summary>
        /// Amount of angle change to position agents in that ring
        /// </summary>
        /// <returns></returns>
        private float GetDeltaAngleForRing(int amountOfWorkers)
        {
            return 360.0f / Mathf.Max(amountOfWorkers, 1);
        }

        public override float GetCrowdWidth()
        {
            return _runnerWidth;
        }

        public override float GetCrowdWidthOffset()
        {
            return _widthOffset;
        }


        protected override Vector3 GetLocalPositionForChild(int childIndex, int workerCount)
        {
            float angle = CurrentAgentCountForRing * CurrentDeltaAngle;
            Vector3 newLocalPos = GetCartesianPositionFromPolar(CurrentRingIndex * FormationData.Radius, angle);
            newLocalPos.x += UnityEngine.Random.Range(0f, 1f) * FormationData.NoiseMagnitude;
            newLocalPos.z += UnityEngine.Random.Range(0f, 1f) * FormationData.NoiseMagnitude;

            CurrentAgentCountForRing++;
            if (CurrentAgentCountForRing >= CurrentMaxAgentForRing)
            {
                //We are in the next ring
                int remainingWorkers = workerCount - childIndex - 1;
                if (remainingWorkers >0)
                {
                    CurrentRingIndex++;
                    CurrentMaxAgentForRing = Mathf.Min(GetMaxAgentCountForRing(CurrentRingIndex), remainingWorkers);
                    CurrentDeltaAngle = GetDeltaAngleForRing(CurrentMaxAgentForRing);
                    CurrentAgentCountForRing = 0;
                }
            }
            return newLocalPos;
        }
    }
}