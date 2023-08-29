namespace Kuantech.Merge
{
    public interface IDropZone
    {
        public void OnDragStart();
        public void OnDrag();
        public void OnDragEnd();
        /// <summary>
        /// Handles dropping. Should return true if draggable is dropped here
        /// </summary>
        /// <param name="draggable"></param>
        /// <returns></returns>
        public bool OnDrop(IDraggable draggable);

        public void ClearSlot(int row, int col);
    }
}