namespace Kuantech.Merge
{
    public interface IDraggable
    {
        void DragStart();
        void Drag(UnityEngine.Vector3 cursorPosition);
        void DragEnd();
    }
}