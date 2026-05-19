using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Core.UI;

namespace Kuantech.Rpg.UI
{
    public class AttributeShowcasePanel : UIElement, IActorBasedPanel
    {
        public List<AttributeIndicator> AttributeIndicator;

        private StatsModule _statsModule;
        private Actor _actor;

        public override void Initialize()
        {
            if(Initialized) return;
            base.Initialize();

            foreach(var attrIndicator in AttributeIndicator)
            {
                attrIndicator.Initialize();
            }
        }

        public void SubscribeToStats(StatsModule statsModule)
        {
            if (_statsModule != null)
                _statsModule.OnAttributeChanged -= OnStatsChanged;

            _statsModule = statsModule;

            if (_statsModule != null)
                _statsModule.OnAttributeChanged += OnStatsChanged;

            OnStatsChanged();
        }

        private void OnStatsChanged()
        {
            foreach(var attrIndicator in AttributeIndicator)
                attrIndicator.UpdateValue(_statsModule);
        }

        public void SetPlayer(Actor actor)
        {
            _actor = actor;
            SubscribeToStats(actor.GetModule<StatsModule>());
        }
    }
}