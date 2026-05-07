using System.Collections.Generic;
using Kuantech.Rpg;
using Kuantech.Utils;

namespace Kuantech.Core.Combat
{
    public class CombatIndicatorHandler : ActorModule
    {
        public List<CombatIndicator> Indicators;
        private Dictionary<CombatIndicator.CombatIndicatorType, CombatIndicator> _indicatorsByType = new Dictionary<CombatIndicator.CombatIndicatorType, CombatIndicator>(); 
        
        public override void OnModulesInitialized()
        {
            base.OnModulesInitialized();
            if(Indicators != null)
            {
                _indicatorsByType = new Dictionary<CombatIndicator.CombatIndicatorType, CombatIndicator>();
                foreach(var indicator in Indicators)
                {
                    indicator.DespawnAfterEnd = false;
                    _indicatorsByType[indicator.Type] = indicator;
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
            if(ap == null || ap.IndicatorType == CombatIndicator.CombatIndicatorType.NONE) return;
            CombatIndicator indicator = GetCombatIndicator(ap.IndicatorType);
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
                SetPosition = false,
            };
            indicator.Show(data);
        }

        public void ShowIndicator(CombatIndicator.CombatIndicatorType type, CombatIndicator.CombatIndicatorData data)
        {
            CombatIndicator indicator = GetCombatIndicator(type);
            if (indicator == null) return;
            indicator.DespawnAfterEnd = false; //Don't despawn it
            indicator.Show(data);
        }

        private void OnAttackEnd(CombatModule cm)
        {
    
        }

        private CombatIndicator GetCombatIndicator(CombatIndicator.CombatIndicatorType type)
        {
            if(_indicatorsByType == null) return null;
            if(_indicatorsByType.ContainsKey(type)) return _indicatorsByType[type];
            return null;
        }


        public override void ResetModule()
        {
            base.ResetModule();
            if(_indicatorsByType!= null)
            {
                foreach(var indicator in _indicatorsByType.Values)
                {
                    indicator.EndIndicator();
                }       
            }
        }

    }
}