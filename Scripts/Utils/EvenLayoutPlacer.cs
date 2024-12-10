using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Kuantech.Utils
{
    public class EvenLayoutPlacer : MonoBehaviour
    {
        public float ChildSize = 1;
        public float InnerPadding = 1;
        public float OuterPadding = 1;
        public Vector3 LocalDirection = new Vector3(1, 0, 0);
        //public List<GameObject> Children = new List<GameObject>();
        
        // public void SetChildren(List<GameObject> children)
        // {
        //     Children = children;
        // }
        
        [Button("Update Positions")]
        public void DistributeChilds()
        {
            int childCount = transform.childCount;
            float totatlSize = childCount * ChildSize + Mathf.Max(childCount - 1, 0) * InnerPadding;
            for (int i = 0; i < childCount; ++i)
            {
                Transform child = transform.GetChild(i);
                child.localPosition = LocalDirection * (i*(ChildSize+InnerPadding) - totatlSize*0.5f+ChildSize*0.5f);
                child.localScale = Vector3.one;
                child.transform.localRotation = Quaternion.identity;
            }
                
        }
    }
}