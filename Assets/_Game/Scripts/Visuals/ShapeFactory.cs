using UnityEngine;

namespace SharkHunter
{
    /// <summary>
    /// Procedural low-poly placeholder meshes, so the project runs before any real art exists.
    /// Meshes are flat-shaded. Submesh slots are documented per mesh so materials can be assigned by index.
    /// Nothing in gameplay code references this class.
    /// </summary>
    public static class ShapeFactory
    {
        // Shark body: submesh 0 = back, 1 = belly, 2 = dark (eyes). Faces +X, length ~2.8, centred near origin.
        public const float SharkTailJointX = -1.2f;

        public static Mesh SharkBody()
        {
            var b = new MeshBuilder(3);
            b.Loft(
                new[] { 1.6f, 1.45f, 1.0f, 0.4f, -0.2f, -0.8f, SharkTailJointX },
                new[] { 0.04f, 0.19f, 0.33f, 0.40f, 0.33f, 0.19f, 0.08f },
                new[] { 0.04f, 0.17f, 0.29f, 0.34f, 0.28f, 0.16f, 0.06f },
                sides: 8, subTop: 0, subBelly: 1, rotOffset: Mathf.PI / 8f);

            // Dorsal fin.
            b.Prism(new[] { new Vector2(0.45f, 0.36f), new Vector2(-0.02f, 0.95f), new Vector2(-0.4f, 0.3f) }, -0.025f, 0.025f, 0);

            // Pectoral fins, swept back and down.
            var pec = new[] { new Vector2(0.3f, 0f), new Vector2(-0.35f, 0f), new Vector2(-0.55f, 0.85f) };
            for (int s = -1; s <= 1; s += 2)
            {
                b.Transform = Matrix4x4.TRS(new Vector3(0.55f, -0.18f, 0.2f * s), Quaternion.Euler(110f * s, 0f, 0f), Vector3.one);
                b.Prism(pec, -0.02f, 0.02f, 0);
            }

            // Small rear fins.
            b.Transform = Matrix4x4.identity;
            b.Prism(new[] { new Vector2(-0.55f, 0.2f), new Vector2(-0.8f, 0.45f), new Vector2(-0.95f, 0.15f) }, -0.015f, 0.015f, 0);
            b.Prism(new[] { new Vector2(-0.6f, -0.2f), new Vector2(-0.8f, -0.4f), new Vector2(-0.95f, -0.14f) }, -0.015f, 0.015f, 1);

            // Eyes.
            for (int s = -1; s <= 1; s += 2)
                b.Sphere(new Vector3(1.22f, 0.1f, 0.15f * s), Vector3.one * 0.045f, 5, 4, 2);

            return b.ToMesh("Shark_Body");
        }

        // Tail: origin is the tail joint, so it can rotate about it. Submesh 0.
        public static Mesh SharkTail()
        {
            var b = new MeshBuilder(1);
            var upper = new[] { new Vector2(0.02f, 0.07f), new Vector2(-0.5f, 0.95f), new Vector2(-0.68f, 0.78f), new Vector2(-0.3f, 0f) };
            b.Prism(upper, -0.025f, 0.025f, 0);
            var lower = new Vector2[upper.Length];
            for (int i = 0; i < upper.Length; i++) lower[i] = new Vector2(upper[i].x * 0.85f, -upper[i].y * 0.6f);
            b.Prism(lower, -0.02f, 0.02f, 0);
            return b.ToMesh("Shark_Tail");
        }

        // Lower jaw: origin is the hinge, extends along +X. Submesh 0.
        public static Mesh SharkJaw()
        {
            var b = new MeshBuilder(1);
            b.Prism(new[] { new Vector2(0f, 0.02f), new Vector2(0.6f, 0.07f), new Vector2(0.55f, -0.04f), new Vector2(0f, -0.1f) }, -0.13f, 0.13f, 0);
            for (int i = 0; i < 3; i++)
            {
                float x = 0.2f + i * 0.14f;
                b.Prism(new[] { new Vector2(x, 0.05f), new Vector2(x + 0.05f, 0.05f), new Vector2(x + 0.025f, 0.13f) }, -0.1f, 0.1f, 0);
            }
            return b.ToMesh("Shark_Jaw");
        }

        public const float JawHingeX = 1.0f, JawHingeY = -0.2f;

