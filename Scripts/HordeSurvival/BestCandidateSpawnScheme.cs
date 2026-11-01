using System;
using System.Collections.Generic;
using Kuantech.Core;
using UnityEngine;

namespace Kuantech.HordeSurvival
{
    /// <summary>
    /// Spawns where the horde is thinnest, inside the part of the ground the player is about to see.
    ///
    /// Each spawn compares a handful of random points that lie in the <see cref="PredictedViewRegion"/> (on
    /// screen now or soon), scores each by how far it is from the nearest living enemy (isolation) plus a
    /// bonus for points that are not on screen yet, and takes the best. This is Mitchell's best-candidate: it
    /// fills voids and avoids clumps without any arc, ring or direction setting, so the same behavior holds
    /// whatever the camera or the movement path.
    ///
    /// Two things keep it cheap when a whole pass of enemies is placed in one frame. The region test is the
    /// expensive part and most random points fail it, so a pool of points that already passed is drawn once
    /// per frame and every spawn of that frame picks its candidates from it. And the enemies live in a flat
    /// grid of arrays rather than a dictionary of lists, so the isolation query is plain indexing and
    /// rebuilding it allocates nothing.
    ///
    /// The owner feeds it: call <see cref="RebuildPopulation"/> and <see cref="SetMotion"/> before a batch of
    /// spawns, and <see cref="NotifyPlaced"/> after each accepted point so the next ones of the same batch see it.
    /// </summary>
    [Serializable]
    public class BestCandidateSpawnScheme : SpawnScheme
    {
        [Header("Region")]
        [Tooltip("The part of the ground that is on screen now or will be soon. Candidates are drawn from it.")]
        public PredictedViewRegion Region = new PredictedViewRegion();

        [Header("Distance (meters, from the focus point)")]
        [Tooltip("Never spawn closer than this to the focus point.")]
        public float MinDistance = 8f;
        [Tooltip("Candidates are drawn within this distance of the focus point.")]
        public float MaxDistance = 70f;

        [Header("Best Candidate")]
        [Tooltip("How many region points are compared per spawn. More = more even spread, a bit more cost.")]
        [Range(1, 32)]
        public int Candidates = 12;
        [Tooltip("Isolation is measured up to this distance: a point with no enemy within it scores the maximum.")]
        public float SearchRadius = 12f;
        [Tooltip("Added to the score of points that are not on screen yet (about to come into view), so the " +
                 "horde fills in ahead of the screen before it fills in on top of it. 0 = no preference.")]
        public float UpcomingBonus = 0.5f;
        [Tooltip("Cell size of the internal grid. About a third of the Search Radius works well.")]
        public float CellSize = 4f;
        [Tooltip("Size of the per-frame pool of region points the candidates are drawn from, as a multiple of " +
                 "Candidates. Bigger = more variety between spawns of the same frame, a bit more cost per frame.")]
        [Range(2, 16)]
        public int PoolFactor = 6;

        [Header("Brush")]
        [Tooltip("A brush stroke puts a whole group down around one thin spot instead of one enemy per spot. " +
                 "This is the radius it covers at least (it grows when the group is too big to fit). " +
                 "0 = no brush, enemies are placed one by one.")]
        public float BrushRadius = 6f;
        [Tooltip("Closest two enemies of a stroke may be placed. The crowd separation spreads them from there.")]
        public float MinSpacing = 1.2f;

        // Tries per enemy of a stroke to find a spot that is clear of the others before giving it up.
        private const int BrushTriesPerEnemy = 6;

        // Pool sampling gives up after this many tries per wanted point when the region is sparse.
        private const int TriesPerPoolPoint = 8;

        [NonSerialized] private Vector3 _velocity;
        [NonSerialized] private float _turnRate;

        // Flat grid of the living enemies, as linked lists in arrays: _head[cell] is the first enemy in the
        // cell, _next[i] the following one. Covers the farthest a candidate can be plus the search radius,
        // centered on the focus at the last rebuild; enemies outside it cannot affect any candidate.
        [NonSerialized] private int[] _head;
        [NonSerialized] private int[] _next;
        [NonSerialized] private float[] _px;
        [NonSerialized] private float[] _pz;
        [NonSerialized] private int _count;
        [NonSerialized] private int _gridWidth;
        [NonSerialized] private float _gridCell;
        [NonSerialized] private float _originX;
        [NonSerialized] private float _originZ;

