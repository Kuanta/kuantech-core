#if NETWORKING_NGO
using Unity.Netcode;
#endif
using UnityEngine;

namespace Kuantech.Core.Combat
{
    /// <summary>
    /// Hold-to-block: input-driven (unlike PoiseModule, which is server-decided), so it follows the same
    /// owner-acts-locally + server-authoritative-relay shape as MovementModule.Dash for zero-lag feedback.
    /// Reduces incoming damage via HealthcareModule.DamageInterceptor (which also covers a hit's
    /// AdditionalDamages entries, so a blocked hit's poise damage is reduced too, for free).
    ///
    /// Doesn't lock attacking -- pressing Attack while blocking still goes through CombatModule.Attack()
    /// normally, it just swings BashPattern instead of the weapon's usual combo (see
    /// CombatModule.SetAttackPatternOverride, set/cleared here in lockstep with the block itself).
    /// </summary>
    public class BlockModule : ActorModule
    {
        [Header("Block")]
        [Range(0f, 1f)] public float DamageReductionPercent = 0.7f;
        [Tooltip("Movement speed multiplier while blocking (1 = no slow, 0 = rooted).")]
        [Range(0f, 1f)] public float MovementSpeedMultiplier = 0.5f;
        [Tooltip("Total frontal arc (degrees) a hit must come from to actually be blocked -- a hit from " +
                 "outside this, e.g. from behind, ignores the raised guard entirely.")]
        [Range(0f, 360f)] public float BlockArcDegrees = 120f;

        [Header("Bash")]
        [Tooltip("What CombatModule.Attack() swings while blocking, instead of the weapon's normal combo. " +
                 "Leave unset for a weapon with no bash.")]

        public AttackPatternAsset DefaultBashPattern;
        public AttackPattern BashPattern;

        private CombatModule _combatModule;
        private MovementModule _movementModule;
        private HealthcareModule _healthcareModule;
        private AnimationModule _animationModule;
        private PoiseModule _poiseModule;

        private bool _blocking;

        public bool IsBlocking() => _blocking;

        public override void OnModulesInitialized()
        {
            base.OnModulesInitialized();
            _combatModule = Actor.GetModule<CombatModule>();
            _movementModule = Actor.GetModule<MovementModule>();
            _healthcareModule = Actor.GetModule<HealthcareModule>();
            _animationModule = Actor.GetModule<AnimationModule>();
            _poiseModule = Actor.GetModule<PoiseModule>();

            if (_healthcareModule != null) _healthcareModule.DamageInterceptor += OnDamageIntercepted;
            if (_poiseModule != null) _poiseModule.OnPoiseBreak += OnPoiseBreak;
            BashPattern = DefaultBashPattern.GetAttackPattern();
        }

        public override void Cleanup()
        {
            base.Cleanup();
            if (_healthcareModule != null) _healthcareModule.DamageInterceptor -= OnDamageIntercepted;
            if (_poiseModule != null) _poiseModule.OnPoiseBreak -= OnPoiseBreak;
        }

        // A heavy enough hit knocks a raised guard down too -- PoiseModule doesn't know or care that this
        // is what happens, it just fires OnPoiseBreak; this is the one place that decides "and also cancel
        // the block". Runs identically on every peer, same as PoiseModule's own break handling (see
        // CancelBlock's doc below).
        private void OnPoiseBreak() => CancelBlock();

        private DamageInfo OnDamageIntercepted(DamageInfo damageInfo, HitInfo hitInfo)
        {
            if (!_blocking || !IsWithinBlockArc(hitInfo.HitDirection)) return damageInfo;

            damageInfo.SetDamage(damageInfo.GetDamage() * (1f - DamageReductionPercent));

            // This only ever runs server-side (DamageInterceptor fires from inside HealthcareModule.OnHit's
            // IsServer block), so the "clang" cue needs its own broadcast to reach every peer -- same reason
            // CombatModule's own melee hit effect has NotifyMeleeHit_Rpc alongside the local instant one.
            if (IsSpawned) ObserversBlockedHit_Rpc();
            else PlayBlockedHitEffect();

            return damageInfo;
        }

