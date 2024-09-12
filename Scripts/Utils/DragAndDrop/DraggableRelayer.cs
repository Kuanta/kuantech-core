using UnityEngine;

namespace Kuantech.Utils
{
    /// <summary>
    /// A relayer class for cases where Draggable can't have a collider. 
    /// For example if a draggable changes the visual object and it also need to change the collider, a draggableRelayer can be used.
    /// </summary>
    public class DraggableRelayer : MonoBehaviour, IDraggable
    {
        public Draggable Draggable;
        public virtual bool CanBeDragged()
        {
            return Draggable != null && Draggable.CanBeDragged();
        }

        public virtual void Drag(Vector3 cursorPosition)
        {
            Draggable.Drag(cursorPosition);
        }

        public virtual void DragEnd()
        {
            if(Draggable == null) return;
            Draggable.DragEnd();
        }

        public virtual void OnClickDown()
        {
            if(Draggable == null) return;
            Draggable.OnClickDown();
        }

        public virtual void OnClickUp()
        {
            if(Draggable == null) return;
            Draggable.OnClickUp();
        }

        public virtual void OnTap()
        {
            if (Draggable == null) return;
            Draggable.OnTap();
        }

        public virtual bool DragStart()
        {
            if(!CanBeDragged()) return false;
            return Draggable.DragStart();
        }
    }
}