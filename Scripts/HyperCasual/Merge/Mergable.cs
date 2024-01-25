using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Merge
{
   
    
    [RequireComponent(typeof(Slottable))]
    public class Mergable : MonoBehaviour
    {
        public MergableTemplate MergableData;
        public int Level = 0;
        
        [Header("Slottable")] 
        public Slottable Slottable;
        
        [Header("Visuals")] 
        [SerializeField] private MergeHeadUI HeadUI;
        
        private IDropZone _dropZone;
        private Vector3 _positionBeforeDrag;
        //private Vector2Int _lastRowCol;
        
        public virtual void Initialize(MergableTemplate mergableData)
        {
            Level = 1;
            MergableData = mergableData;
            UpdateHeadUI();
        }

        public void SetLevel(int level)
        {
            Level = level;
            UpdateHeadUI();
        }
        public void Upgrade()
        {
            Level++;
            UpdateHeadUI();
        }

        public bool CanBeMergedWith(Mergable other)
        {
            if (other.MergableData.Id == MergableData.Id && other.Level == Level) return true;
            return false;
        }

        #region Visuals

        protected virtual void UpdateHeadUI()
        {
            if (HeadUI == null) return;
            HeadUI.SetText($"{Level}");
        }

        #endregion
    }
}