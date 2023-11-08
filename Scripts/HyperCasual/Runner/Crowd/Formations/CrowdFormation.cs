using System;
using DG.Tweening;
using UnityEngine;

namespace Kuantech.Core.HyperCasual.Runner
{
    public enum FormationType
    {
        Circular,
        Rectangular,
        Triangular,
    }

    [Serializable]
    public struct CrowdFormationData
    {
        public FormationType FormationType;

        //Circular
        public float Radius;
        public float AgentRadius;
        public float NoiseMagnitude;

        //Rectangular
        public int MaxColumn;
        public float RowDelta;
        public float ColumnDelta;
    }
    public abstract class CrowdFormation
    {
        public Crowd Crowd;
        public CrowdFormationData FormationData;

        protected float MinX;
        protected float MaxX;
        protected float MaxZ;
        protected float MinZ;
        public CrowdFormation(CrowdFormationData data)
        {
            FormationData = data;
        }
        public virtual void SetCrowdFormation(Transform parent)
        {
            MinX = 0f;
            MaxX = 0f;
            int count = parent.childCount;
            for (int i = 0; i < count; ++i)
            {
                Vector3 localPos = GetLocalPositionForChild(i, count);

                if(localPos.x > MaxX) MaxX = localPos.x;
                if(localPos.x < MinX) MinX = localPos.x;
                if(localPos.z > MaxZ) MaxZ = localPos.z;
                if(localPos.z < MinZ) MinZ = localPos.z;
                parent.transform.GetChild(i).DOLocalMove(localPos, 1f).SetEase(Ease.OutBack);
            }
        }
        public virtual float GetCrowdWidth()
        {
            return MaxX - MinX;
        }
        public virtual float GetCrowdWidthOffset()
        {
            return (MaxX + MinX) * 0.5f;
        }

        public virtual float GetCrowdDepth()
        {
            return MaxZ - MinZ;
        }
        public virtual float GetCrowdDepthOffset()
        {
            return (MaxZ + MinZ) * 0.5f;
        }
        protected abstract Vector3 GetLocalPositionForChild(int childIndex, int workerCount);

    }

}