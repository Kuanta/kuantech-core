using System;
using DTT.Utils.Extensions;
using Kuantech.Core;
using Kuantech.Utils;
using UnityEngine;

namespace Kuantech.HordeSurvival
{
    public class HordeEnemyModule : ActorModule, IUpdateRateProvider
    {
        public float ApproachDistance = 2f; //Data dependent, see HordeEnemyBlueprintComponent

        [Tooltip("How long, in seconds, this enemy may sit off-screen — after having been seen on-screen at " +
                 "least once — before it asks to be moved to a fresh position. This is the single most " +
                 "important number for the 'surrounded' feel: an enemy that trails the player off-screen " +
                 "instead of recycling is what turns a horde into one blob following behind. A fresh spawn " +
                 "is exempt until the player has actually seen it once, so spawning off-screen (by design) " +
                 "never triggers an instant relocation.")]
        public float RelocationVisibilityTimeout = 2f; //Data dependent, see HordeEnemyBlueprintComponent

        [Header("Update Rate")]
        [Tooltip("Closer than this to the player, the enemy asks for the actor's fastest update rate.")]
        [SerializeField] private float FullRateDistance = 10f;
        [Tooltip("Past this, it asks for the slowest. In between it scales.")]
        [SerializeField] private float SlowestRateDistance = 30f;

        [Header("Attack")]
        public float AttackCooldown = 1.5f; //Data dependent, see HordeEnemyBlueprintComponent

        public enum EnemyState { Idle, Active }

        //Runtime
        // Idle = frozen (between runs, on the results screen, or a test scene before activation). Active =
        // pursue + attack. Driven externally (the run handler in-game) so the module never depends on a
        // specific manager — a test scene just calls SetState(Active). Defaults to Active so an enemy dropped
        // into a bare scene with no run handler works out of the box.
        private EnemyState _state = EnemyState.Active;

        private Actor _player;
        private SteeringMovementModule _movement;
        private CombatModule _combatModule;
        private SpawnInModule _spawnIn;

        private float _lastAttackTime;

        // Visibility tracking for relocation: false until the player has actually seen this enemy on-screen
        // once, so an off-screen spawn (by design) is never mistaken for one that needs recycling.
        private bool _hasEverBeenVisible;
        private float _lastVisibleTime;

        // Raised when the enemy was seen, then stayed off-screen too long, and wants a fresh position near
        // the player. Whoever owns spawn positioning (the wave handler) subscribes and calls Relocate — kept
        // as an event so the module never depends on a specific manager (a test scene simply leaves it
        // unsubscribed).
        public event Action<HordeEnemyModule> OnRelocationRequested;

        public EnemyState State => _state;
        public void SetState(EnemyState state) => _state = state;

        public override void OnModulesInitialized()
        {
            base.OnModulesInitialized();
            _movement = Actor.GetModule<SteeringMovementModule>();
            _combatModule = Actor.GetModule<CombatModule>();
            _spawnIn = Actor.GetModule<SpawnInModule>();
        }

        public override void OnActorStateChanged(ActorState oldState, ActorState newState)
        {
            base.OnActorStateChanged(oldState, newState);
            if (newState == ActorState.Spawned)
            {
                // Reset pooled state to Active; the spawn-in module holds movement until its beat is over.
                SetState(EnemyState.Active);
                ResetVisibilityTracking();
            }
        }

        public void SetPlayer(Actor player)
        {
            _player = player;
            Actor.MotionVectorsHandler.SetTargetObject(_player.transform);
        }
        // Plays the spawn-in (animation + effect + delay) via the shared module. Called on a real spawn and
        // on relocation, so a recycled enemy eases in instead of popping into the fight.
        public void OnSpawn(Vector3 spawnPoint)
        {
            if (_spawnIn != null) _spawnIn.Play(spawnPoint);
        }

