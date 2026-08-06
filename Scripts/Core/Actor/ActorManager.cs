using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Kuantech.Core
{
    /// <summary>
    /// Drives every registered <see cref="Actor"/> from one place instead of each actor carrying its own
    /// Unity Update. Consolidating the per-MonoBehaviour Update/FixedUpdate/LateUpdate calls into a single
    /// managed loop removes the native→managed call overhead Unity pays per component — a modest but real
    /// win when hundreds of actors are alive. Actors register on Spawn and unregister on Cleanup (despawn).
    /// </summary>
    public class ActorManager : SubManager
    {
        private readonly HashSet<Actor> _actors = new HashSet<Actor>();
        // Iterated instead of _actors directly so an actor that registers/unregisters mid-update (e.g. one
        // that spawns a projectile actor, or despawns itself) can't mutate the collection we're looping.
        private readonly List<Actor> _iterationBuffer = new List<Actor>();

        public override async UniTask Initialize(GameManager gameManager)
        {
            await base.Initialize(gameManager);
            _actors.Clear();
        }

        public static void RegisterActor(Actor actor)
        {
            var ctx = GetContext<ActorManager>();
            if (ctx == null || actor == null) return;
            ctx._actors.Add(actor);
        }

        public static void UnregisterActor(Actor actor)
        {
            var ctx = GetContext<ActorManager>();
            if (ctx == null || actor == null) return;
            ctx._actors.Remove(actor);
        }

        private void FixedUpdate()
        {
            if (!Initialized) return;
            BufferActors();
            for (int i = 0; i < _iterationBuffer.Count; i++)
            {
                Actor actor = _iterationBuffer[i];
                if (actor != null) actor.ManagedFixedUpdate();
            }
        }

        private void Update()
        {
            if (!Initialized) return;
            BufferActors();
            for (int i = 0; i < _iterationBuffer.Count; i++)
            {
                Actor actor = _iterationBuffer[i];
                if (actor != null) actor.ManagedUpdate();
            }
        }

        private void LateUpdate()
        {
            if (!Initialized) return;
            BufferActors();
            for (int i = 0; i < _iterationBuffer.Count; i++)
            {
                Actor actor = _iterationBuffer[i];
                if (actor != null) actor.ManagedLateUpdate();
            }
        }

        // Snapshots the live set into a reusable list (reuses capacity, so no per-frame alloc after warmup).
        private void BufferActors()
        {
            _iterationBuffer.Clear();
            _iterationBuffer.AddRange(_actors);
        }

        public override void Cleanup()
        {
            base.Cleanup();
            //Destroying actors is not ActorManager's responsibility
            _actors.Clear();
            _iterationBuffer.Clear();
        }

        public override void OnSceneLeave()
        {
            Cleanup();
        }
    }
}
