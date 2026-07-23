using System.Collections.Generic;
using System.Text;
using Kuantech.Core;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.Midcore
{
    /// <summary>
    /// A talent-tree node. What it DOES is composed from a list of TraitApplier effects (a stat bonus, a
    /// granted passive, ...) rather than baked in — so one node can stack effects and new effect kinds are
    /// added by writing a TraitApplier subclass, not by subclassing this ScriptableObject.
    /// </summary>
    [CreateAssetMenu(fileName = "TraitUpgradeProgressable", menuName = "Kuantech/Midcore/Trait Upgrade Progressable")]
    public class TraitUpgradeProgressable : ProgressableDataAsset
    {
        [Tooltip("Effects applied to the actor when this trait is active, each scaled by the current rank.")]
        [SerializeReference] [SubclassSelector]
        public List<TraitApplier> Appliers = new();

        public virtual bool CanBeAppliedToActor(Actor actor)
        {
            return true;
        }

        /// <summary>Runs every effect on the actor at this trait's current rank.</summary>
        public void ApplyToActor(Actor actor)
        {
            int rank = GetUpgradeRank();
            if (rank < 0) return;
            if (actor == null || !CanBeAppliedToActor(actor)) return;
            if (Appliers == null) return;

            foreach (var applier in Appliers)
                applier?.ApplyToActor(actor, rank);
        }

        public int GetUpgradeRank()
        {
            return ProgressionManager.GetCurrentRank(this);
        }

        public override string GetName() => GetName(GetUpgradeRank());

        public string GetName(int rank)
        {
            string effects = BuildEffectsText(rank);
            return string.IsNullOrEmpty(effects) ? Name : $"{Name} {effects}";
        }

        // Joins the non-empty effect descriptions for the given rank, e.g. "+5 +2%".
        private string BuildEffectsText(int rank)
        {
            if (Appliers == null) return string.Empty;
            StringBuilder sb = new StringBuilder();
            foreach (var applier in Appliers)
            {
                if (applier == null) continue;
                string desc = applier.GetDescription(rank);
                if (string.IsNullOrEmpty(desc)) continue;
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(desc);
            }
            return sb.ToString();
        }
    }
}
