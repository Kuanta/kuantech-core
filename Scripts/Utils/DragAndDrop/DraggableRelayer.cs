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
        public bool CanBeDragged()
        {
            return Draggable != null && Draggable.CanBeDragged();
        }

        public void Drag(Vector3 cursorPosition)
        {
            Draggable.Drag(cursorPosition);
        }

        public void DragEnd()
        {
            if(Draggable == null) return;
            Draggable.DragEnd();
        }

        public void OnClickDown()
        {
            if(Draggable == null) return;
            Draggable.OnClickDown();
        }

        public void OnClickUp()
        {
            if(Draggable == null) return;
            Draggable.OnClickUp();
        }

        public void OnTap()
        {
            if (Draggable == null) return;
            Draggable.OnTap();
        }

        public bool DragStart()
        {
            if(!CanBeDragged()) return false;
            return Draggable.DragStart();
        }
    }
}