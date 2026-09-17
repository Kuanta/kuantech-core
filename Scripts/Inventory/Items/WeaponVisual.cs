using System;
using System.Collections.Generic;
using Kuantech.Core;
using Kuantech.Core.FX;
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

        [Header("Attack Point")]
        [Tooltip("Where CombatModule.GetAttackPosition() fires projectiles / measures range from while this " +
                 "weapon is equipped -- e.g. a bow's arrow-nock point. Registered into the owner's " +
                 "ActorSlotsHandler on equip, removed on unequip. Leave unset for a weapon that doesn't need " +
                 "a specific muzzle (CombatModule falls back to whatever slot -- or actor root -- was already there).")]
        public Transform AttackPoint;
        [Tooltip("Slot name AttackPoint registers under -- must match the AttackPattern's own " +
                 "AttackPointSlotName for this to actually be picked up.")]
        public string AttackPointSlot = "AttackPoint";

        [Header("Environment Clang")]
        [Tooltip("Layers considered solid environment (walls, pillars, ...) for the clang effect below -- " +
                 "separate from SweepLayers, which is only actors/hittables. Leave unset to skip this check.")]

        public EffectPlayer HitEffect;

        [Header("Block")]
        [Tooltip("Played once when block is raised with this item equipped.")]
        public EffectPlayer BlockStartEffect;
        [Tooltip("Played once when block is lowered with this item equipped.")]
        public EffectPlayer BlockEndEffect;
        [Tooltip("Played when a hit actually lands while blocking with this item (the 'clang').")]
        public EffectPlayer BlockedHitEffect;

        /// <summary>Fired once per IHittable the first time it's touched during the current sweep (never
        /// twice for the same target within one BeginSweep/StopSweep window).</summary>
        public event Action<IHittable, Vector3> HitDetected;

        private bool _sweeping;
        private bool _environmentHitThisSwing;
        private readonly HashSet<Collider> _hitThisSwing = new HashSet<Collider>();
        private CombatModule _combatModule;
        private ActorSlotsHandler _slotsHandler;

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

            if (AttackPoint != null)
            {
                _slotsHandler = owner != null ? owner.GetModule<ActorSlotsHandler>() : null;
                _slotsHandler?.RegisterSlot(AttackPointSlot, AttackPoint);
            }
        }

        public override void OnUnequipped()
        {
            base.OnUnequipped();
            StopSweep();
            _combatModule?.SetActiveWeapon(null);
            _combatModule = null;

            if (AttackPoint != null && _slotsHandler != null)
            {
                _slotsHandler.UnregisterSlot(AttackPointSlot, AttackPoint);
                _slotsHandler = null;
            }
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
            // Only the owning client's weapon actually sweeps -- the server doesn't mirror every client's
            // swing animation, so its own copy of a remote player's weapon has no accurate StartSweep/
            // EndSweep positions to query with. See CombatModule.OnWeaponHitDetected for the report-to-
            // server side of this (owner detects, server decides whether it counts).
            if (_combatModule != null && !_combatModule.IsOwner) return;
            DoSweepStep();
        }

        private void DoSweepStep()
        {
            Collider[] hits = UnityEngine.Physics.OverlapCapsule(StartSweep.position, EndSweep.position, SweepRadius, SweepLayers);
            if (hits.Length == 0) return;
            Actor owner = ParentItem?.GetOwner();
            foreach (var hit in hits)
            {
                if(hit.gameObject == owner.gameObject) continue;
                if (_hitThisSwing.Contains(hit)) continue;

                _hitThisSwing.Add(hit);
                Vector3 midPoint = (StartSweep.position + EndSweep.position) * 0.5f;
                Vector3 contactPoint = hit.ClosestPoint(midPoint);
                HitEffect.PlayEffectAtPosition(contactPoint, Quaternion.identity);

                IHittable hittable = hit.GetComponentInParent<IHittable>();
                if (hittable == null || !hittable.CanBeHit())
                {
                    HitDetected?.Invoke(null, contactPoint);
                }
                else
                {
                    HitDetected?.Invoke(hittable, contactPoint);
                }

            }
        }

    }
}
