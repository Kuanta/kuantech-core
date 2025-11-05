using System.Collections.Generic;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Core.FX
{
    public class HitStopFxBehaviour : FxBehaviour
    {
        
        [Header("Default HitStops")]
        public CombatManager.HitStopEntry DefaultHitStop;
        


        [Header("HitStops By Combo")]
        public List<CombatManager.HitStopEntry> HitStopsByCombo;
        
        protected override void OnFxStarted(Effect parentFx)
        {
            int comboIndex = parentFx.EffectPlaySettings.ComboIndex;
            CombatManager cm = CombatManager.GetContext<CombatManager>();
            if (cm == null) return;
            if (comboIndex < 0 || HitStopsByCombo.IsNullOrEmpty())
            {
                if (DefaultHitStop.Duration == 0) return;
                cm.PushHitStop(DefaultHitStop.TimeScale, DefaultHitStop.Duration, DefaultHitStop.PushType, DefaultHitStop.Priority);
            }
            else
            {
                comboIndex %= HitStopsByCombo.Count;
                CombatManager.HitStopEntry hitStop = HitStopsByCombo[comboIndex];
                if(hitStop.Duration == 0) return;
                cm.PushHitStop(hitStop.TimeScale, hitStop.Duration, hitStop.PushType, hitStop.Priority);
            }
         
        }
    }
}