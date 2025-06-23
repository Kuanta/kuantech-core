using Kuantech.Merge;
using UnityEngine;

namespace Kuantech.Utils
{
    public class DropZone : MonoBehaviour, IDropZone
    {
        public void ClearSlot(int row, int col)
        {
        }

        public bool OnDrop(IDraggable draggable)
        {
            return true;
        }
    }
}