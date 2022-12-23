using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.Utils
{
    public static class Helpers
    {
        private static System.Random _rng = new System.Random();
        public static Vector3 ProjectVector(Vector3 vec, Vector3 to)
        {
            Vector3 normalized = to.normalized;
            return Vector3.Dot(vec, normalized) * normalized;
        }
        
        /// <summary>
        /// Calculate nth value of the fibonacci sequence using Dynamic Programming
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public static int FibonacciNumber(int level)
        {
            switch (level)
            {
                case 0:
                case 1:
                    return level;
                case 2:
                    return 2;
            }

            int[] sequence = {2, 1};
            for (int i = 2; i <= level; ++i)
            {
                int nextInSequence = sequence[1] + sequence[0];
                sequence[0] = sequence[1];
                sequence[1] = nextInSequence;
            }
            return sequence[1];
        }

        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;  
            while (n > 1) {  
                n--;  
                int k = _rng.Next(n + 1);  
                (list[k], list[n]) = (list[n], list[k]);
            } 
        }

        public static void IterateChildren(this Transform parent, UnityAction<GameObject> handler)
        {
            Transform[] childs = parent.GetComponentsInChildren<Transform>();
            foreach (var child in childs)
            {
                if(parent == child) continue;
                handler(child.gameObject);
            }
        }

        public static string Stringfy(this float number, bool roundToInteger = false)
        {
            float abs = Mathf.Abs(number);
            string signString = number < 0 ? "- " : "";
            string numberString = "";
            string quantitySuffix = "";
            if (abs > 10E9)
            {
                abs /= 10E9f;
                quantitySuffix = " b";
            }else if (abs > 10E6)
            {
                abs /= 10E6f;
                quantitySuffix = " m";
            }else if (abs > 10E3)
            {
                abs /= 10E3f;
                quantitySuffix = " k";
            }

            if (roundToInteger)
            {
                abs = (int) abs;
            }
            numberString = abs.ToString("F1");
            return signString + numberString + quantitySuffix;
        }
        
        #region Geometry

        public static Vector3 GetRelativeRightVector(this Transform parent, Transform target)
        {
            Vector3 diff = target.position - parent.position;
            Vector3 right = parent.right;
            diff.y = 0;
            return Vector3.Dot(diff, right) * right;
        }

        public static Vector3 GetRelativeForwardVector(this Transform parent, Transform target)
        {
            Vector3 diff = target.position - parent.position;
            Vector3 forward = parent.forward;
            diff.y = 0;
            return Vector3.Dot(diff, forward) * forward;
        }

        public static bool PointIsInAngleRange(this Transform parent, Transform target, float angle)
        {
            float halfAngle = angle * 0.5f;
            Vector3 forward = parent.forward;
            Vector3 diff = target.position - parent.position;
            float angleToPoint = Vector3.SignedAngle(diff, forward, Vector3.up);
            return Mathf.Abs(angleToPoint) <= halfAngle && Vector3.Dot(forward, diff) >= 0f;
        }
        #endregion
    }
}