        // Fish: submesh 0 = back/fins, 1 = belly, 2 = dark. Faces +X, length ~0.8.
        public static Mesh Fish()
        {
            var b = new MeshBuilder(3);
            b.Loft(
                new[] { 0.4f, 0.3f, 0.1f, -0.15f, -0.35f },
                new[] { 0.03f, 0.14f, 0.17f, 0.1f, 0.04f },
                new[] { 0.02f, 0.07f, 0.08f, 0.05f, 0.02f },
                sides: 6, subTop: 0, subBelly: 1, rotOffset: Mathf.PI / 6f);
            b.Prism(new[] { new Vector2(-0.3f, 0f), new Vector2(-0.6f, 0.22f), new Vector2(-0.6f, -0.22f) }, -0.015f, 0.015f, 0);
            b.Prism(new[] { new Vector2(0.15f, 0.15f), new Vector2(-0.15f, 0.3f), new Vector2(-0.2f, 0.1f) }, -0.012f, 0.012f, 0);
            for (int s = -1; s <= 1; s += 2)
                b.Sphere(new Vector3(0.27f, 0.05f, 0.05f * s), Vector3.one * 0.02f, 4, 3, 2);
            return b.ToMesh("Fish");
        }

        // Rock: one submesh, roughly 2 wide, flat bottom buried at y = 0.
        public static Mesh Rock(int seed)
        {
            var rnd = new System.Random(seed);
            var noise = new float[8, 12];
            for (int r = 0; r < 8; r++) for (int s = 0; s < 12; s++) noise[r, s] = 0.8f + (float)rnd.NextDouble() * 0.4f;
            var b = new MeshBuilder(1);
            b.Sphere(Vector3.zero, new Vector3(1f, 0.75f, 0.85f), 7, 5, 0, (r, s) => noise[r, s]);
            var mesh = b.ToMesh("Rock_" + seed);
            return mesh;
        }

        // Kelp: three tapered blades. Origin at the base, grows along +Y, ~7 tall. One submesh.
        public static Mesh Kelp(int seed)
        {
            var rnd = new System.Random(seed);
            var b = new MeshBuilder(1);
            const int segs = 8;
            for (int blade = 0; blade < 3; blade++)
            {
                float yaw = blade * 60f + (float)rnd.NextDouble() * 20f;
                float lean = ((float)rnd.NextDouble() - 0.5f) * 0.6f;
                b.Transform = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, yaw, 0f), Vector3.one);
                float x = 0f;
                for (int i = 0; i < segs; i++)
                {
                    float y0 = i * 0.9f, y1 = (i + 1) * 0.9f;
                    float x0 = x, x1 = x + Mathf.Sin(i * 0.8f + blade) * 0.18f + lean * 0.1f;
                    float w0 = Mathf.Lerp(0.32f, 0.06f, i / (float)segs), w1 = Mathf.Lerp(0.32f, 0.06f, (i + 1) / (float)segs);
                    b.Prism(new[] { new Vector2(x0 - w0, y0), new Vector2(x0 + w0, y0), new Vector2(x1 + w1, y1), new Vector2(x1 - w1, y1) }, -0.03f, 0.03f, 0);
                    x = x1;
                }
            }
            return b.ToMesh("Kelp_" + seed);
        }

        public static float SeabedHeight(float x, float z)
        {
            float n = Mathf.PerlinNoise(x * 0.11f + 40f, z * 0.17f + 7f) - 0.5f;
            return -6f + Mathf.Max(0f, z) * 0.04f + n * 1.0f;
        }

        // Seabed: x in [-70, 70], z in [-10, 75]. One submesh.
        public static Mesh Seabed()
        {
            var b = new MeshBuilder(1);
            const float x0 = -70f, x1 = 70f, z0 = -10f, z1 = 75f, step = 2.5f;
            for (float x = x0; x < x1; x += step)
            for (float z = z0; z < z1; z += step)
            {
                Vector3 a = new Vector3(x, SeabedHeight(x, z), z);
                Vector3 bb = new Vector3(x + step, SeabedHeight(x + step, z), z);
                Vector3 c = new Vector3(x + step, SeabedHeight(x + step, z + step), z + step);
                Vector3 d = new Vector3(x, SeabedHeight(x, z + step), z + step);
                b.Quad(a, bb, c, d, 0, Vector3.up);
            }
            return b.ToMesh("Seabed");
        }

        // Ridge: a jagged silhouette wall facing the camera (-Z), top profile from noise. One submesh.
        public static Mesh Ridge(int seed, float width, float baseY, float amplitude)
        {
            var b = new MeshBuilder(1);
            const float step = 4f;
            float prevX = -width * 0.5f, prevTop = Top(prevX);
            float Top(float x) => baseY + Mathf.PerlinNoise(x * 0.045f + seed * 13.7f, seed * 3.1f) * amplitude
                                  + Mathf.PerlinNoise(x * 0.17f + seed, 5f) * amplitude * 0.25f;
            for (float x = -width * 0.5f + step; x <= width * 0.5f + 0.01f; x += step)
            {
                float top = Top(x);
                b.Quad(new Vector3(prevX, -16f, 0f), new Vector3(x, -16f, 0f), new Vector3(x, top, 0f), new Vector3(prevX, prevTop, 0f), 0, Vector3.back);
                prevX = x; prevTop = top;
            }
            return b.ToMesh("Ridge_" + seed);
        }
    }
}