        /// <summary>
        /// HitDirection points FROM the attacker TOWARD this actor (see CombatModule.GetAttackDirection),
        /// so the direction back to the attacker is its negation -- same convention AnimationModule's own
        /// GetHitDirectionIndex uses for picking a hit-reaction clip.
        /// </summary>
        private bool IsWithinBlockArc(Vector3 hitDirection)
        {
            if (hitDirection.sqrMagnitude < 0.0001f) return true; // no direction info -- don't penalize
            Vector3 directionToAttacker = -hitDirection;
            float angleFromForward = Vector3.Angle(Actor.transform.forward, directionToAttacker);
            return angleFromForward <= BlockArcDegrees * 0.5f;
        }

        private void PlayBlockedHitEffect()
        {
            _combatModule?.GetActiveWeapon()?.BlockedHitEffect.PlayEffectAtPosition(Actor.transform.position, Actor.transform.rotation);
        }

#if NETWORKING_NGO
        [Rpc(SendTo.Everyone)]
        private void ObserversBlockedHit_Rpc()
        {
            PlayBlockedHitEffect();
        }
#endif

        public void StartBlock()
        {
            if (_blocking) return;
            if (IsServer)
            {
                ExecuteStartBlock();
                if (IsSpawned) ObserversStartBlock_Rpc();
            }
            else if (IsOwner)
            {
                ExecuteStartBlock();
                if (IsSpawned) ServerStartBlock_Rpc();
            }
        }

        public void EndBlock()
        {
            if (!_blocking) return;
            if (IsServer)
            {
                ExecuteEndBlock();
                if (IsSpawned) ObserversEndBlock_Rpc();
            }
            else if (IsOwner)
            {
                ExecuteEndBlock();
                if (IsSpawned) ServerEndBlock_Rpc();
            }
        }

        /// <summary>
        /// Poise breaking through a raised guard should knock it down -- reached via OnPoiseBreak, fired
        /// from inside PoiseModule's own SendTo.Everyone broadcast handler, which already runs identically
        /// on every peer. Calls ExecuteEndBlock directly rather than the public EndBlock() -- that one gates
        /// on IsServer/IsOwner to decide whether TO dispatch an RPC, which would leave a remote observer's
        /// local mirrored state (and Animator "Blocking" bool) stuck on, since a plain observer is neither.
        /// </summary>
        public void CancelBlock()
        {
            if (!_blocking) return;
            ExecuteEndBlock();
        }

        private void ExecuteStartBlock()
        {
            _blocking = true;
            _combatModule?.SetAttackPatternOverride(BashPattern);
            _movementModule?.SetSpeedMultiplier(MovementSpeedMultiplier);
            _animationModule?.SetBlocking(true);
            _combatModule?.GetActiveWeapon()?.BlockStartEffect.PlayEffect();
        }

        private void ExecuteEndBlock()
        {
            _blocking = false;
            _combatModule?.SetAttackPatternOverride(null);
            _movementModule?.SetSpeedMultiplier(1f);
            _animationModule?.SetBlocking(false);
            _combatModule?.GetActiveWeapon()?.BlockEndEffect.PlayEffect();
        }

#if NETWORKING_NGO
        [Rpc(SendTo.Server)]
        private void ServerStartBlock_Rpc()
        {
            ExecuteStartBlock();
            ObserversStartBlock_Rpc();
        }

        [Rpc(SendTo.Server)]
        private void ServerEndBlock_Rpc()
        {
            ExecuteEndBlock();
            ObserversEndBlock_Rpc();
        }

        [Rpc(SendTo.NotOwner)]
        private void ObserversStartBlock_Rpc()
        {
            ExecuteStartBlock();
        }

        [Rpc(SendTo.NotOwner)]
        private void ObserversEndBlock_Rpc()
        {
            ExecuteEndBlock();
        }
#endif
    }
}
