using UnityEngine;

namespace Kuantech.Utils
{
    public interface IDraggable
    {
        bool CanBeDragged();
        bool DragStart(Vector3 dragHitPosition);
        void Drag(UnityEngine.Vector3 cursorPosition, Vector3 cursorPositionChange);
        void DragEnd();
        void OnClickDown();
        void OnClickUp();
        void OnTap(Vector3 hitPoint);
        virtual Vector3 GetCursorPosition(){
            return Input.mousePosition;
        }
    }
}