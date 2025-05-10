using System;
using UnityEngine;

namespace Kuantech.Puzzle
{
    public class DraggableSlot : MonoBehaviour
    {
        [NonSerialized] public GridTileDraggable CurrentDraggable;

        public void SetDraggable(GridTileDraggable draggable)
        {
            draggable.OnPlacedToBoard -= OnDraggablePlaced;
            draggable.OnPlacedToBoard += OnDraggablePlaced;
        }

        public void UnsetDraggable()
        {
            if (CurrentDraggable == null) return;
            CurrentDraggable.OnPlacedToBoard -= OnDraggablePlaced;
            CurrentDraggable = null;
        }

        private void OnDraggablePlaced(GridTileDraggable tileDraggable)
        {
            if (tileDraggable != CurrentDraggable) return;
            UnsetDraggable();
        }

        public void Reset()
        {
            if (CurrentDraggable == null) return;
            Destroy(CurrentDraggable.gameObject);
            CurrentDraggable = null;
        }
    }
}