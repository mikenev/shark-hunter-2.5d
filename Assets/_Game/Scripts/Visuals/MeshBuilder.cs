using System.Collections.Generic;
using UnityEngine;

namespace SharkHunter
{
    /// <summary>
    /// Accumulates flat-shaded (unwelded) triangles with per-triangle submesh indices.
    /// Triangle winding is corrected from an "outward" hint, so shape code doesn't need to care about winding.
    /// </summary>
    public sealed class MeshBuilder
    {
        readonly List<Vector3> verts = new List<Vector3>();
        readonly List<int>[] tris;

        /// <summary>Applied to every vertex (and outward hint) added after it is set.</summary>
        public Matrix4x4 Transform = Matrix4x4.identity;

        public MeshBuilder(int submeshCount = 1)
        {
            tris = new List<int>[submeshCount];
            for (int i = 0; i < submeshCount; i++) tris[i] = new List<int>();
        }

        public void Tri(Vector3 a, Vector3 b, Vector3 c, int sub, Vector3 outward)
        {
            a = Transform.MultiplyPoint3x4(a);
            b = Transform.MultiplyPoint3x4(b);
            c = Transform.MultiplyPoint3x4(c);
            outward = Transform.MultiplyVector(outward);

            Vector3 n = Vector3.Cross(b - a, c - a);
            if (n.sqrMagnitude < 1e-10f) return;
            if (Vector3.Dot(n, outward) < 0f) { var t = b; b = c; c = t; }

            int i = verts.Count;
            verts.Add(a); verts.Add(b); verts.Add(c);
            tris[sub].Add(i); tris[sub].Add(i + 1); tris[sub].Add(i + 2);
        }

        public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, int sub, Vector3 outward)
        {
            Tri(a, b, c, sub, outward);
            Tri(a, c, d, sub, outward);
        }

        /// <summary>Extrudes a convex XY polygon along Z.</summary>
        public void Prism(IList<Vector2> poly, float zMin, float zMax, int sub = 0)
        {
            Vector2 c = Vector2.zero;
            foreach (var p in poly) c += p;
            c /= poly.Count;

            var c0 = new Vector3(c.x, c.y, zMin);
            var c1 = new Vector3(c.x, c.y, zMax);
            for (int i = 0; i < poly.Count; i++)
            {
                Vector2 p = poly[i], q = poly[(i + 1) % poly.Count];
                var p0 = new Vector3(p.x, p.y, zMin); var q0 = new Vector3(q.x, q.y, zMin);
                var p1 = new Vector3(p.x, p.y, zMax); var q1 = new Vector3(q.x, q.y, zMax);
                Tri(c0, p0, q0, sub, Vector3.back);
                Tri(c1, p1, q1, sub, Vector3.forward);
                Vector2 mid = (p + q) * 0.5f - c;
                Quad(p0, q0, q1, p1, sub, new Vector3(mid.x, mid.y, 0f));
            }
        }

        public void Box(Vector3 center, Vector3 size, int sub = 0)
        {
            float x = size.x * 0.5f, y = size.y * 0.5f;
            var poly = new[]
            {
                new Vector2(center.x - x, center.y - y), new Vector2(center.x + x, center.y - y),
                new Vector2(center.x + x, center.y + y), new Vector2(center.x - x, center.y + y),
            };
            Prism(poly, center.z - size.z * 0.5f, center.z + size.z * 0.5f, sub);
        }

        /// <summary>
        /// Lofts along +X from ring to ring. Rings of radius (ry, rz). Triangles in the lower half (y &lt; 0) use subBelly.
        /// The first/last rings are capped.
        /// </summary>
        public void Loft(float[] xs, float[] ry, float[] rz, int sides, int subTop, int subBelly, float rotOffset = 0f)
        {
            int n = xs.Length;
            var rings = new Vector3[n][];
            for (int i = 0; i < n; i++)
            {
                rings[i] = new Vector3[sides];
                for (int k = 0; k < sides; k++)
                {
                    float a = rotOffset + k * Mathf.PI * 2f / sides;
                    rings[i][k] = new Vector3(xs[i], Mathf.Sin(a) * ry[i], Mathf.Cos(a) * rz[i]);
                }
            }

            for (int i = 0; i < n - 1; i++)
            for (int k = 0; k < sides; k++)
            {
                int k2 = (k + 1) % sides;
                Vector3 a = rings[i][k], b = rings[i][k2], c = rings[i + 1][k2], d = rings[i + 1][k];
                Vector3 centroid = (a + b + c + d) * 0.25f;
                int sub = centroid.y < -0.001f ? subBelly : subTop;
                Quad(a, b, c, d, sub, new Vector3(0f, centroid.y, centroid.z));
            }

            Cap(rings[0], new Vector3(xs[0] - Mathf.Abs(xs[1] - xs[0]) * 0.15f, 0f, 0f), subTop, Vector3.left);
            Cap(rings[n - 1], new Vector3(xs[n - 1], 0f, 0f), subTop, Vector3.right);
        }

        void Cap(Vector3[] ring, Vector3 apex, int sub, Vector3 outward)
        {
            for (int k = 0; k < ring.Length; k++)
                Tri(apex, ring[k], ring[(k + 1) % ring.Length], sub, outward);
        }

        public void Sphere(Vector3 center, Vector3 radii, int segments, int rings, int sub = 0, System.Func<int, int, float> jitter = null)
        {
            Vector3 P(int ring, int seg)
            {
                float v = ring / (float)rings;
                float phi = v * Mathf.PI;
                float theta = seg * Mathf.PI * 2f / segments;
                float j = (jitter != null && ring > 0 && ring < rings) ? jitter(ring, seg % segments) : 1f;
                return center + new Vector3(Mathf.Cos(phi) * radii.x, Mathf.Sin(phi) * Mathf.Sin(theta) * radii.y, Mathf.Sin(phi) * Mathf.Cos(theta) * radii.z) * j;
            }
            for (int r = 0; r < rings; r++)
            for (int s = 0; s < segments; s++)
            {
                Vector3 a = P(r, s), b = P(r, s + 1), c = P(r + 1, s + 1), d = P(r + 1, s);
                Quad(a, b, c, d, sub, (a + b + c + d) * 0.25f - center);
            }
        }

        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh { name = name, indexFormat = verts.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
            mesh.SetVertices(verts);
            mesh.subMeshCount = tris.Length;
            for (int i = 0; i < tris.Length; i++) mesh.SetTriangles(tris[i], i);
            mesh.SetUVs(0, new List<Vector2>(new Vector2[verts.Count]));
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
