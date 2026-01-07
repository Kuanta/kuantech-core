using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using Kuantech.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Kuantech.Utils
{
    public static class Helpers
    {
        private static System.Random _rng = new System.Random();
        
        #region Math
        
        /// <summary>
        /// Normalized the value between min and max
        /// </summary>
        /// <param name="value"></param>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <returns></returns>
        public static float NormalizeBetweenValues(float value, float maxValue, float minValue = 0)
        {
            return Mathf.Clamp01((value - minValue) / Mathf.Max(maxValue - minValue, 0.01f));
        }
        
        //Snaps a value to increments
        public static float SnapToIncrements(float value, float increment)
        {
            int divisions = Mathf.FloorToInt(value / increment);
            float val = divisions * increment;
            return val + increment * 0.5f;
        }
        #endregion

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
            if(list.Count == 0) return null;
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
        
        public static bool CheckProbability(float probability)
        {
            return Random.Range(0f, 1f) <= probability;
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
        

        public static float Fmod(float x, float y)
        {
            return x - y * Mathf.Floor(x / y);
        }
        
        #region String
        public static string Stringfy(this float number, bool roundToInteger = false, bool roundSmallerToInteger = false, bool quantify = true)
        {
            float abs = Mathf.Abs(number);
            string signString = number < 0 ? "- " : "";
            string numberString = "";
            string quantitySuffix = "";

            if (quantify)
            {
                if (abs >= 1E18f)
                {
                    abs /= 1E18f;
                    quantitySuffix = "qi"; // quintillion
                }
                else if (abs >= 1E15f)
                {
                    abs /= 1E15f;
                    quantitySuffix = "q"; // quadrillion
                }
                else if (abs >= 1E12f)
                {
                    abs /= 1E12f;
                    quantitySuffix = "t"; // trillion
                }
                else if (abs >= 1E9f)
                {
                    abs /= 1E9f;
                    quantitySuffix = "b";
                }else if (abs >= 1E6)
                {
                    abs /= 1E6f;
                    quantitySuffix = "m";
                }else if (abs >= 1E3)
                {
                    abs /= 1E3f;
                    quantitySuffix = "k";
                }
                else if(roundSmallerToInteger)
                {
                    roundToInteger = true; //Round numbers smaller than 1k to integer
                }
            }


            if (abs - Mathf.Floor(abs) == 0) roundToInteger = true; //If no value after decimal point, to integer
            if (roundToInteger)
            {
                abs = Mathf.Ceil(abs);
                numberString = abs.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                numberString = abs.ToString("F1", CultureInfo.InvariantCulture);
            }
            return signString + numberString + quantitySuffix;
        }

        public static string Stringfy(this int number, bool roundToInteger = false,
            bool roundSmallerToInteger = false, bool quantify = true)
        {
            return ((float) number).Stringfy(roundToInteger, roundSmallerToInteger, quantify);
        }
        
        public static float TryParseFloat(this string text, float defaultVal)
        {
            return float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : defaultVal;
        }

        public static int TryParseInt(this string text, int defaultVal)
        {
            return int.TryParse(text, out var parsed) ? parsed : defaultVal;
        }

        public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable) => enumerable == null || !enumerable.Any();

        public static bool IsValidIndex<T>(this List<T> list, int index)
        {
            if (list == null) return false;
            if (list.Count > index && index >= 0) return true;
            return false;
        }
        
        /// <summary>
        /// Tries to split a text to float array
        /// </summary>
        /// <param name="arraytext"></param>
        /// <param name="seperator"></param>
        /// <param name="defaultVal"></param>
        /// <returns></returns>
        public static float[] SplitToFloats(string arraytext, string seperator, float defaultVal)
        {
            string[] splitText = arraytext.Split(seperator);
            float[] floatArray = new float[splitText.Length];
            for (int i = 0; i < splitText.Length; ++i)
            {
                floatArray[i] = splitText[i].TryParseFloat(defaultVal);
            }

            return floatArray;
        }
        
        /// <summary>
        /// Tries to split a text to int array
        /// </summary>
        /// <param name="arraytext"></param>
        /// <param name="seperator"></param>
        /// <param name="defaultVal"></param>
        /// <returns></returns>
        public static int[] SplitToInts(string arraytext, string seperator, int defaultVal)
        {
            string[] splitText = arraytext.Split(seperator);
            int[] intArray = new int[splitText.Length];
            for (int i = 0; i < splitText.Length; ++i)
            {
                intArray[i] = splitText[i].TryParseInt(defaultVal);
            }

            return intArray;
        }
        #endregion
        
        public static Vector2 Get2D(this Vector3 vector3)
        {
            return new Vector2(vector3.x, vector3.z);
        }

        #region Probability
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
        
        #endregion

        #region Time

        public static void GetMinSecMil(float elapsedTime, out int minutes, out int seconds, out int milliseconds)
        {
            minutes = Mathf.FloorToInt(elapsedTime / 60f);
            seconds = Mathf.FloorToInt(elapsedTime % 60f);
            milliseconds = Mathf.FloorToInt((elapsedTime * 100f) % 100f);
        }

        #endregion
        
        #region Geometry

        /// <summary>
        /// Verilen direction, worldUp ve local düzlem forward'ına göre bir rotasyon döndürür.
        /// </summary>
        /// <param name="direction">Hedef yön</param>
        /// <param name="worldUp">Tanımlı dünya yukarı vektörü (örnek: Vector3.up veya Vector3.forward)</param>
        /// <param name="referenceForward">Ground düzlemindeki yerel ileri yön (örnek: Vector3.forward veya Vector3.up)</param>
        /// <returns>Uygulanabilir Quaternion rotasyonu</returns>
        public static Quaternion GetRotationFromWorldForward(Vector3 direction, Vector3 worldUp, Vector3 referenceForward)
        {
            if (direction == Vector3.zero)
                return Quaternion.identity;

            // 1. Ground düzlemine projeksiyon
            Vector3 groundDir = Vector3.ProjectOnPlane(direction, worldUp);
            if (groundDir.sqrMagnitude < 1e-6f)
            {
                // Tam yukarı/aşağıya bakıyorsa fallback
                return Quaternion.LookRotation(direction.normalized, worldUp);
            }

            // 2. Yaw açısı: düzlemdeki yön ile referans ileri yön arasındaki açı
            float yaw = Vector3.SignedAngle(referenceForward, groundDir, worldUp);

            // 3. Pitch açısı: yönün yer düzlemiyle yaptığı dik açı
            Vector3 pitchAxis = Vector3.Cross(groundDir, worldUp).normalized;
            float pitch = Vector3.SignedAngle(groundDir, direction, pitchAxis);

            // 4. Rotasyonları sırayla uygula
            Quaternion yawRot = Quaternion.AngleAxis(yaw, worldUp);
            Quaternion pitchRot = Quaternion.AngleAxis(pitch, pitchAxis);

            return yawRot ;
        }
        
        public static Vector3 ProjectVector(Vector3 vec, Vector3 to)
        {
            Vector3 normalized = to.normalized;
            return Vector3.Dot(vec, normalized) * normalized;
        }

        public static float DotProjection(Vector3 vec, Vector3 to)
        {
            Vector3 normalized = to.normalized;
            float dotProduct = Vector3.Dot(vec, normalized);
            float roundedDotProduct = Mathf.Round(dotProduct * 100f) / 100f;
            return roundedDotProduct;
        }

        public static Vector3 CheckRayAgainstPlane(Ray ray, Vector3 planeNormal, Vector3 planePoint)
        {
            Vector3 o = ray.origin;
            Vector3 d = ray.direction;
            float denom = Vector3.Dot(planeNormal, d);
            
            if (Mathf.Abs(denom) < 1e-6f)
            {
                return planePoint;
            }
            
            float t = Vector3.Dot(planePoint - o, planeNormal) / denom;
            
            if (t < 0f)
            {
                return planePoint;
            }

            Vector3 intersection = o + t * d;
            return intersection;
        }
        
        /// <summary>
        /// Projects a vector to a plane
        /// </summary>
        /// <param name="p"></param>
        /// <param name="planeNormal"></param>
        /// <param name="planePoint"></param>
        /// <returns></returns>
        public static Vector3 ProjectVectorOnPlane(Vector3 p, Vector3 planeNormal, Vector3 planePoint)
        {
            // (p - planePoint) dot planeNormal
            float dist = Vector3.Dot(p - planePoint, planeNormal);
        
            // planeNormal'un uzunluğunun karesi
            float magSq = Vector3.Dot(planeNormal, planeNormal);
        
            if (Mathf.Approximately(magSq, 0f))
            {
                // Normal vektörü yoksa (0,0,0) -> tanımsız
                return p;
            }

            // p' = p - (dist / magSq) * planeNormal
            Vector3 projection = p - (dist / magSq) * planeNormal;
            return projection; 
        }
        
        public static Vector2 GetVector2FromVector3WithUpDirection(Vector3 v, Vector3 up)
        {
         // 1. Normalize up vector
            Vector3 upNorm = up.normalized;
        
            // 2. Remove the component in up direction
            Vector3 projected = v - Vector3.Dot(v, upNorm) * upNorm;
        
            // 3. Build coordinate axes on the plane
            Vector3 right = Vector3.Cross(Vector3.forward, upNorm);
            if (right == Vector3.zero) right = Vector3.right; // fallback
            right.Normalize();
        
            Vector3 forward = Vector3.Cross(upNorm, right);
        
            // 4. Project onto these axes
            float x = Vector3.Dot(projected, right);
            float y = Vector3.Dot(projected, forward);
        
            return new Vector2(x, y);
        }
        
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
        public static void IterateChildren(this Transform parent, UnityAction<GameObject> handler)
        {
            Transform[] childs = parent.GetComponentsInChildren<Transform>();
            foreach (var child in childs)
            {
                if(parent == child) continue;
                handler(child.gameObject);
            }
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
        public static bool IsCursorOnUI()
        {
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);

                if (EventSystem.current.IsPointerOverGameObject(touch.fingerId) || EventSystem.current.currentSelectedGameObject != null)
                {
                    return true;
                }
            }
#else
            if (EventSystem.current == null) return false;
            if (EventSystem.current.IsPointerOverGameObject() || EventSystem.current.currentSelectedGameObject != null)
            {
                return true;
            }
#endif
            return false;
        }
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

        /// <summary>
        /// Transforms a local bounds to given global position and rotation
        /// </summary>
        /// <param name="localBounds">Bounds in the local space</param>
        /// <param name="position">Position in global space</param>
        /// <param name="rotation">Position in local space</param>
        /// <returns></returns>
        public static Bounds TransformBounds(Bounds localBounds, Vector3 position, Quaternion rotation)
        {
            Matrix4x4 localToWorldMatrix = Matrix4x4.TRS(position, rotation, Vector3.one);

            Vector3 center = localToWorldMatrix.MultiplyPoint(localBounds.center);

            // Transform the local extents to world space
            Vector3 extentsX = localToWorldMatrix.MultiplyVector(new Vector3(localBounds.extents.x, 0, 0));
            Vector3 extentsY = localToWorldMatrix.MultiplyVector(new Vector3(0, localBounds.extents.y, 0));
            Vector3 extentsZ = localToWorldMatrix.MultiplyVector(new Vector3(0, 0, localBounds.extents.z));

            // Calculate the new extents
            float x = Mathf.Abs(extentsX.x) + Mathf.Abs(extentsY.x) + Mathf.Abs(extentsZ.x);
            float y = Mathf.Abs(extentsX.y) + Mathf.Abs(extentsY.y) + Mathf.Abs(extentsZ.y);
            float z = Mathf.Abs(extentsX.z) + Mathf.Abs(extentsY.z) + Mathf.Abs(extentsZ.z);

            return new Bounds(center, new Vector3(x, y, z));
        }
        
        /// <summary>
        /// Instantiates a prefab, considering the editor context
        /// </summary>
        /// <param name="prefab"></param>
        /// <returns></returns>
        public static GameObject InstantiatePrefab(GameObject prefab)
        {
            #if UNITY_EDITOR
            if (Application.isPlaying)
            {
                return Object.Instantiate(prefab);
            }
            else
            {
                return PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            }
#else
return GameObject.Instantiate(prefab);
#endif
        }

        public static void DestroyGameObject(GameObject gameObj)
        {
#if UNITY_EDITOR
            if (Application.isPlaying)
            {
                GameObject.Destroy(gameObj);
            }
            else
            {
                GameObject.DestroyImmediate(gameObj);
            }
#else
            GameObject.Destroy(gameObj);
#endif
        }
        public static void DestroyAllChildren(this Transform transform)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                #if UNITY_EDITOR
                if(Application.isPlaying)
                {
                    GameObject.Destroy(transform.GetChild(i).gameObject);
                }
                else{
                    GameObject.DestroyImmediate(transform.GetChild(i).gameObject);
                }
#else
                GameObject.Destroy(transform.GetChild(i).gameObject);
#endif
            }
        }

        public static void ToggleChild(this Transform transform, int childIndex)
        {
            for (int i = 0; i < transform.childCount; ++i)
            {
                transform.GetChild(i).gameObject.SetActive(childIndex == i);
            }
        }
        
        public static void PoolAllChildren(this Transform transform)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                PoolManager.PoolObject(transform.GetChild(i).gameObject);
            }
        }
        
        public static void AttachChild(this GameObject gameObject, Transform child)
        {
            child.gameObject.AttachToParent(gameObject.transform);
        }

        public static void AttachToParent(this GameObject gameObject, Transform parent)
        {
            gameObject.transform.SetParent(parent);
            gameObject.transform.localPosition = Vector3.zero;
            gameObject.transform.localRotation = Quaternion.identity;
            gameObject.transform.localScale = Vector3.one;
        }
        
        public static void SortChildren(this Transform parentTransform, Comparison<Transform> comparison)
        {
            List<Transform> children = new List<Transform>();
            for (int i = 0; i < parentTransform.childCount; i++)
            {
                children.Add(parentTransform.GetChild(i));
            }
            
            children.Sort(comparison);
            
            for (int i = 0; i < children.Count; i++)
            {
                children[i].SetSiblingIndex(i);
            }
        }
        
        public static Transform FindDeepChild(this Transform aParent, string aName)
        {
            foreach(Transform child in aParent)
            {
                if(child.name == aName) return child;
                var result = child.FindDeepChild(aName);
                if (result != null) return result;
            }
            return null;
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

        #region Serialization
        public static byte[] Serialize(object obj)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream memoryStream = new MemoryStream())
            {
                formatter.Serialize(memoryStream, obj);
                return memoryStream.ToArray();
            }
        }

        public static T Deserialize<T>(byte[] data)
        {
            try
            {
                BinaryFormatter formatter = new BinaryFormatter();
                using (MemoryStream memoryStream = new MemoryStream(data))
                {
                    return (T)formatter.Deserialize(memoryStream);
                }
            }
            catch (Exception ex)
            {
                return default(T); // Return default value if deserialization fails
            }
        }
        #endregion
    }
}