using System.Collections.Generic;
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
    }
}