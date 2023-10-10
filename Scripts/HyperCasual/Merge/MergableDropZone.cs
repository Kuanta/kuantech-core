using System;
using System.Collections.Generic;
using System.Linq;
using Kuantech.Core;
using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.Merge
{
    public struct SlotChangeData
    {
        public IDropZone PreviousZone;
        public IDropZone NewZone;
        public Slottable slottable;
        public Vector2Int rowCol;
    }
    [RequireComponent(typeof(BoxCollider))]
    public class MergableDropZone : MonoBehaviour, IDropZone
    {
        [SerializeField] private int RowCount;
        [SerializeField] private int ColumnCount;
        [SerializeField] private float cellSize = 2f;
        [SerializeField] private BoxCollider BoxCollider;

        private float Height;
        private float Width;
        
        [Header("Highlight")]
        [SerializeField] private GameObject HighlightObject;
        [SerializeField] private Renderer HighlightObjectRenderer;
        [SerializeField] private float HighlightHeight = 0.5f;
        [SerializeField] private Color HighlightAvailableColor;
        [SerializeField] private Color HighlightUnavailableColor;
        
        //Matrix of elements
        private Slottable[,] _slottedObjects;
        private MergeManager _mergeManager;
        
        //Events
        public EventHandler<SlotChangeData> OnSlotEvent;
        public EventHandler<SlotChangeData> OnClearSlotEvent;

        public void Initialize(MergeManager mergeManager)
        {
            Height = cellSize * RowCount;
            Width = cellSize * ColumnCount;
            if (BoxCollider == null) BoxCollider = GetComponent<BoxCollider>();
            BoxCollider.size = new Vector3(Width, BoxCollider.size.y, Height);
            _slottedObjects = new Slottable[RowCount, ColumnCount];
            for (int row = 0; row < RowCount; row++)
            {
                for (int col = 0; col < ColumnCount; col++)
                {
                    // Create a new Slottable object for each slot
                    _slottedObjects[row, col] = null;
                }
            }

            _mergeManager = mergeManager;
        }

        public void LoadData(SlottableZoneState data)
        {
            if (data == null) return;
            MergeManager mergeManager = (GameManager.Instance).GetSubManagerByType<MergeManager>() as MergeManager;
            List<int> keys = data.Slots.Keys.ToList();
            for(int i=0;i<keys.Count;++i)
            {
                SlotState slotState = data.Slots[keys[i]];
                int index = keys[i];
                Mergable mergable = mergeManager.CreateMergable(slotState.SlottableId, slotState.SlottableLevel);
                if(mergable == null) continue; //todo(refactor): Same funciton exists in Tank. 
                Vector2Int rowCol = GetRowColFromFlattened(index);
                SlotObject(mergable.Slottable, rowCol.x, rowCol.y, false);
            }
        }
        /// <summary>
        /// Toggles all slottables on the bench
        /// </summary>
        /// <param name="toggle"></param>
        public void ToggleSlottables(bool toggle)
        {
            if (_slottedObjects == null) return;
            for (int row = 0; row < RowCount; row++)
            {
                for (int col = 0; col < ColumnCount; col++)
                {
                    // Create a new Slottable object for each slot
                    if(_slottedObjects[row, col] == null) continue;
                    _slottedObjects[row, col].gameObject.SetActive(toggle);
                }
            }
        }

        public void ApplyFunctionToSlottables(UnityAction<Slottable> handler)
        {
            if (_slottedObjects == null) return;
            for (int row = 0; row < RowCount; row++)
            {
                for (int col = 0; col < ColumnCount; col++)
                {
                    // Create a new Slottable object for each slot
                    if(_slottedObjects[row, col] == null) continue;
                    handler?.Invoke( _slottedObjects[row, col]);
                }
            }
        }


        public void Highlight(Slottable dragged)
        {
            //todo: Stop highlight if occupied
            if (HighlightObject == null) return;
            Vector2Int rowColIndices = GetRowColIndices(dragged.gameObject);
            HighlightObject.SetActive(true);
            Vector3 highlightLocalPos = GetLocalCellPosition(rowColIndices);
            highlightLocalPos.y = HighlightHeight;
            HighlightObject.transform.localPosition = highlightLocalPos;
            
            //Check if there is an object in that spot
            Slottable existing = GetSlottedObject(rowColIndices.x, rowColIndices.y);

            if (existing == dragged || existing == null)
            {
                HighlightObjectRenderer.material.SetColor("_MainColor", HighlightAvailableColor);
            }
            else
            {
                bool mergable = _mergeManager.CanBeMerged(
                    dragged.GetComponent<Mergable>(),
                    existing.GetComponent<Mergable>());
                HighlightObjectRenderer.material.SetColor("_MainColor", mergable ? HighlightAvailableColor : HighlightUnavailableColor);
            }
        }

        public void CancelHighlight()
        {
            if(HighlightObject != null) HighlightObject.SetActive(false);
        }

        
        #region Slotting

        public bool GetAvailableSlot(out Vector2Int availableSlot)
        {
            availableSlot = Vector2Int.one * -1;
            for (int row = RowCount - 1; row >= 0; row--)
            {
                //Try center column first
                int centerColumn = ColumnCount / 2;
                int offset = 0;
                availableSlot.x = row;
                while (offset <= Mathf.FloorToInt(ColumnCount / (float)2))
                {
                    int leftColumn = centerColumn - offset;
                    int rightColumn = centerColumn + offset;

                    if (_slottedObjects[row, leftColumn] == null)
                    {
                        availableSlot.y = leftColumn;
                        return true;
                    }


                    if (_slottedObjects[row, rightColumn] == null)
                    {
                        availableSlot.y = rightColumn;
                        return true;
                    }
                    offset++;
                }
            }

            return false;
        }
        public Slottable GetSlottedObject(int row, int col)
        {
            row = Mathf.Clamp(row, 0, RowCount - 1);
            col = Mathf.Clamp(col, 0, ColumnCount - 1);
            return  _slottedObjects[row, col];
        }
        
        /// <summary>
        /// Slots a slottable if the given row and col is available
        /// </summary>
        /// <param name="slottable"></param>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        public bool SlotObject(Slottable slottable, int row, int col, bool triggerEvent = true)
        {
            if (slottable == null) return false;
            IDropZone previosZone = slottable.slottedZone;
            Slottable existing = GetSlottedObject(row, col);
            if (existing != null) return false;
            
            //Check if this is previously positioned
            if (slottable.slottedZone != null)
            {
                slottable.ClearSlot();
            }
            
            GetSlottedObject(slottable.Row, slottable.Column);
            slottable.Row = row;
            slottable.Column = col;
            slottable.slottedZone = this;
            slottable.transform.SetParent(transform);
            slottable.transform.localPosition = GetLocalCellPosition(new Vector2(row, col));
            slottable.transform.localRotation = Quaternion.identity;
            slottable.transform.localScale = Vector3.one;
            //slottable.transform.position = GetGlobalCellPosition(new Vector2(row, col));
            _slottedObjects[row, col] = slottable;
            if (triggerEvent)
            {
                OnSlotEvent?.Invoke(this, new SlotChangeData()
                {
                    slottable = slottable,
                    PreviousZone = previosZone,
                    NewZone = this,
                });
            }

            return true;
        }

        public void ClearSlot(int row, int col)
        {
            OnClearSlotEvent?.Invoke(this, new SlotChangeData()
            {
                slottable = _slottedObjects[row, col],
                rowCol =  new Vector2Int(row, col),
                PreviousZone = this,
                NewZone = null,
            });
            _slottedObjects[row, col] = null;
        }
        #endregion
        
        #region Getters
        
        public Vector2Int GetRowColIndices(GameObject dropped)
        {
            Vector3 localPosition = transform.InverseTransformPoint(dropped.transform.position);
            int row = Mathf.FloorToInt(Mathf.Clamp(localPosition.z + Height*0.5f, 0, Height)/cellSize);
            int col = Mathf.FloorToInt(Mathf.Clamp(localPosition.x + Width*0.5f, 0, Width)/cellSize);
            return new Vector2Int(row, col);
        }
        
        /// <summary>
        /// Gets the local position given row and col indices
        /// </summary>
        /// <param name="index">row and col indices in the form of (row, col)</param>
        /// <returns></returns>
        public Vector3 GetLocalCellPosition(Vector2 index)
        {
            float x = (cellSize * index.y + cellSize * 0.5f) - Width * 0.5f;
            float z = (cellSize * index.x + cellSize * 0.5f) - Height * 0.5f;
            return new Vector3(x, BoxCollider.size.y*0.5f + BoxCollider.center.y, z);
        }

        public Vector3 GetGlobalCellPosition(Vector2 index)
        {
            Vector3 localPosition = GetLocalCellPosition(index);
            return transform.TransformPoint(localPosition);
        }
        
        public int GetRowCount()
        {
            return RowCount;
        }

        public int GetColCount()
        {
            return ColumnCount;
        }
        public Slottable GetSlottableAtIndex(int row, int col)
        {
            //Don't check arguments? Let it raise error?
            return _slottedObjects[row, col];
        }
        
        #endregion

        #region Drag Events

        public void OnDragStart()
        {
        }

        public void OnDrag()
        {
        }

        public void OnDragEnd()
        {
        }

        public bool OnDrop(IDraggable dropped)
        {
            Mergable droppedMergable = ((Mergable) dropped);
            if (droppedMergable == null) return false;
            if (droppedMergable.Slottable == null) return false;
            Vector2Int rowColIndices = GetRowColIndices(droppedMergable.gameObject);
            
            //Is there any object here?
            Slottable existing = GetSlottedObject(rowColIndices.x, rowColIndices.y);
            if (existing == null)
            {
                return SlotObject(droppedMergable.Slottable, (int)rowColIndices.x, (int)rowColIndices.y);
            }

            Mergable existingMergable = existing.GetComponent<Mergable>();
            if (!_mergeManager.CanBeMerged(droppedMergable, existingMergable))
            {
                return false;
            }
            droppedMergable.Slottable.ClearSlot();
            existing.ClearSlot();
            Mergable newMergable = _mergeManager.Merge(droppedMergable, existingMergable);
            SlotObject(newMergable.Slottable, rowColIndices.x, rowColIndices.y );
            return true;

        }

        #endregion

        #region Utilities

        public int GetFlattenedIndex(int row, int col)
        {
            return row * ColumnCount + col;
        }

        public Vector2Int GetRowColFromFlattened(int flattened)
        {
            int row = Mathf.FloorToInt(flattened / (float)ColumnCount);
            int col = flattened - row * ColumnCount;
            return new Vector2Int(row, col);
        }
        #endregion
      
    }
}