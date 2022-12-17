using UnityEngine;

namespace Kuantech.Core.UI
{
    public abstract class Telemetry : MonoBehaviour
    {
        public abstract void SetLength(float length);
        public abstract void SetFill(float fill);
        public abstract void SetAngle(float angle);

        public virtual void SetWidth(float width)
        {
            
        }

        public void Reset()
        {
            gameObject.SetActive(false);
            SetFill(0f);
        }
    }
}