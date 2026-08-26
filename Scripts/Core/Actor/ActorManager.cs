using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Kuantech.Core
{
    /// <summary>
    /// Drives every registered <see cref="Actor"/> from one place instead of each actor carrying its own
    /// Unity Update. Consolidating the per-MonoBehaviour Update/FixedUpdate/LateUpdate calls into a single
    /// managed loop removes the native→managed call overhead Unity pays per component — a modest but real
    /// win when hundreds of actors are alive. Actors register on Spawn and unregister on Cleanup (despawn).
    ///
    /// How often an actor updates is the actor's own business: this loop only asks
    /// <see cref="Actor.ShouldUpdate"/> and honours the answer. Keeping the policy there means an enemy can
    /// raise or lower its own rate — with distance, with state, however it likes — without this class
    /// knowing anything about enemies.
    /// </summary>
    public class ActorManager : SubManager
    {
        [Header("Debug")]
        [Tooltip("Read-only: actors updated last frame, out of how many are registered.")]
        [SerializeField] private int UpdatedActors;
        [SerializeField] private int RegisteredActors;

        private readonly HashSet<Actor> _actors = new HashSet<Actor>();
        // Iterated instead of _actors directly so an actor that registers/unregisters mid-update (e.g. one
        // that spawns a projectile actor, or despawns itself) can't mutate the collection we're looping.
        private readonly List<Actor> _iterationBuffer = new List<Actor>();
        // Actors that actually ran their Update this frame. LateUpdate follows the same set: the delta an
        // actor hands its modules is measured in ManagedUpdate, so running LateUpdate for an actor that
        // skipped Update would feed it a stale one.
        private readonly List<Actor> _updatedThisFrame = new List<Actor>();

        private bool _isBufferDirty = true;

        public override async UniTask Initialize(GameManager gameManager)
        {
            await base.Initialize(gameManager);
            _actors.Clear();
            _iterationBuffer.Clear();
            _updatedThisFrame.Clear();
            _isBufferDirty = true;
        }

        public static void RegisterActor(Actor actor)
        {
            var ctx = GetContext<ActorManager>();
            if (ctx == null || actor == null) return;
            if (ctx._actors.Add(actor))
            {
                ctx._isBufferDirty = true;
            }
        }

        public static void UnregisterActor(Actor actor)
        {
            var ctx = GetContext<ActorManager>();
            if (ctx == null || actor == null) return;
            if (ctx._actors.Remove(actor))
            {
                ctx._isBufferDirty = true;
            }
            ctx._updatedThisFrame.Remove(actor);
        }

        /// <summary>
        /// All currently-registered actors. Used by KtNetworkManager to push full state to a client that
        /// just connected, since NGO has no automatic per-object "new observer" hook the way FishNet did.
        /// </summary>
        public static IReadOnlyCollection<Actor> GetAllActors()
        {
            var ctx = GetContext<ActorManager>();
            return ctx == null ? System.Array.Empty<Actor>() : ctx._actors;
        }

        private void FixedUpdate()
        {
            if (!Initialized) return;
            // Never thinned out: a fixed step is already a fixed slice of time, and physics-facing modules
            // expect to see every one of them.
            BufferActors();
            for (int i = 0; i < _iterationBuffer.Count; i++)
            {
                Actor actor = _iterationBuffer[i];
                if (actor != null && IsUpdatable(actor)) actor.ManagedFixedUpdate();
            }
        }

        private void Update()
        {
            if (!Initialized) return;

            BufferActors();
            _updatedThisFrame.Clear();

            RegisteredActors = _iterationBuffer.Count;

            for (int i = 0; i < _iterationBuffer.Count; i++)
            {
                Actor actor = _iterationBuffer[i];
                if (actor == null || !IsUpdatable(actor) || !actor.ShouldUpdate()) continue;

                _updatedThisFrame.Add(actor);
                actor.ManagedUpdate();
            }

            UpdatedActors = _updatedThisFrame.Count;
        }

        private void LateUpdate()
        {
            if (!Initialized) return;
            for (int i = 0; i < _updatedThisFrame.Count; i++)
            {
                Actor actor = _updatedThisFrame[i];
                if (actor != null && IsUpdatable(actor)) actor.ManagedLateUpdate();
            }
        }

        /// <summary>
        /// Whether an actor's modules should still be ticked. Not spawned-only: death is a state an actor
        /// plays through, not one it stops in — the corpse still runs its death animation and, here, gets
        /// yeeted across the arena. Gating on Spawned would freeze it on the frame it died. Only an actor
        /// that has not started yet, or has already been torn down, has nothing left to do.
        /// </summary>
        private static bool IsUpdatable(Actor actor)
        {
            return actor.CurrentActorState != ActorState.Inactive &&
                   actor.CurrentActorState != ActorState.Despawned;
        }

        // Snapshots the live set into a reusable list (reuses capacity, so no per-frame alloc after warmup).
        private void BufferActors()
        {
            if (_isBufferDirty)
            {
                _iterationBuffer.Clear();
                _iterationBuffer.AddRange(_actors);
                _isBufferDirty = false;
            }
        }

        public override void Cleanup()
        {
            base.Cleanup();
            //Destroying actors is not ActorManager's responsibility
            _actors.Clear();
            _iterationBuffer.Clear();
            _updatedThisFrame.Clear();
            _isBufferDirty = true;
        }

        public override void OnSceneLeave()
        {
            Cleanup();
        }
    }
}