        public override void ModuleUpdate(float deltaTime)
        {
            if (_player == null || _movement == null) return;
            // Hold still while spawning in (the spawn-in beat), while the run has frozen us (Idle), or when
            // the player is gone.
            if (_state != EnemyState.Active || (_spawnIn != null && !_spawnIn.IsReady) || !_player.IsAlive())
            {
                _movement.Stop();
                return;
            }

            // Decide the seek: head toward the player on the ground plane, but stop pushing in once within
            // range (then try to attack). The steering module folds in separation + obstacle avoidance and
            // drives the mover — this module only decides the intent.
            Vector3 seek = Vector3.zero;
            Vector3 selfLocation = Actor.GetActorLocation();
            Vector3 toPlayer = _player.GetActorLocation() - selfLocation;
            toPlayer.y = 0f;

            // Track on-screen presence, then recycle: once seen, an enemy that stays off-screen too long asks
            // for a fresh position → ask to relocate. With no subscriber (e.g. a test scene) relocation never
            // fires, so it just keeps pursuing instead of getting stuck idle.
            if (CameraManager.IsInViewport(selfLocation))
            {
                _hasEverBeenVisible = true;
                _lastVisibleTime = Time.time;
            }
            else if (_hasEverBeenVisible && Time.time - _lastVisibleTime > RelocationVisibilityTimeout && OnRelocationRequested != null)
            {
                RequestRelocation();
                return;
            }
            bool isInApproachRange = toPlayer.sqrMagnitude <= ApproachDistance * ApproachDistance;
            if (!isInApproachRange)
            {
                seek = toPlayer.normalized;
            }
            else if (CanAttack())
            {
                bool result = _combatModule.Attack(GetActionCastData());
                if (result) _lastAttackTime = Time.time;
            }

            _movement.Move(seek);
        }

        /// <summary>
        /// How urgently this enemy needs updating, from the one thing that decides it: distance to the
        /// player. Near the fight it wants every frame — that is where responsiveness is visible and where
        /// attacks land. Far away nobody can tell a chase apart at 5 Hz from one at 60, and there are far
        /// more enemies out there than in close, so that is where the saving actually is.
        /// </summary>
        public float GetUpdateIntervalFactor()
        {
            if (_player == null) return 0f;

            float distance = Vector3.Distance(_player.GetActorLocation(), Actor.GetActorLocation());
            float span = SlowestRateDistance - FullRateDistance;
            if (span <= 0f) return distance >= SlowestRateDistance ? 1f : 0f;

            return Mathf.Clamp01((distance - FullRateDistance) / span);
        }

        // Go idle + stop, then ask the position owner to move us. The handler calls Relocate synchronously,
        // which repositions us near the player and replays the spawn-in.
        private void RequestRelocation()
        {
            SetState(EnemyState.Idle);
            if (_movement != null) _movement.Stop();
            OnRelocationRequested?.Invoke(this);
        }

        /// <summary>
        /// Teleports to a fresh (obstacle-free) position and replays the spawn-in (anim + effect + become-
        /// active delay), so a recycled enemy reads as a brand-new spawn. Called by the position owner.
        /// </summary>
        public void Relocate(Vector3 position)
        {
            Actor.transform.position = position;
            // RequestRelocation left us Idle; nothing else flips it back since this isn't a full
            // despawn/respawn through the actor lifecycle (OnActorStateChanged never fires here).
            SetState(EnemyState.Active);
            ResetVisibilityTracking();
            OnSpawn(position);
        }

        // Shared by a real spawn and a relocation: an enemy earns relocation eligibility only after the
        // player has actually seen it, so this always starts that clock over.
        private void ResetVisibilityTracking()
        {
            _hasEverBeenVisible = false;
            _lastVisibleTime = Time.time;
        }
    
        private bool CanAttack()
        {
            if(Time.time - _lastAttackTime < AttackCooldown) return false;
            return _combatModule.CanAttack();
        }

        private ActionCastData GetActionCastData()
        {
            WorldPoint hitPoint = _player.GetHitPoint(Actor);
            Vector3 hitPosition = hitPoint.GetTargetPosition();
            Vector3 direction = hitPosition - Actor.GetActorLocation();
            
            return new ActionCastData
            {
                Target = _player,
                TargetPosition = hitPosition,
                Direction = direction,
            };
        }
    }
}
