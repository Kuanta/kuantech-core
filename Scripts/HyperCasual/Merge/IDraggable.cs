namespace Kuantech.Merge
{
    public interface IDraggable
    {
        bool CanBeDragged();
        bool DragStart();
        void Drag(UnityEngine.Vector3 cursorPosition);
        void DragEnd();
    }
}