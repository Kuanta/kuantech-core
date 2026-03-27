using System;
using System.Collections.Generic;
using UnityEngine;

namespace Kuantech.World
{
    [Serializable]
    public class ZoneQuad
    {
        public int I0, I1, I2, I3;

        public int GetIndex(int corner) => corner switch { 0 => I0, 1 => I1, 2 => I2, _ => I3 };
    }

    public class WorldArea : MonoBehaviour
    {
        public List<Vector3> Vertices = new();
        public List<ZoneQuad> Quads   = new();

        public Color FillColor    = new Color(1f, 0.5f, 0f, 0.15f);
        public Color OutlineColor = new Color(1f, 0.5f, 0f, 0.90f);

        /// <summary>Adds a standalone 4x4 quad at local origin.</summary>
        public void AddDefaultQuad()
        {
            int b = Vertices.Count;
            Vertices.Add(new Vector3(-2, 0, -2));
            Vertices.Add(new Vector3( 2, 0, -2));
            Vertices.Add(new Vector3( 2, 0,  2));
            Vertices.Add(new Vector3(-2, 0,  2));
            Quads.Add(new ZoneQuad { I0 = b, I1 = b+1, I2 = b+2, I3 = b+3 });
        }

        public Bounds GetLocalBounds()
        {
            if (Vertices.Count == 0) return new Bounds(Vector3.zero, Vector3.zero);
            var b = new Bounds(Vertices[0], Vector3.zero);
            for (int i = 1; i < Vertices.Count; i++) b.Encapsulate(Vertices[i]);
            return b;
        }

        public bool ValidQuad(ZoneQuad q) =>
            q.I0 >= 0 && q.I0 < Vertices.Count &&
            q.I1 >= 0 && q.I1 < Vertices.Count &&
            q.I2 >= 0 && q.I2 < Vertices.Count &&
            q.I3 >= 0 && q.I3 < Vertices.Count;

        // Always-visible wireframe
        private void OnDrawGizmos()
        {
            if (Vertices == null || Quads == null) return;
            Gizmos.color = OutlineColor;
            foreach (var q in Quads)
            {
                if (!ValidQuad(q)) continue;
                Vector3 w0 = transform.TransformPoint(Vertices[q.I0]);
                Vector3 w1 = transform.TransformPoint(Vertices[q.I1]);
                Vector3 w2 = transform.TransformPoint(Vertices[q.I2]);
                Vector3 w3 = transform.TransformPoint(Vertices[q.I3]);
                Gizmos.DrawLine(w0, w1);
                Gizmos.DrawLine(w1, w2);
                Gizmos.DrawLine(w2, w3);
                Gizmos.DrawLine(w3, w0);
            }
        }
    }
}
