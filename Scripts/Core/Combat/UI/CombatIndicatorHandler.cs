using System;
using System.Collections.Generic;
using Kuantech.Rpg;
using Kuantech.Utils;

namespace Kuantech.Core.Combat
{
    public class CombatIndicatorHandler : ActorModule
    {
        [Serializable]
        public struct CombatIndicatorTypeData
        {
            public AttackTypes AttackType;
            public CombatIndicator Indicator;
        }

        private HashSet<CombatIndicator> ActiveIndicators = new HashSet<CombatIndicator>();

        public List<CombatIndicatorTypeData> Indicators;
        private Dictionary<AttackTypes, CombatIndicator> _indicatorsByType = new Dictionary<AttackTypes, CombatIndicator>();
        
        public override void OnModulesInitialized()
        {
            base.OnModulesInitialized();
            if(Indicators != null)
            {
                _indicatorsByType = new Dictionary<AttackTypes, CombatIndicator>();
                foreach(var indicatorData in Indicators)
                {
                    _indicatorsByType[indicatorData.AttackType] = indicatorData.Indicator;
                }
            }
            CombatModule combatModule = Actor.GetModule<CombatModule>();
            if(combatModule != null)
            {
                combatModule.AlignedEvent += OnAttackStart;
                combatModule.AttackCompletedEvent += OnAttackEnd;
            }
        }

        private void OnAttackStart(CombatModule cm)
        {
            AttackPattern ap = cm.GetCurrentAttackPattern();
            ActionCastData acd = cm.GetActionCastData();
            if(ap == null) return;
            CombatIndicator indicator = GetCombatIndicator(ap.AttackType);
            if(indicator == null) return;
            StatsModule sm = Actor.GetModule<StatsModule>();
            if(sm == null) return;
            var data = new CombatIndicator.CombatIndicatorData
            {
                Duration = ap.AttackImplementationTime,
                Range = ap.Range.GetValue(Actor.GetModule<StatsModule>()),
                Width = ap.Width.GetValue(Actor.GetModule<StatsModule>()),
                Angle = ap.Angle.GetValue(Actor.GetModule<StatsModule>()),
                Direction = acd.Direction,
                Position = acd.TargetPosition,
                SetPosition = false,
            };
            indicator.Show(data);
            AddToActiveIndicators(indicator);
        }

        private void AddToActiveIndicators(CombatIndicator indicator)
        {
            if(ActiveIndicators == null) ActiveIndicators = new HashSet<CombatIndicator>();
            ActiveIndicators.Add(indicator);
        }

        private void OnAttackEnd(CombatModule cm)
        {
            if(ActiveIndicators.IsNullOrEmpty()) return;
            foreach(var indicator in ActiveIndicators)
            {
                indicator.EndIndicator();
            }
            ActiveIndicators.Clear();
        }

        private CombatIndicator GetCombatIndicator(AttackTypes type)
        {
            if(_indicatorsByType == null) return null;
            if(_indicatorsByType.ContainsKey(type)) return _indicatorsByType[type];
            return null;
        }

        public override void ResetModule()
        {
            base.ResetModule();
            if(_indicatorsByType != null)
            {
                foreach(var indicator in _indicatorsByType.Values)
                {
                    indicator.EndIndicator();
                }       
            }
        }

    }
}