using System;
using System.Collections.Generic;
using Kuantech.Utils;

namespace Kuantech.Core.UI
{
    [Serializable]
    public struct PhasePanelsEntry
    {
        public UIElement PhasePanel;
        public string PhaseKey;
    }
    
    public class LevelUI : UICanvas
    {
        public List<PhasePanelsEntry> PhasePanels;
        
        //UI elements that could be accessed by other systems
        public List<UIElement> UIElements;

        private Dictionary<string, UIElement> _phasePanelsById = new Dictionary<string, UIElement>();
        public virtual void Initialize()
        {
            
        }
        
        public virtual void OnLevelSetup(Level level)
        {
            level.OnStateChangeEvent += OnLevelStateChanged;
            level.OnPhaseChangeEvent += OnLevelPhaseChanged;

            _phasePanelsById = new Dictionary<string, UIElement>();
            foreach(var panelEntry in PhasePanels)
            {
                if(panelEntry.PhasePanel == null || panelEntry.PhaseKey.IsNullOrEmpty()) continue;
                _phasePanelsById[panelEntry.PhaseKey] = panelEntry.PhasePanel;
            }
        }

        public virtual void OnPlayLevel()
        {
            Reset();
        }

        public virtual void OnLevelFail()
        {
            
        }

        public virtual void OnLevelComplete()
        {
            
        }
        
        protected virtual void OnLevelPhaseChanged(LevelPhaseChangeData phaseChangeData)
        {
            foreach (var pair in _phasePanelsById)
            {
                bool show = pair.Key == phaseChangeData.NewPhase.Key;
                if (show)
                {
                    pair.Value.Show();
                }
                else
                {
                    pair.Value.Close();
                }
            }
        }
        
        protected virtual void OnLevelStateChanged(LevelStateChangeData stateChangeData)
        {
            if (stateChangeData.NewState == LevelState.Playing)
            {
                OnPlayLevel();
            }else if (stateChangeData.NewState == LevelState.Completed)
            {
                OnLevelComplete();
            }else if (stateChangeData.NewState == LevelState.Failed)
            {
                OnLevelFail();
            }
        }

        #region Panels

        public T GetUIElementByType<T>() where T : UIElement
        {
            if (UIElements.IsNullOrEmpty()) return null;
            foreach (var uiElement in UIElements)
            {
                if (uiElement is T)
                {
                    return uiElement as T;
                }
            }

            return null;
        }

        #endregion
        public virtual void Reset()
        {
            
        }
    }
}