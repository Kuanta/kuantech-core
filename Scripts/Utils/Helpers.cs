using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Kuantech.Core.Utils;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

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

        public static float DotProjection(Vector3 vec, Vector3 to)
        {
            Vector3 normalized = to.normalized;
            return Vector3.Dot(vec, normalized); 
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
            
        public static T GetRandomElement<T>(this IList<T> list) where T:class
        {
            int n = list.Count;
            return list[Random.Range(0, n)];
        }
        
        public static T GetRandomElement<T>(List<T> list)
        {
            if (list == null || list.Count == 0)
            {
                throw new ArgumentException("List cannot be null or empty.");
            }

            int index = _rng.Next(list.Count);
            return list[index];
        }
        
        /// <summary>
        /// Removes an item from a queue.
        /// </summary>
        /// <typeparam name="T">The type of items in the queue.</typeparam>
        /// <param name="queue">The queue to process.</param>
        /// <param name="itemToRemove">The item to remove.</param>
        /// <returns>Returns true if the item was found and removed, false otherwise.</returns>
        public static bool RemoveFromQueue<T>(Queue<T> queue, T itemToRemove)
        {
            int initialCount = queue.Count;
            bool found = false;

            // Using a temporary list to hold dequeued items
            List<T> tempList = new List<T>();

            for (int i = 0; i < initialCount; i++)
            {
                T currentItem = queue.Dequeue();

                if (EqualityComparer<T>.Default.Equals(currentItem, itemToRemove))
                {
                    found = true;
                    continue;
                }

                tempList.Add(currentItem);
            }

            // Re-enqueue items that aren't removed
            foreach (T item in tempList)
            {
                queue.Enqueue(item);
            }

            return found;
        }
        
        private static readonly float GaussianNormalizationFactor = Mathf.Sqrt(2 * Mathf.PI);
        public static float Gaussian(float x, float mu, float sigma)
        {
            return (1 / (sigma * GaussianNormalizationFactor)) *
                   Mathf.Exp(-1 * (x - mu) * (x - mu) / (2 * sigma * sigma));
        }
        
        /// <summary>
        /// Define a gaussian where the result is 1 between two mean values
        /// </summary>
        /// <param name="x"></param>
        /// <param name="mu1">min mean</param>
        /// <param name="mu2">max mean</param>
        /// <param name="sigma"></param>
        /// <returns></returns>
        public static float PlateauGaussian(float x, float mu1, float mu2, float sigma)
        {
            if (x >= mu1 && x <= mu2) return 1;
            if (x < mu1) return Gaussian(x, mu1, sigma);
            if (x > mu2) return Gaussian(x, mu2, sigma);
            return 0;
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

        public static string Stringfy(this float number, bool roundToInteger = false, bool roundSmallerToInteger = false)
        {
            float abs = Mathf.Abs(number);
            string signString = number < 0 ? "- " : "";
            string numberString = "";
            string quantitySuffix = "";
            if (abs > 1E9)
            {
                abs /= 1E9f;
                quantitySuffix = "b";
            }else if (abs > 1E6)
            {
                abs /= 1E6f;
                quantitySuffix = "m";
            }else if (abs > 1E3)
            {
                abs /= 1E3f;
                quantitySuffix = "k";
            }
            else if(roundSmallerToInteger)
            {
                roundToInteger = true; //Round numbers smaller than 1k to integer
            }

            if (abs - Mathf.Floor(abs) == 0) roundToInteger = true; //If no value after decimal point, to integer
            if (roundToInteger)
            {
                abs = (int) abs;
                numberString = abs.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                numberString = abs.ToString("F1", CultureInfo.InvariantCulture);
            }
            return signString + numberString + quantitySuffix;
        }

        public static string Stringfy(this int number, bool roundToInteger = false,
            bool roundSmallerToInteger = false)
        {
            return ((float) number).Stringfy();
        }
        
        public static float TryParseFloat(this string text, float defaultVal)
        {
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : defaultVal;
        }

        public static int TryParseInt(this string text, int defaultVal)
        {
            return int.TryParse(text, out var parsed) ? parsed : defaultVal;
        }
        
        public static Vector2 Get2D(this Vector3 vector3)
        {
            return new Vector2(vector3.x, vector3.z);
        }
        /// <summary>
        /// Returns an index from a list of probabilities
        /// </summary>
        /// <param name="probabilities">List of probabilities. Doesn't have to be normalized</param>
        /// <returns></returns>
        public static int DrawFromWeightedProbabilities(float[] probabilities)
        {
            float total = probabilities.Sum();
            float rand = Random.Range(0, total);
            float currentMin = 0f;
            for (int i = 0; i < probabilities.Length; ++i)
            {
                if (rand < probabilities[i] + currentMin)
                {
                    return i;
                }

                currentMin += probabilities[i];
            }

            return probabilities.Length - 1;
        }
        
        public static void ChangeTagRecursively(this Transform transform, string newTag)
        {
            transform.tag = newTag;
 
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                ChangeTagRecursively(child, newTag);
            }
        }
        public static void ChangeLayerRecursively(this Transform transform, int newLayer)
        {
            transform.gameObject.layer = newLayer;
 
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                ChangeLayerRecursively(child, newLayer);
            }
        }

        #region Time

        public static void GetMinSecMil(float elapsedTime, out int minutes, out int seconds, out int milliseconds)
        {
            minutes = Mathf.FloorToInt(elapsedTime / 60f);
            seconds = Mathf.FloorToInt(elapsedTime % 60f);
            milliseconds = Mathf.FloorToInt((elapsedTime * 100f) % 100f);
        }

        #endregion
        
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

        public static Vector3 ProjectPointOnSegment(Vector3 point, Vector3 segmentStart, Vector3 segmentEnd)
        {
            Vector3 segmentDirection = (segmentEnd - segmentStart).normalized;
            return Vector3.Dot((point - segmentStart), segmentDirection) * segmentDirection + segmentStart;
        }
        #endregion

        #region GameObjects

        public static Bounds CalculateBounds(GameObject parentObject)
        {
            // Get all renderers of this GameObject and its children
            Renderer[] renderers = parentObject.GetComponentsInChildren<Renderer>();

            // If there's no renderer, the bounds are undefined
            if (renderers.Length == 0)
            {
                return new Bounds();
            }

            // Start by setting the complete bounds to the first renderer's bounds
            Bounds completeBounds = renderers[0].bounds;

            // Go through the other renderers, encapsulating all bounds
            for (int i = 1; i < renderers.Length; i++)
            {
                completeBounds.Encapsulate(renderers[i].bounds);
            }

            return completeBounds;
        }
        
        public static void DestroyAllChildren(this Transform transform)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject.Destroy(transform.GetChild(i).gameObject);
            }
        }
        #endregion
        
        #region Tricks
        public static List<Type> GetAllDerivedTypes(AppDomain currentDomain, Type baseType)
        {
            return currentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsSubclassOf(baseType)).ToList();
        }
        
        /// <summary>
        /// Gets all the public variables of a type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static FieldInfo[] GetSerializedFields(Type type)
        {
            if (type == null)
            {
                return null;
            }
            FieldInfo[] publicFields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            FieldInfo[] serializedFields =  type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(field => field.GetCustomAttribute<SerializeField>() != null)
                .ToArray();
            
            return publicFields.Concat(serializedFields).ToArray();
        }
        
        #endregion

        #region Custom Attributes

        public static void ResetAttributes(object target)
        {
            // Ensure target is not null
            if (target == null) return;

            // Get all fields of the target object's class
            FieldInfo[] fields = target.GetType().GetFields();

            foreach (FieldInfo field in fields)
            {
                // Check if field has the Resettable attribute
                ResettableAttribute attribute = (ResettableAttribute)Attribute.GetCustomAttribute(field, typeof(ResettableAttribute));

                if (attribute != null)
                {
                    // Set field value to its default
                    field.SetValue(target, attribute.DefaultVal);
                }
            }
        }

        #endregion
    }
}