        // Region points drawn for this frame, with whether each is already on screen (upcoming gets a bonus).
        [NonSerialized] private Vector3[] _pool;
        [NonSerialized] private bool[] _poolUpcoming;
        [NonSerialized] private int _poolCount;
        [NonSerialized] private bool _poolStale = true;

        /// <summary>The focus' current velocity (XZ) and turn rate in degrees per second, for the region's forecast.</summary>
        public void SetMotion(Vector3 velocity, float turnRateDegrees)
        {
            _velocity = velocity;
            _turnRate = turnRateDegrees;
            _poolStale = true;
        }

        /// <summary>Re-bins the living enemies into the internal grid. Call once per frame before spawning.</summary>
        public void RebuildPopulation(HashSet<Actor> alive, Vector3 focus)
        {
            BeginPopulation(focus);

            if (alive == null) return;
            foreach (Actor actor in alive)
            {
                if (actor == null) continue;
                Vector3 p = actor.transform.position;
                Add(p.x, p.z);
            }
        }

        /// <summary>The same for a crowd kept as plain arrays: positions are (x, z), a slot counts when its alive flag is set.</summary>
        public void RebuildPopulation(Unity.Collections.NativeArray<Unity.Mathematics.float2> positions,
            Unity.Collections.NativeArray<byte> alive, int count, Vector3 focus)
        {
            BeginPopulation(focus);
            for (int i = 0; i < count; i++)
            {
                if (alive[i] == 0) continue;
                Add(positions[i].x, positions[i].y);
            }
        }

        // Clears the grid and centers it on the focus; the callers above then Add every enemy.
        private void BeginPopulation(Vector3 focus)
        {
            _gridCell = Mathf.Max(0.5f, CellSize);
            float extent = Mathf.Max(MinDistance, MaxDistance) + Mathf.Max(_gridCell, SearchRadius);
            int width = Mathf.CeilToInt(extent * 2f / _gridCell) + 1;

            if (_head == null || _gridWidth != width)
            {
                _gridWidth = width;
                _head = new int[width * width];
            }
            Array.Fill(_head, -1);
            _originX = focus.x - extent;
            _originZ = focus.z - extent;
            _count = 0;
            _poolStale = true;
        }

        /// <summary>Registers a point that was just accepted, so later candidates of the same batch keep away from it.</summary>
        public void NotifyPlaced(Vector3 position)
        {
            if (_head == null) return;
            Add(position.x, position.z);
        }

        /// <summary>
        /// One brush stroke: finds a thin spot (the stroke's center), then fills the disc around it with up to
        /// <paramref name="count"/> points, each at least <see cref="MinSpacing"/> from everything already
        /// there. Every point returned is already registered with the scheme, so the owner must not call
        /// <see cref="NotifyPlaced"/> for them. Returns how many fit; fewer than asked is normal in a tight spot.
        /// </summary>
        public int FillBrush(Vector3 focus, int count, List<Vector3> results)
        {
            results.Clear();
            if (count <= 0 || !TryGetSpawnPoint(focus, out Vector3 center)) return 0;

            float spacing = Mathf.Max(0f, MinSpacing);
            // Room for the group at roughly the spacing asked for, with slack for the tries that miss.
            float neededRadius = Mathf.Sqrt(count * spacing * spacing * 1.3f / Mathf.PI);
            float radius = Mathf.Max(BrushRadius, neededRadius);

            results.Add(center);
            NotifyPlaced(center);

            int tries = (count - 1) * BrushTriesPerEnemy;
            for (int i = 0; i < tries && results.Count < count; i++)
            {
                float r = Mathf.Sqrt(UnityEngine.Random.value) * radius;
                float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                Vector3 point = center + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * r;

                if (NearestDistance(point) < spacing) continue;
                if (!IsPointValid(point)) continue;

                results.Add(point);
                NotifyPlaced(point);
            }
            return results.Count;
        }

