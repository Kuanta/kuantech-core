using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Merge
{
    public class Slottable : MonoBehaviour
    {
        public IDropZone slottedZone; 
        public int Row = -1;
        public int Column = -1;

        public void ClearSlot()
        {
            if (slottedZone == null) return;
            slottedZone.ClearSlot(Row, Column);
        }
    }
}