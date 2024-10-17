using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.Utils
{
    public class EvenLayoutPlacer : MonoBehaviour
    {
        public float ChildSize = 1;
        public float InnerPadding = 1;
        public float OuterPadding = 1;
        public Vector3 LocalDirection = new Vector3(1, 0, 0);
        private List<GameObject> Children;
        
        public void SetChildren(List<GameObject> children)
        {
            Children = children;
        }
        
        public void DistributeChilds()
        {
            float totatlSize = Children.Count * ChildSize + Mathf.Max(Children.Count - 1, 0) * InnerPadding;
            for (int i = 0; i < Children.Count; ++i)
            {
                Children[i].transform.SetParent(transform);
                Children[i].transform.localPosition = LocalDirection * (i*(ChildSize+InnerPadding) - totatlSize*0.5f+ChildSize*0.5f);
                Children[i].transform.localScale = Vector3.one;
                Children[i].transform.localRotation = Quaternion.identity;
            }
                
        }
    }
}