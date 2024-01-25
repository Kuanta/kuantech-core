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
            return Draggable.CanBeDragged();
        }

        public void Drag(Vector3 cursorPosition)
        {
            Draggable.Drag(cursorPosition);
        }

        public void DragEnd()
        {
            Draggable.DragEnd();
        }

        public bool DragStart()
        {
            return Draggable.DragStart();
        }
    }
}