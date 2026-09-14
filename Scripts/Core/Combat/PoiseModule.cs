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
    /// </summary>
    public class PoiseModule : ActorModule
    {
        [Header("Poise")]
        public ResourceAsset PoiseResourceAsset;
        [Tooltip("How long the actor is staggered (can't attack or move) after poise breaks.")]
        public float PoiseBreakDuration = 1.5f;

        private HealthcareModule _healthcareModule;
        private CombatModule _combatModule;
        private MovementModule _movementModule;
        private AnimationModule _animationModule;
        private BlockModule _blockModule;

        private bool _broken;
        private Coroutine _breakRoutine;

        public override void OnModulesInitialized()
        {
            base.OnModulesInitialized();
            _healthcareModule = Actor.GetModule<HealthcareModule>();
            _combatModule = Actor.GetModule<CombatModule>();
            _movementModule = Actor.GetModule<MovementModule>();
            _animationModule = Actor.GetModule<AnimationModule>();
            _blockModule = Actor.GetModule<BlockModule>();

            if (_healthcareModule != null) _healthcareModule.OnResourceChanged += OnResourceChanged;
        }

        public override void Cleanup()
        {
            base.Cleanup();
            if (_healthcareModule != null) _healthcareModule.OnResourceChanged -= OnResourceChanged;
        }

        public bool IsBroken() => _broken;

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
            _blockModule?.CancelBlock(); // a heavy enough hit knocks the guard down too
            _combatModule?.LockAttack(this);
            _movementModule?.Lock(this);
            _animationModule?.SetPoised(true);
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
