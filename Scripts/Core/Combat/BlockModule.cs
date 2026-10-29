using UnityEngine;
using UnityEngine.Events;

namespace Kuantech.Core.Combat
{
    /// <summary>
    /// Hold-to-block: input-driven (unlike PoiseModule, which is authority-decided), so it follows the same
    /// controller-acts-locally + authority-relay shape as MovementModule.Dash for zero-lag feedback.
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

        // Outbound events for whatever carries a block to other peers. Nobody listens offline.
        // Requested: the controlling peer asks the authority. Broadcast: the authority tells everyone else.
        public UnityAction OnStartBlockRequested;
        public UnityAction OnEndBlockRequested;
        public UnityAction OnStartBlockBroadcast;
        public UnityAction OnEndBlockBroadcast;
        public UnityAction OnBlockedHitBroadcast;

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

            // This only ever runs on the authority (DamageInterceptor fires from inside HealthcareModule.OnHit's
            // authority block), so the "clang" cue is played here and also broadcast to reach every other peer --
            // same reason CombatModule's own melee hit effect has NotifyMeleeHit_Rpc alongside the local instant one.
            PresentBlockedHit();
            OnBlockedHitBroadcast?.Invoke();

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

        public void PresentBlockedHit()
        {
            _combatModule?.GetActiveWeapon()?.BlockedHitEffect.PlayEffectAtPosition(Actor.transform.position, Actor.transform.rotation);
        }



        public void StartBlock()
        {
            if (_blocking) return;

            // Raising the guard out of the tail of your own swing, same bargain as the dash.
            Actor.GetModule<CombatModule>()?.TryCancelAttack();

            if (Actor.HasAuthority)
            {
                ExecuteStartBlock();
                OnStartBlockBroadcast?.Invoke();
            }
            else if (Actor.IsLocallyControlled)
            {
                ExecuteStartBlock();
                OnStartBlockRequested?.Invoke();
            }
        }

        public void EndBlock()
        {
            if (!_blocking) return;
            if (Actor.HasAuthority)
            {
                ExecuteEndBlock();
                OnEndBlockBroadcast?.Invoke();
            }
            else if (Actor.IsLocallyControlled)
            {
                ExecuteEndBlock();
                OnEndBlockRequested?.Invoke();
            }
        }

        // Entry points for the network layer. The guards matter: a broadcast can reach a peer that already
        // ran the same block itself (the authority relaying a request it just executed, say), and running
        // ExecuteStartBlock twice would play its effect twice.

        /// <summary>On the authority: the controlling peer asked to raise its guard.</summary>
        public void ReceiveStartBlockRequest()
        {
            if (!Actor.HasAuthority || _blocking) return;
            ExecuteStartBlock();
            OnStartBlockBroadcast?.Invoke();
        }

        /// <summary>On the authority: the controlling peer asked to lower its guard.</summary>
        public void ReceiveEndBlockRequest()
        {
            if (!Actor.HasAuthority || !_blocking) return;
            ExecuteEndBlock();
            OnEndBlockBroadcast?.Invoke();
        }

        /// <summary>On any other peer: the authority raised the guard.</summary>
        public void ReceiveStartBlock()
        {
            if (_blocking) return;
            ExecuteStartBlock();
        }

        /// <summary>On any other peer: the authority lowered the guard.</summary>
        public void ReceiveEndBlock()
        {
            CancelBlock();
        }

        /// <summary>
        /// Poise breaking through a raised guard should knock it down -- reached via OnPoiseBreak, fired
        /// from inside PoiseModule's own break handling, which already runs identically on every peer.
        /// Calls ExecuteEndBlock directly rather than the public EndBlock() -- that one gates on authority and
        /// local control to decide whether TO dispatch a request, which would leave a remote observer's
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

    }
}
