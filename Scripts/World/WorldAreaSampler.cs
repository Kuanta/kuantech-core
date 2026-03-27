using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.World
{
    /// <summary>
    /// Sampling utilities for WorldArea.
    /// All public methods accept/return world-space positions.
    /// </summary>
    public static class WorldAreaSampler
    {
        // ── Single Point (multiple areas, area-weighted) ──────────────────────

        /// <summary>
        /// Picks one WorldArea weighted by XZ surface area, then samples from it.
        /// Larger areas get proportionally more spawn candidates.
        /// </summary>
        public static bool TrySampleOne(IList<WorldArea> areas, float minDistance,
            IList<Vector3> existingWorldPoints, out Vector3 worldPosition,
            int maxAttempts = 30)
        {
            worldPosition = Vector3.zero;
            if (areas == null || areas.Count == 0) return false;

            WorldArea picked = PickAreaWeighted(areas);
            return picked != null &&
                   TrySampleOne(picked, minDistance, existingWorldPoints, out worldPosition, maxAttempts);
        }

        /// <summary>
        /// Returns a WorldArea chosen at random, weighted by its XZ surface area.
        /// </summary>
        public static WorldArea PickAreaWeighted(IList<WorldArea> areas)
        {
            float total = 0f;
            foreach (var a in areas) total += ComputeXZArea(a);

            if (total <= 0f) return areas[Random.Range(0, areas.Count)];

            float pick = Random.Range(0f, total);
            float accumulated = 0f;
            foreach (var a in areas)
            {
                accumulated += ComputeXZArea(a);
                if (pick <= accumulated) return a;
            }
            return areas[^1];
        }

        /// <summary>
        /// Computes the total XZ surface area of a WorldArea (sum of all quad triangles).
        /// </summary>
        public static float ComputeXZArea(WorldArea area)
        {
            float total = 0f;
            foreach (var q in area.Quads)
            {
                if (!area.ValidQuad(q)) continue;
                Vector2 v0 = XZ(area.Vertices[q.I0]);
                Vector2 v1 = XZ(area.Vertices[q.I1]);
                Vector2 v2 = XZ(area.Vertices[q.I2]);
                Vector2 v3 = XZ(area.Vertices[q.I3]);
                total += TriangleArea2D(v0, v1, v2);
                total += TriangleArea2D(v0, v2, v3);
            }
            return total;
        }

        private static float TriangleArea2D(Vector2 a, Vector2 b, Vector2 c) =>
            Mathf.Abs(Cross(b - a, c - a)) * 0.5f;

        // ── Single Point ──────────────────────────────────────────────────────

        /// <summary>
        /// Tries to find one valid position inside the area that is at least
        /// minDistance from every point in existingWorldPoints.
        /// Uses simple rejection sampling — fast for small actor counts.
        /// </summary>
        public static bool TrySampleOne(WorldArea area, float minDistance,
            IList<Vector3> existingWorldPoints, out Vector3 worldPosition,
            int maxAttempts = 30)
        {
            Bounds localBounds = area.GetLocalBounds();
            float  minDistSq   = minDistance * minDistance;

            for (int i = 0; i < maxAttempts; i++)
            {
                Vector3 local = RandomInBounds(localBounds);
                if (!IsInsideZone(area, local)) continue;

                Vector3 world = area.transform.TransformPoint(WithInterpolatedY(area, local));
                if (IsFarFromAll(world, existingWorldPoints, minDistSq))
                {
                    worldPosition = world;
                    return true;
                }
            }

            worldPosition = Vector3.zero;
            return false;
        }

        // ── Poisson Disk Batch ────────────────────────────────────────────────

        /// <summary>
        /// Generates well-distributed positions inside the area using
        /// Bridson's Poisson disk algorithm.
        /// existingWorldPoints are treated as already-placed obstacles —
        /// new points will keep minDistance from them too.
        /// Returns world-space positions (only the newly generated ones).
        /// </summary>
        public static List<Vector3> SamplePoisson(WorldArea area, float minDistance,
            IList<Vector3> existingWorldPoints = null, int maxCandidatesPerPoint = 30)
        {
            Bounds localBounds = area.GetLocalBounds();

            float cellSize = minDistance / Mathf.Sqrt(2f);
            int   gridW    = Mathf.CeilToInt(localBounds.size.x / cellSize) + 1;
            int   gridH    = Mathf.CeilToInt(localBounds.size.z / cellSize) + 1;

            int[,]       grid   = InitGrid(gridW, gridH);
            List<Vector3> points = new();  // local space
            List<int>     active = new();

            // Seed existing points as obstacles (not returned as new results)
            int existingCount = 0;
            if (existingWorldPoints != null)
            {
                foreach (var wp in existingWorldPoints)
                {
                    Vector3 lp = area.transform.InverseTransformPoint(wp);
                    AddToGrid(lp, localBounds, cellSize, gridW, gridH, grid, points);
                }
                existingCount = points.Count;
            }

            // First point — random inside zone
            Vector3 first = FindInitialPoint(area, localBounds, 100);
            if (!IsInsideZone(area, first))
            {
                Debug.LogWarning("[WorldAreaSampler] Could not find initial point inside zone.");
                return new List<Vector3>();
            }

            int firstIdx = points.Count;
            AddToGrid(first, localBounds, cellSize, gridW, gridH, grid, points);
            active.Add(firstIdx);

            // Bridson's main loop
            while (active.Count > 0)
            {
                int     pickIdx = Random.Range(0, active.Count);
                int     ptIdx   = active[pickIdx];
                Vector3 origin  = points[ptIdx];
                bool    placed  = false;

                for (int k = 0; k < maxCandidatesPerPoint; k++)
                {
                    Vector3 candidate = RandomAnnulus(origin, minDistance);

                    if (!InsideBounds(candidate, localBounds))  continue;
                    if (!IsInsideZone(area, candidate))         continue;
                    if (!FarEnoughInGrid(candidate, localBounds, cellSize, gridW, gridH,
                                         grid, points, minDistance)) continue;

                    int newIdx = points.Count;
                    AddToGrid(candidate, localBounds, cellSize, gridW, gridH, grid, points);
                    active.Add(newIdx);
                    placed = true;
                    break;
                }

                if (!placed)
                    active.RemoveAt(pickIdx);
            }

            // Return only newly generated points in world space
            var result = new List<Vector3>(points.Count - existingCount);
            for (int i = existingCount; i < points.Count; i++)
            {
                Vector3 localWithY = WithInterpolatedY(area, points[i]);
                result.Add(area.transform.TransformPoint(localWithY));
            }
            return result;
        }

        // ── Zone Geometry ─────────────────────────────────────────────────────

        private static bool IsInsideZone(WorldArea area, Vector3 localPoint)
        {
            Vector2 p = XZ(localPoint);
            foreach (var q in area.Quads)
            {
                if (!area.ValidQuad(q)) continue;
                Vector2 v0 = XZ(area.Vertices[q.I0]);
                Vector2 v1 = XZ(area.Vertices[q.I1]);
                Vector2 v2 = XZ(area.Vertices[q.I2]);
                Vector2 v3 = XZ(area.Vertices[q.I3]);
                if (PointInTriangle2D(p, v0, v1, v2)) return true;
                if (PointInTriangle2D(p, v0, v2, v3)) return true;
            }
            return false;
        }

        /// <summary>
        /// Replaces the Y of a local-space XZ point with the interpolated Y
        /// from the quad surface it falls on. Handles sloped/non-flat zones.
        /// </summary>
        private static Vector3 WithInterpolatedY(WorldArea area, Vector3 local)
        {
            Vector2 p = XZ(local);
            foreach (var q in area.Quads)
            {
                if (!area.ValidQuad(q)) continue;
                Vector3 v0 = area.Vertices[q.I0];
                Vector3 v1 = area.Vertices[q.I1];
                Vector3 v2 = area.Vertices[q.I2];
                Vector3 v3 = area.Vertices[q.I3];

                if (PointInTriangle2D(p, XZ(v0), XZ(v1), XZ(v2)))
                    return new Vector3(local.x, BarycentricY(p, v0, v1, v2), local.z);
                if (PointInTriangle2D(p, XZ(v0), XZ(v2), XZ(v3)))
                    return new Vector3(local.x, BarycentricY(p, v0, v2, v3), local.z);
            }
            return local;
        }

        private static float BarycentricY(Vector2 p, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector2 v0 = XZ(b) - XZ(a), v1 = XZ(c) - XZ(a), v2 = p - XZ(a);
            float d00 = Vector2.Dot(v0, v0), d01 = Vector2.Dot(v0, v1);
            float d11 = Vector2.Dot(v1, v1), d20 = Vector2.Dot(v2, v0), d21 = Vector2.Dot(v2, v1);
            float denom = d00 * d11 - d01 * d01;
            if (Mathf.Abs(denom) < 1e-6f) return a.y;
            float v = (d11 * d20 - d01 * d21) / denom;
            float w = (d00 * d21 - d01 * d20) / denom;
            return (1f - v - w) * a.y + v * b.y + w * c.y;
        }

        private static bool PointInTriangle2D(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Cross(p - a, b - a);
            float d2 = Cross(p - b, c - b);
            float d3 = Cross(p - c, a - c);
            return !((d1 < 0 || d2 < 0 || d3 < 0) && (d1 > 0 || d2 > 0 || d3 > 0));
        }

        // ── Grid Helpers ──────────────────────────────────────────────────────

        private static int[,] InitGrid(int w, int h)
        {
            var g = new int[w, h];
            for (int x = 0; x < w; x++)
                for (int z = 0; z < h; z++)
                    g[x, z] = -1;
            return g;
        }

        private static void AddToGrid(Vector3 local, Bounds bounds, float cellSize,
            int gridW, int gridH, int[,] grid, List<Vector3> points)
        {
            int gx = Mathf.Clamp(Mathf.FloorToInt((local.x - bounds.min.x) / cellSize), 0, gridW - 1);
            int gz = Mathf.Clamp(Mathf.FloorToInt((local.z - bounds.min.z) / cellSize), 0, gridH - 1);
            grid[gx, gz] = points.Count;
            points.Add(local);
        }

        private static bool FarEnoughInGrid(Vector3 candidate, Bounds bounds, float cellSize,
            int gridW, int gridH, int[,] grid, List<Vector3> points, float minDist)
        {
            int cx = Mathf.FloorToInt((candidate.x - bounds.min.x) / cellSize);
            int cz = Mathf.FloorToInt((candidate.z - bounds.min.z) / cellSize);
            float minSq = minDist * minDist;

            for (int dx = -2; dx <= 2; dx++)
            for (int dz = -2; dz <= 2; dz++)
            {
                int nx = cx + dx, nz = cz + dz;
                if (nx < 0 || nx >= gridW || nz < 0 || nz >= gridH) continue;
                int idx = grid[nx, nz];
                if (idx < 0) continue;
                Vector3 diff = candidate - points[idx];
                diff.y = 0f; // XZ distance only
                if (diff.sqrMagnitude < minSq) return false;
            }
            return true;
        }

        // ── Misc ──────────────────────────────────────────────────────────────

        private static Vector3 FindInitialPoint(WorldArea area, Bounds bounds, int attempts)
        {
            for (int i = 0; i < attempts; i++)
            {
                Vector3 p = RandomInBounds(bounds);
                if (IsInsideZone(area, p)) return p;
            }
            return bounds.center; // fallback
        }

        private static Vector3 RandomInBounds(Bounds b) =>
            new(Random.Range(b.min.x, b.max.x), 0f, Random.Range(b.min.z, b.max.z));

        private static Vector3 RandomAnnulus(Vector3 origin, float minDist)
        {
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float dist  = Random.Range(minDist, minDist * 2f);
            return origin + new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
        }

        private static bool InsideBounds(Vector3 p, Bounds b) =>
            p.x >= b.min.x && p.x <= b.max.x && p.z >= b.min.z && p.z <= b.max.z;

        private static bool IsFarFromAll(Vector3 p, IList<Vector3> others, float minSq)
        {
            if (others == null) return true;
            foreach (var o in others)
            {
                Vector3 diff = p - o;
                diff.y = 0f;
                if (diff.sqrMagnitude < minSq) return false;
            }
            return true;
        }

        private static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
        private static Vector2 XZ(Vector3 v)              => new(v.x, v.z);
    }
}
