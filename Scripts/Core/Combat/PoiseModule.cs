using System;
using System.Collections;
#if NETWORKING_NGO
using Unity.Netcode;
#endif
using Kuantech.Rpg;
using UnityEngine;

namespace Kuantech.Core.Combat
{
    /// <summary>
    /// Watches a Poise resource (a normal ResourceAsset, chipped by AttackPattern.AdditionalDamages entries
    /// using a poise DamageType) and triggers a stagger once it hits zero: cancels/locks the current attack,
    /// locks movement, and plays the Animator's "Poised" bool (drives the PoiseBreak/PoiseBreakLoop states
    /// already authored on EnemyAnimator/PlayerAnimator). Poise itself refills to max once the break ends,
    /// rather than regenerating continuously mid-fight.
    ///
    /// Doesn't know or care what reacts to a break -- it just fires OnPoiseBreak and lets subscribers decide
    /// (e.g. BlockModule cancelling a raised guard). By default Poise is a plain always-on stagger meter
    /// (every hit chips it, a Zombie should stay this way); setting ReceivePoiseDamageWhileNotBlocking to
    /// false turns it into a guard-stability meter instead, where poise damage only lands while the actor is
    /// actively blocking (requires a BlockModule) -- a break then only ever happens from over-committing to
    /// a guard, never from an unblocked hit, which just deals its normal health damage and hit-react instead.
    /// </summary>
    public class PoiseModule : ActorModule
    {
        [Header("Poise")]
        public ResourceAsset PoiseResourceAsset;
        [Tooltip("How long the actor is staggered (can't attack or move) after poise breaks.")]
        public float PoiseBreakDuration = 1.5f;
        [Tooltip("If false, poise damage is ignored entirely while not actively blocking (requires a " +
                 "BlockModule) -- Poise becomes a guard-stability meter instead of a general stagger meter. " +
                 "Leave true for actors that should always be staggerable regardless of blocking (Zombies).")]
        public bool ReceivePoiseDamageWhileNotBlocking = true;

        private HealthcareModule _healthcareModule;
        private CombatModule _combatModule;
        private MovementModule _movementModule;
        private AnimationModule _animationModule;
        private BlockModule _blockModule;

        private bool _broken;
        private Coroutine _breakRoutine;

        /// <summary>Fired the instant poise breaks, on every peer alongside the lock/animation it triggers
        /// locally -- lets anything react (BlockModule, FX, ...) without this module needing to know what.</summary>
        public event Action OnPoiseBreak;

        public override void OnModulesInitialized()
        {
            base.OnModulesInitialized();
            _healthcareModule = Actor.GetModule<HealthcareModule>();
            _combatModule = Actor.GetModule<CombatModule>();
            _movementModule = Actor.GetModule<MovementModule>();
            _animationModule = Actor.GetModule<AnimationModule>();
            _blockModule = Actor.GetModule<BlockModule>();

            if (!ReceivePoiseDamageWhileNotBlocking && _blockModule == null)
                Debug.LogWarning($"[PoiseModule] {Actor.name}: ReceivePoiseDamageWhileNotBlocking is false but there's no BlockModule on this actor -- it can never take poise damage at all.");

            if (_healthcareModule != null)
            {
                _healthcareModule.OnResourceChanged += OnResourceChanged;
                _healthcareModule.DamageInterceptor += OnDamageIntercepted;
            }
        }

        public override void Cleanup()
        {
            base.Cleanup();
            if (_healthcareModule != null)
            {
                _healthcareModule.OnResourceChanged -= OnResourceChanged;
                _healthcareModule.DamageInterceptor -= OnDamageIntercepted;
            }
        }

        public bool IsBroken() => _broken;

        // Guard-stability gate -- see class doc. Only ever touches damage entries that actually affect
        // PoiseResourceAsset; health damage (and any other resource) passes through untouched.
        private DamageInfo OnDamageIntercepted(DamageInfo damageInfo, HitInfo hitInfo)
        {
            if (ReceivePoiseDamageWhileNotBlocking) return damageInfo; // no gating -- plain stagger meter
            if (PoiseResourceAsset == null || damageInfo.DamageType == null) return damageInfo;
            if (damageInfo.DamageType.AffectedResource != PoiseResourceAsset) return damageInfo;
            if (_blockModule != null && _blockModule.IsBlocking()) return damageInfo; // actively guarding -- let it chip away normally

            damageInfo.SetDamage(0f);
            return damageInfo;
        }

        // OnResourceChanged also fires on clients (ObserverSyncResource_Rpc applies the synced value through
        // the same setter) -- only the server gets to decide a break happened, then broadcasts it below.
        private void OnResourceChanged(ResourceAsset resource)
        {
            if (!IsServerInitialized || _broken) return;
            if (PoiseResourceAsset == null || resource != PoiseResourceAsset) return;
            if (_healthcareModule.GetCurrentResource(PoiseResourceAsset) <= 0f)
                TriggerPoiseBreak();
        }

        private void TriggerPoiseBreak()
        {
            _broken = true;
            if (IsSpawned) ObserversPoiseBreak_Rpc(true);
            else ExecutePoiseBreakStart();

            if (_breakRoutine != null) StopCoroutine(_breakRoutine);
            _breakRoutine = StartCoroutine(PoiseBreakRoutine());
        }

        private IEnumerator PoiseBreakRoutine()
        {
            yield return new WaitForSeconds(PoiseBreakDuration);
            EndPoiseBreak();
        }

        private void EndPoiseBreak()
        {
            _broken = false;
            _breakRoutine = null;
            if (_healthcareModule != null && PoiseResourceAsset != null)
                _healthcareModule.RefreshResource(PoiseResourceAsset);

            if (IsSpawned) ObserversPoiseBreak_Rpc(false);
            else ExecutePoiseBreakEnd();
        }

        private void ExecutePoiseBreakStart()
        {
            _combatModule?.LockAttack(this);
            _movementModule?.Lock(this);
            _animationModule?.SetPoised(true);
            OnPoiseBreak?.Invoke();
        }

        private void ExecutePoiseBreakEnd()
        {
            _combatModule?.UnlockAttack(this);
            _movementModule?.Unlock(this);
            _animationModule?.SetPoised(false);
        }

#if NETWORKING_NGO
        [Rpc(SendTo.Everyone)]
#endif
        private void ObserversPoiseBreak_Rpc(bool broken)
        {
            if (broken) ExecutePoiseBreakStart();
            else ExecutePoiseBreakEnd();
        }
    }
}