        protected override Vector3 SampleCandidate(Vector3 focus)
        {
            UnityEngine.Camera view = CameraManager.GetCamera();
            if (view == null) return SampleAnnulus(focus);

            if (_poolStale) FillPool(view, focus);
            if (_poolCount == 0) return SampleAnnulus(focus);

            Vector3 best = default;
            float bestScore = float.NegativeInfinity;
            int picks = Mathf.Max(1, Candidates);
            for (int i = 0; i < picks; i++)
            {
                int index = UnityEngine.Random.Range(0, _poolCount);
                Vector3 point = _pool[index];
                float score = Isolation(point) + (_poolUpcoming[index] ? UpcomingBonus : 0f)
                              + UnityEngine.Random.value * 0.02f; // tie-break between equally empty spots
                if (score > bestScore)
                {
                    bestScore = score;
                    best = point;
                }
            }
            return best;
        }

        // Draws the frame's pool: random points of the ring that the region says are (or will be) on screen.
        private void FillPool(UnityEngine.Camera view, Vector3 focus)
        {
            int size = Mathf.Max(1, Candidates) * Mathf.Max(2, PoolFactor);
            if (_pool == null || _pool.Length != size)
            {
                _pool = new Vector3[size];
                _poolUpcoming = new bool[size];
            }

            _poolCount = 0;
            int budget = size * TriesPerPoolPoint;
            for (int i = 0; i < budget && _poolCount < size; i++)
            {
                Vector3 point = SampleAnnulus(focus);
                if (!Region.Contains(view, point, _velocity, _turnRate, out float firstTime)) continue;

                _pool[_poolCount] = point;
                _poolUpcoming[_poolCount] = firstTime > 0f;
                _poolCount++;
            }
            _poolStale = false;
        }

        // Area-uniform point in the ring MinDistance..MaxDistance around the focus.
        private Vector3 SampleAnnulus(Vector3 focus)
        {
            float max = Mathf.Max(MinDistance, MaxDistance);
            float radius = Mathf.Sqrt(UnityEngine.Random.Range(MinDistance * MinDistance, max * max));
            float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            return focus + new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * radius;
        }

        // Distance to the nearest enemy, in meters, measured up to the search radius.
        private float NearestDistance(Vector3 point)
        {
            return Isolation(point) * Mathf.Max(_gridCell, SearchRadius);
        }

        /// <summary>0..1: distance to the nearest enemy, measured up to the search radius.</summary>
        private float Isolation(Vector3 point)
        {
            float search = Mathf.Max(_gridCell, SearchRadius);
            if (_head == null) return 1f;

            int range = Mathf.CeilToInt(search / _gridCell);
            int cx = Mathf.FloorToInt((point.x - _originX) / _gridCell);
            int cz = Mathf.FloorToInt((point.z - _originZ) / _gridCell);
            int minX = Mathf.Max(0, cx - range), maxX = Mathf.Min(_gridWidth - 1, cx + range);
            int minZ = Mathf.Max(0, cz - range), maxZ = Mathf.Min(_gridWidth - 1, cz + range);

            float bestSq = search * search;
            for (int x = minX; x <= maxX; x++)
            {
                int row = x * _gridWidth;
                for (int z = minZ; z <= maxZ; z++)
                {
                    for (int i = _head[row + z]; i >= 0; i = _next[i])
                    {
                        float ox = _px[i] - point.x;
                        float oz = _pz[i] - point.z;
                        float sq = ox * ox + oz * oz;
                        if (sq < bestSq) bestSq = sq;
                    }
                }
            }
            return Mathf.Sqrt(bestSq) / search;
        }

        private void Add(float x, float z)
        {
            float fx = (x - _originX) / _gridCell;
            float fz = (z - _originZ) / _gridCell;
            if (fx < 0f || fz < 0f || fx >= _gridWidth || fz >= _gridWidth) return;

            if (_px == null)
            {
                _px = new float[256];
                _pz = new float[256];
                _next = new int[256];
            }
            else if (_count == _px.Length)
            {
                Array.Resize(ref _px, _count * 2);
                Array.Resize(ref _pz, _count * 2);
                Array.Resize(ref _next, _count * 2);
            }

            int cell = (int)fx * _gridWidth + (int)fz;
            _px[_count] = x;
            _pz[_count] = z;
            _next[_count] = _head[cell];
            _head[cell] = _count;
            _count++;
        }
    }
}
