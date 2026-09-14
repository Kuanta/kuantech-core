using System;
using UnityEngine;

namespace Kuantech.HordeSurvival
{
    /// <summary>
    /// A condition evaluated against the wave handler's live state (budget spent, wave cleared, ...).
    /// Concrete triggers are chosen per event through the inspector [SerializeReference] picker, or
    /// resolved from JSON via Newtonsoft's TypeNameHandling ("$type") -- see ArenaData.WaveEvents.
    /// </summary>
    [Serializable]
    public abstract class WaveEventTrigger
    {
        public abstract bool IsMet(HordeWaveHandler handler);
    }

    /// <summary>
    /// The effect run when a trigger fires — spawn a boss/elite, a surge, a hazard, a cue. Chosen per event
    /// through [SerializeReference], or resolved from JSON the same way as WaveEventTrigger.
    /// </summary>
    [Serializable]
    public abstract class WaveEventAction
    {
        public abstract void Execute(HordeWaveHandler handler);
    }

    /// <summary>
    /// A scripted moment inside a wave: WHEN (Trigger) something happens and WHAT (Action) happens. Authored
    /// on the wave handler in the inspector for now. Keeping trigger and action as separate, pluggable pieces
    /// is what will let a future data layer build these from rows/JSON without new code — mix any trigger
    /// with any action.
    /// </summary>
    [Serializable]
    public class WaveEvent
    {
        [Tooltip("Which wave this fires on. -1 = every wave. Ignored when LastWaveOnly is set.")]
        public int WaveIndex = -1;
        [Tooltip("Fire only on the level's last wave (robust to the wave count changing).")]
        public bool LastWaveOnly = false;
        [Tooltip("Fire at most once per wave (the flag resets when the wave changes).")]
        public bool OneShot = true;

        [SerializeReference] public WaveEventTrigger Trigger;
        [SerializeReference] public WaveEventAction Action;

        [NonSerialized] public bool Fired;
    }
}
