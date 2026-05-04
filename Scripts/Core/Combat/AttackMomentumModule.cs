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

        private CombatModule _combatModule;
        private AimHandler   _aimHandler;
        private StatsModule  _statsModule;

        private Vector3   _appliedMomentum;
        private Coroutine _clearRoutine;

        public override void OnModulesInitialized()
        {
            base.OnModulesInitialized();
            _combatModule = Actor.GetModule<CombatModule>();
            _aimHandler   = Actor.GetModule<AimHandler>();
            _statsModule  = Actor.GetModule<StatsModule>();

            if (_combatModule != null)
                _combatModule.AttackStartedEvent += OnAttackStarted;
        }

        private void OnAttackStarted(CombatModule combat)
        {
            AttackPattern pattern   = combat.GetCurrentAttackPattern();
            float         magnitude = pattern.AttackMomentum.GetValue(_statsModule);
            if (magnitude <= 0f) return;

            Vector3 direction = GetMomentumDirection(combat);
            if (direction.sqrMagnitude < 0.001f) return;

            if (_clearRoutine != null) StopCoroutine(_clearRoutine);
            ClearMomentum();

            _appliedMomentum = direction * (magnitude * ComputeReductionFactor(combat));
            Actor.MotionVectorsHandler.AddForceMovementVector(_appliedMomentum);

            if (_aimHandler != null) _aimHandler.LockRotation(this);

            _clearRoutine = StartCoroutine(ClearAfter(pattern.AttackMomentumDuration));
        }

        // Reduces momentum when moving against the attack direction.
        // Returns 1 when moving forward, (1 - BackwardsMomentumFactor) when fully backward.
        private float ComputeReductionFactor(CombatModule combat)
        {
            Vector3 moveVec = Actor.MotionVectorsHandler.GetMovementVector();
            if (moveVec.sqrMagnitude < 0.001f) return 1f; // standing still — full momentum

            Vector3 attackDir = combat.GetAttackDirection();
            float dot = Vector3.Dot(attackDir.normalized, moveVec.normalized); // [-1, 1]
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
            if (_aimHandler != null) _aimHandler.UnlockRotation(this);
        }

        public override void ResetModule()
        {
            base.ResetModule();
            if (_clearRoutine != null) { StopCoroutine(_clearRoutine); _clearRoutine = null; }
            ClearMomentum();
        }

        public override void Cleanup()
        {
            base.Cleanup();
            if (_combatModule != null)
                _combatModule.AttackStartedEvent -= OnAttackStarted;
        }
    }
}
