using UnityEngine;

namespace Kuantech.Utils
{
    public interface IDraggable
    {
        bool CanBeDragged();
        bool DragStart();
        void Drag(UnityEngine.Vector3 cursorPosition);
        void DragEnd();
        void OnClickDown();
        void OnClickUp();
        void OnTap(Vector3 hitPoint);
        virtual Vector3 GetCursorPosition(){
            return Input.mousePosition;
        }
    }
}