using UnityEngine;

namespace Kuantech.Core
{
    public class ActorVisualAnimationListener : MonoBehaviour {
        public ActorVisual ActorVisual;

        public void OnSweepStart()
        {
            if(ActorVisual == null || ActorVisual.ParentActor == null) return;
            CombatModule cm = ActorVisual.ParentActor.GetModule<CombatModule>();
            if(cm == null) return;
            cm.OnMeleeSweepStart();
        }

        public void OnSweepEnd()
        {
            if (ActorVisual == null || ActorVisual.ParentActor == null) return;
            CombatModule cm = ActorVisual.ParentActor.GetModule<CombatModule>();
            if (cm == null) return;
            cm.OnMeleeSweepEnd();
        }
    }
}