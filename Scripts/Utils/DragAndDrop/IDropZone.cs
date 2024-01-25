namespace Kuantech.Utils
{
    public interface IDropZone
    {
        /// <summary>
        /// Handles dropping. Should return true if draggable is dropped here
        /// </summary>
        /// <param name="draggable"></param>
        /// <returns></returns>
        public bool OnDrop(IDraggable draggable);

        public void ClearSlot(int row, int col);
    }
}