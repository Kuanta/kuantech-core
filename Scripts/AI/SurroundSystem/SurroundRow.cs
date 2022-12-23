using System;
using System.Collections.Generic;
using Kuantech.Data;
using UnityEngine;

namespace Kuantech.SurroundSystem
{
    [Serializable]
    public class SurroundRow
    {
        public int RowIndex;
        public List<SurroundSlot> LeftSlots;
        public List<SurroundSlot> RightSlots;
       
        public SurroundRow(int rowIndex)
        {
            LeftSlots = new List<SurroundSlot>();
            RightSlots = new List<SurroundSlot>();
            RowIndex = rowIndex;
        }

        public void FillSlots(int columnCount, float horizontalDistance, float verticalDistance)
        {
            Clear();
            for (float i = -(columnCount-1)*0.5f; i <= (columnCount-1)*0.5f; ++i)
            {
                SurroundSlot slot = new SurroundSlot
                {
                    Row = RowIndex,
                    Column = i,
                    VerticalDistance = verticalDistance,
                    HorizontalDistance = horizontalDistance,
                    Occupied = false
                };
                
                //if i=0, add the center to both column list
                if (i <= 0)
                {
                    LeftSlots.Add(slot);
                }
                if (i >= 0)
                {
                    RightSlots.Add(slot);
                }
            }
            //Reverse the left list  so that the center slots are lower indices
            LeftSlots.Reverse();
        }
        
        /// <summary>
        /// Calculates left and right offsets
        /// </summary>
        /// <param name="horizontalWidth"></param>
        /// <param name="normalizedHorizontalPosition"></param>
        public void SetHorizontalOffsets(float horizontalWidth, float normalizedHorizontalPosition)
        {
            float leftOffset = 0f;
            float rightOffset = 0f;
            bool foundEdge = false;
            if (normalizedHorizontalPosition < 0)
            {
                //Check Left horizontal offset
                for (int i = LeftSlots.Count - 1; i >= 0; --i)
                {
                    SurroundSlot leftSlot = LeftSlots[i];
                    if (!leftSlot.Occupied) continue;
                    if (!foundEdge)
                    {
                        float horizontalPosition = Mathf.Abs(leftSlot.GetHorizontalDistance()) + Mathf.Abs(normalizedHorizontalPosition) * horizontalWidth*0.5f;
                        leftOffset = horizontalPosition - horizontalWidth * 0.5f;
                        leftOffset = Mathf.Max(leftOffset, 0f);
                    }
                    leftSlot.LeftOffset = leftOffset;
                    leftSlot.RightOffset = rightOffset;
                    foundEdge = true;
                }

                foreach (var rightSlot in RightSlots)
                {
                    rightSlot.LeftOffset = leftOffset;
                    rightSlot.RightOffset = rightOffset;
                }
            }
            else
            {
                //Check right horizontal offset
                //Check Left horizontal offset
                for (int i = RightSlots.Count - 1; i >= 0; --i)
                {
                    SurroundSlot rightSlot = RightSlots[i];
                    if (!rightSlot.Occupied) continue;
                    if (!foundEdge)
                    {
                        float horizontalPosition = Mathf.Abs(rightSlot.GetHorizontalDistance()) + Mathf.Abs(normalizedHorizontalPosition) * horizontalWidth*0.5f;
                        rightOffset = horizontalPosition - horizontalWidth * 0.5f;
                        rightOffset = Mathf.Max(rightOffset, 0f);
                    }
                    rightSlot.LeftOffset = leftOffset;
                    rightSlot.RightOffset = rightOffset;
                    foundEdge = true;
                }
                
                foreach (var leftSlot in LeftSlots)
                {
                    leftSlot.LeftOffset = leftOffset;
                    leftSlot.RightOffset = rightOffset;
                }
            }
        }
        
        public SurroundSlot FindSuitableSlot(Enums.Directions direction)
        {
            List<SurroundSlot> primaryCandidates;
            List<SurroundSlot> secondaryCandidates;
    
            if (direction == Enums.Directions.LEFT)
            {
                primaryCandidates = LeftSlots;
                secondaryCandidates = RightSlots;
            }
            else
            {
                primaryCandidates = RightSlots;
                secondaryCandidates = LeftSlots;
            }

            for (int i = 0; i < primaryCandidates.Count; ++i)
            {
                if (!primaryCandidates[i].Occupied) return primaryCandidates[i];
                if (!secondaryCandidates[i].Occupied) return secondaryCandidates[i];
            }

            return null;
        }
        public void Clear()
        {
           LeftSlots.Clear();
           RightSlots.Clear();
        }
    }
}