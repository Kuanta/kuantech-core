using System.Collections;
using Kuantech.Rpg;
using UnityEngine;

namespace Kuantech.Core
{
    public class AttackMomentumModule : ActorModule
    {
        [SerializeField] private bool DirectionalMomentum;
        [Tooltip("How much momentum is reduced when moving backwards relative to attack direction (0=no reduction, 1=zero momentum)")]
        [SerializeField] private float BackwardsMomentumFactor = 0.5f;

        private CombatModule   _combatModule;
        private AimHandler     _aimHandler;
        private MovementModule _movementModule;
        private StatsModule    _statsModule;

        private Vector3   _appliedMomentum;
        private Coroutine _clearRoutine;
        private Coroutine _rotationLockRoutine;
        private bool      _attackLockActive;

        public override void OnModulesInitialized()
        {
            base.OnModulesInitialized();
            _combatModule   = Actor.GetModule<CombatModule>();
            _aimHandler     = Actor.GetModule<AimHandler>();
            _movementModule = Actor.GetModule<MovementModule>();
            _statsModule    = Actor.GetModule<StatsModule>();

            if (_combatModule != null)
            {
                _combatModule.AttackStartedEvent   += OnAttackStarted;
                _combatModule.AlignedEvent         += OnAligned;
                _combatModule.AttackCompletedEvent += OnAttackCompleted;
            }
        }

        private void OnAttackStarted(CombatModule combat)
        {
            AttackPattern pattern = combat.GetCurrentAttackPattern();

            // If WaitRotationalAlign is true, defer locks until OnAligned fires.
            // Otherwise lock immediately so the actor stays facing the attack direction.
            if (!pattern.WaitRotationalAlign)
                ApplyAttackLocks(pattern);

            // Movement lock always applies immediately (enemy shouldn't walk during telegraph)
            if (pattern.LockMovementOnAttack && _movementModule != null)
            {
                _movementModule.Lock(this);
                _attackLockActive = true;
            }

            // Momentum force
            float magnitude = pattern.AttackMomentum.GetValue(_statsModule);
            if (magnitude <= 0f) return;

            Vector3 direction = GetMomentumDirection(combat);
            if (direction.sqrMagnitude < 0.001f) return;

            if (_clearRoutine != null) StopCoroutine(_clearRoutine);
            ClearMomentum();

            _appliedMomentum = direction * (magnitude * ComputeReductionFactor(combat));
            Actor.MotionVectorsHandler.AddForceMovementVector(_appliedMomentum);

            _clearRoutine = StartCoroutine(ClearAfter(pattern.AttackMomentumDuration));
        }

        private void OnAligned(CombatModule combat)
        {
            AttackPattern pattern = combat.GetCurrentAttackPattern();
            if (pattern.LockRotationOnAttack)
                ApplyAttackLocks(pattern);
        }

        private void ApplyAttackLocks(AttackPattern pattern)
        {
            if (!pattern.LockRotationOnAttack) return;
            if (_aimHandler == null) { Debug.LogWarning($"[AttackMomentumModule] {Actor.name}: LockRotationOnAttack=true but AimHandler not found"); return; }

            if (_rotationLockRoutine != null) StopCoroutine(_rotationLockRoutine);

            if (pattern.RotationLockDelay > 0f)
                _rotationLockRoutine = StartCoroutine(LockRotationAfter(pattern.RotationLockDelay));
            else
                ExecuteRotationLock();
        }

        private IEnumerator LockRotationAfter(float delay)
        {
            yield return new WaitForSeconds(delay);
            ExecuteRotationLock();
            _rotationLockRoutine = null;
        }

        private void ExecuteRotationLock()
        {
            if (_aimHandler == null) return;
            _aimHandler.LockRotation(this);
            _attackLockActive = true;
        }

        private void OnAttackCompleted(CombatModule combat)
        {
            ReleaseAttackLocks();
        }

        private void ReleaseAttackLocks()
        {
            if (_rotationLockRoutine != null) { StopCoroutine(_rotationLockRoutine); _rotationLockRoutine = null; }
            if (!_attackLockActive) return;
            _attackLockActive = false;
            if (_aimHandler != null)     _aimHandler.UnlockRotation(this);
            if (_movementModule != null) _movementModule.Unlock(this);
        }

        private float ComputeReductionFactor(CombatModule combat)
        {
            Vector3 moveVec = Actor.MotionVectorsHandler.GetMovementVector();
            if (moveVec.sqrMagnitude < 0.001f) return 1f;

            Vector3 attackDir = combat.GetAttackDirection();
            float dot = Vector3.Dot(attackDir.normalized, moveVec.normalized);
            return Mathf.Lerp(1f - BackwardsMomentumFactor, 1f, (dot + 1f) * 0.5f);
        }

        private Vector3 GetMomentumDirection(CombatModule combat)
        {
            Vector3 movement = Actor.MotionVectorsHandler.GetMovementVector();
            if (DirectionalMomentum && movement.sqrMagnitude > 0.001f)
                return movement.normalized;

            Vector3 dir = combat.GetAttackDirection();
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f)
            {
                dir = Actor.MotionVectorsHandler.GetTargetVector();
                dir.y = 0f;
            }
            return dir.normalized;
        }

        private IEnumerator ClearAfter(float duration)
        {
            yield return new WaitForSeconds(duration);
            ClearMomentum();
        }

        private void ClearMomentum()
        {
            if (_appliedMomentum.sqrMagnitude <= 0f) return;
            Actor.MotionVectorsHandler.RemoveForceMovementVector(_appliedMomentum);
            _appliedMomentum = Vector3.zero;
        }

        public override void ResetModule()
        {
            base.ResetModule();
            if (_clearRoutine != null)        { StopCoroutine(_clearRoutine);        _clearRoutine        = null; }
            if (_rotationLockRoutine != null) { StopCoroutine(_rotationLockRoutine); _rotationLockRoutine = null; }
            ClearMomentum();
            ReleaseAttackLocks();
        }

        public override void Cleanup()
        {
            base.Cleanup();
            if (_combatModule != null)
            {
                _combatModule.AttackStartedEvent   -= OnAttackStarted;
                _combatModule.AlignedEvent         -= OnAligned;
                _combatModule.AttackCompletedEvent -= OnAttackCompleted;
            }
        }
    }
}
