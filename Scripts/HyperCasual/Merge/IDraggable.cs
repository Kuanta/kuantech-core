namespace Kuantech.Merge
{
    public interface IDraggable
    {
        bool DragStart();
        void Drag(UnityEngine.Vector3 cursorPosition);
        void DragEnd();
    }
}