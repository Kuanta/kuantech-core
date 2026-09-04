using System;
using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Core.Combat;
using UnityEngine;

namespace Kuantech.Inventory
{
    /// <summary>
    /// Melee-specific weapon visual. Sweeps a capsule between StartSweep and EndSweep every frame while
    /// active and reports whatever IHittable it touches via HitDetected — purely geometric, no combat
    /// decisions here. CombatModule (or whatever subscribes) decides whether a reported hit actually deals
    /// damage: server authority, faction, "did this attack even hit this weapon's owner's enemies" etc. all
    /// stay outside this class, same as every other CombatUtilities query already works in this project.
    ///
    /// StartSweep/EndSweep are plain child Transforms of the weapon mesh (not bones) — since the weapon as
    /// a whole is parented to a hand bone and animated with it, these two points move correctly through the
    /// swing for free, no extra rigging needed beyond placing them roughly at the blade's two ends.
    /// </summary>
    public class WeaponVisual : ItemVisual
    {
        [Header("Melee Sweep")]
        [Tooltip("Roughly the hilt/base end of the cutting edge. Leave both this and EndSweep unset for a non-melee item.")]
        public Transform StartSweep;
        [Tooltip("Roughly the tip end of the cutting edge.")]
        public Transform EndSweep;
        [Tooltip("Radius of the capsule swept between StartSweep and EndSweep. Generous is fine and typical — " +
                 "this never gets rendered, and hit feel comes from timing, not mesh-accurate geometry.")]
        public float SweepRadius = 0.15f;
        public LayerMask SweepLayers;

        /// <summary>Fired once per IHittable the first time it's touched during the current sweep (never
        /// twice for the same target within one BeginSweep/StopSweep window).</summary>
        public event Action<IHittable> HitDetected;

        private bool _sweeping;
        private readonly HashSet<IHittable> _hitThisSwing = new HashSet<IHittable>();
        private CombatModule _combatModule;

        public bool IsMeleeWeapon => StartSweep != null && EndSweep != null;

        // ParentItem is set by ActorVisual.EquipItemVisual right before this fires — Item.GetOwner() (via
        // ParentInventory.Owner) is how a piece of equipment reaches the Actor wearing it, without
        // CombatModule ever needing to know how weapons get equipped.
        public override void OnEquipped()
        {
            base.OnEquipped();
            Actor owner = ParentItem?.GetOwner();
            _combatModule = owner != null ? owner.GetModule<CombatModule>() : null;
            _combatModule?.SetActiveWeapon(this);
        }

        public override void OnUnequipped()
        {
            base.OnUnequipped();
            StopSweep();
            _combatModule?.SetActiveWeapon(null);
            _combatModule = null;
        }

        /// <summary>Starts the active window — call this when the swing's "blade is now cutting" moment
        /// begins (animation event, ideally; a timer for now).</summary>
        public void BeginSweep()
        {
            if (!IsMeleeWeapon) return;
            _sweeping = true;
            _hitThisSwing.Clear();
        }

        /// <summary>Ends the active window — call this when the swing's cutting moment is over.</summary>
        public void StopSweep()
        {
            _sweeping = false;
        }

        private void Update()
        {
            if (!_sweeping) return;
            DoSweepStep();
        }

        private void DoSweepStep()
        {
            List<IHittable> hits = CombatUtilities.GetHittablesInCapsule(
                StartSweep.position, EndSweep.position, SweepRadius, SweepLayers);

            foreach (IHittable hit in hits)
            {
                if (!_hitThisSwing.Add(hit)) continue; // already reported this swing
                HitDetected?.Invoke(hit);
            }
        }
    }
}
