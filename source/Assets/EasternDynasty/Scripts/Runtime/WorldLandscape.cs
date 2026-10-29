using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace EasternDynasty
{
    // Original procedural scenery. Game rules and Land IDs remain in World.
    internal sealed class WorldLandscape
    {
        private readonly World world;
        private readonly Transform root;
        private readonly Land[,] cells = new Land[World.Width, World.Height];
        private readonly List<Vector2> waterCenters = new List<Vector2>();
        private readonly List<Vector4> waterLinks = new List<Vector4>();
        private readonly List<Mesh> meshes = new List<Mesh>();
        private readonly List<Material> materials = new List<Material>();
        private readonly Material earthMaterial, propMaterial, waterMaterial;
        private Mesh ownershipMesh;
        private GameObject ownershipObject;
        private long buildMilliseconds;
        private int treeCount, townCount, fieldCount;

        public WorldLandscape(World state, Transform parent)
        {
            world = state; root = parent;
            foreach (var tile in world.lands) cells[tile.x, tile.z] = tile;
            foreach (var tile in world.lands)
            {
                if (tile.kind != LandKind.Water) continue;
                waterCenters.Add(new Vector2(tile.x, tile.z));
                for (int dz = 0; dz <= 1; dz++) for (int dx = -1; dx <= 1; dx++)
                {
                    if (dz == 0 && dx <= 0) continue;
                    var next = Cell(tile.x + dx, tile.z + dz);
                    if (next != null && next.kind == LandKind.Water)
                        waterLinks.Add(new Vector4(tile.x, tile.z, next.x, next.z));
                }
            }
            earthMaterial = MakeMaterial(1, .04f);
            propMaterial = MakeMaterial(0, .06f);
            waterMaterial = MakeMaterial(2, .58f);
        }

        private Material MakeMaterial(float detail, float smoothness)
        {
            Shader shader = Resources.Load<Shader>("WorldLandscapeLit");
            var material = new Material(shader != null ? shader : Shader.Find("Standard"));
            material.name = detail > 1.5f ? "River water" : detail > .5f ? "Continuous earth" : "Shared scenery";
            material.SetColor("_Color", Color.white);
            material.SetFloat("_Detail", detail); material.SetFloat("_Smoothness", smoothness);
            materials.Add(material); return material;
        }

        private Land Cell(int x, int z)
        { return x < 0 || z < 0 || x >= World.Width || z >= World.Height ? null : cells[x, z]; }

        private static Color C(string hex)
        { Color value; ColorUtility.TryParseHtmlString("#" + hex, out value); return value; }

        private static float Rand(int seed)
        { return Mathf.Repeat(Mathf.Sin(seed * 127.13f + 19.72f) * 43758.5453f, 1); }

        private static float SmoothRange(float from, float to, float value)
        { return Mathf.SmoothStep(0, 1, Mathf.InverseLerp(from, to, value)); }

        private float WaterDistance(float x, float z)
        {
            var p = new Vector2(x, z); float best = 999;
            foreach (var center in waterCenters) best = Mathf.Min(best, (p - center).sqrMagnitude);
            foreach (var link in waterLinks)
            {
                var a = new Vector2(link.x, link.y); var b = new Vector2(link.z, link.w);
                var v = b - a; float t = Mathf.Clamp01(Vector2.Dot(p - a, v) / v.sqrMagnitude);
                best = Mathf.Min(best, (p - a - v * t).sqrMagnitude);
            }
            return Mathf.Sqrt(best);
        }

        public float Height(float x, float z)
        {
            float h = .035f + Mathf.PerlinNoise(x * .18f + 71, z * .18f + 39) * .12f;
            h += Mathf.PerlinNoise(x * .73f + 13, z * .73f + 91) * .035f;
            int cx = Mathf.RoundToInt(x), cz = Mathf.RoundToInt(z);
            for (int dz = -1; dz <= 1; dz++) for (int dx = -1; dx <= 1; dx++)
            {
                var tile = Cell(cx + dx, cz + dz);
                if (tile == null || tile.kind != LandKind.Mountain) continue;
                float px = x - tile.x, pz = z - tile.z;
                float along = (px + pz * .55f) / 1.3f, across = (pz - px * .4f) / .7f;
                float weight = Mathf.Clamp01(1 - (along * along + across * across));
                h += weight * weight * .38f;
            }
            var nearest = Cell(cx, cz);
            if (nearest != null && (nearest.home || nearest.capital))
                h = Mathf.Lerp(h, .15f, 1 - SmoothRange(.42f, .68f, Mathf.Max(Mathf.Abs(x - cx), Mathf.Abs(z - cz))));
            float water = WaterDistance(x, z);
            if (water < .62f) h = Mathf.Lerp(-.105f, h, SmoothRange(.29f, .62f, water));
            return h;
        }

        private Vector3 Ground(float x, float z, float lift = 0)
        { return new Vector3(x, Height(x, z) + lift, z); }

        private Color GroundColor(float x, float z)
        {
            float macro = Mathf.PerlinNoise(x * .23f + 21, z * .23f + 43);
            float soil = SmoothRange(.46f, .70f, Mathf.PerlinNoise(x * .39f + 83, z * .39f + 7));
            Color han = Color.Lerp(C("677b63"), C("8a9677"), macro);
            han = Color.Lerp(han, C("8b876d"), soil * .38f);
            Color steppe = Color.Lerp(C("8b9573"), C("a6a27b"), macro);
            Color forest = Color.Lerp(C("596f5d"), C("82977c"), macro);
            Color color = Color.Lerp(han, steppe, SmoothRange(19, 23, z));
            color = Color.Lerp(color, forest, SmoothRange(24, 29, x) * SmoothRange(16, 20, z));
            float water = WaterDistance(x, z);
            if (water < .80f) color = Color.Lerp(C("a59b7c"), color, SmoothRange(.32f, .80f, water));
            return color;
        }

        public void Build()
        {
            var timer = System.Diagnostics.Stopwatch.StartNew();
            // A modest skirt continues the terrain beyond the playable coordinates.
            for (int cz = -1; cz < 4; cz++) for (int cx = -1; cx < 5; cx++)
            {
                bool inner = cx >= 0 && cx < 4 && cz >= 0 && cz < 3;
                BuildGround(cx * 9 - .5f, cz * 9 - .5f, inner ? 4 : 2, cx, cz);
            }
            var distant = new Geometry();
            distant.Quad(new Vector3(-70, -.24f, -70), new Vector3(-70, -.24f, 100), new Vector3(100, -.24f, 100), new Vector3(100, -.24f, -70), C("7f8c71"));
            Emit("Distant meadow", distant, earthMaterial, false);
            BuildWater();
            for (int cz = 0; cz < 3; cz++) for (int cx = 0; cx < 4; cx++)
            {
                var batch = new Geometry();
                for (int z = cz * 9; z < (cz + 1) * 9; z++) for (int x = cx * 9; x < (cx + 1) * 9; x++)
                {
                    var tile = Cell(x, z);
                    if (tile.home || tile.capital) BuildTown(batch, tile);
                    else BuildResource(batch, tile);
                }
                Emit("Settlement and vegetation " + cx + ":" + cz, batch, propMaterial, true);
            }
            timer.Stop(); buildMilliseconds = timer.ElapsedMilliseconds;
        }

        private void BuildGround(float startX, float startZ, int resolution, int cx, int cz)
        {
            var batch = new Geometry(); int side = 9 * resolution + 1;
            for (int iz = 0; iz < side; iz++) for (int ix = 0; ix < side; ix++)
            {
                float x = startX + ix / (float)resolution, z = startZ + iz / (float)resolution;
                Vector3 normal = new Vector3(Height(x - .04f, z) - Height(x + .04f, z), .08f, Height(x, z - .04f) - Height(x, z + .04f)).normalized;
                batch.Vertex(Ground(x, z), GroundColor(x, z), normal);
            }
            for (int z = 0; z < side - 1; z++) for (int x = 0; x < side - 1; x++)
            {
                int i = z * side + x;
                batch.Index(i, i + side, i + 1); batch.Index(i + 1, i + side, i + side + 1);
            }
            Emit("Continuous land " + cx + ":" + cz, batch, earthMaterial, false);
        }

        private void BuildWater()
        {
            var water = new Geometry();
            Color deep = C("426f75"), shallow = C("789b92");
            foreach (var center in waterCenters)
            {
                Vector3 p = new Vector3(center.x, -.015f, center.y);
                for (int i = 0; i < 20; i++)
                {
                    float a = i * Mathf.PI * .1f, b = (i + 1) * Mathf.PI * .1f;
                    water.Triangle(p, p + new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * .43f, p + new Vector3(Mathf.Sin(b), 0, Mathf.Cos(b)) * .43f, deep, shallow, shallow);
                }
            }
            foreach (var link in waterLinks)
            {
                Vector3 a = new Vector3(link.x, -.014f, link.y), b = new Vector3(link.z, -.014f, link.w);
                Vector3 side = Vector3.Cross((b - a).normalized, Vector3.up) * .42f;
                water.Quad(a - side, a, b, b - side, shallow, deep, deep, shallow);
                water.Quad(a, a + side, b + side, b, deep, shallow, shallow, deep);
            }
            Emit("Connected river surface", water, waterMaterial, false);
        }

        private void BuildResource(Geometry g, Land tile)
        {
            int seed = tile.id * 17 + 5;
            if (tile.kind == LandKind.Water) return;
            if (tile.kind == LandKind.Mountain)
            {
                for (int i = 0; i < 3; i++)
                {
                    float x = tile.x - .38f + i * .32f, z = tile.z - .18f + i * .15f;
                    g.Rock(Ground(x, z, -.08f), new Vector3(.46f, .58f + Rand(seed + i) * .48f, .37f), seed + i, C("7e897a"));
                }
                for (int i = 0; i < 3; i++) Tree(g, tile.x - .5f + i * .38f, tile.z - .48f, .45f, seed + i);
            }
            else if (tile.kind == LandKind.Stone || tile.kind == LandKind.Iron)
            {
                Color stone = tile.kind == LandKind.Iron ? C("5f7068") : C("919c8b");
                int rockCount = 2 + Mathf.FloorToInt(Rand(seed + 70) * 3);
                for (int i = 0; i < rockCount; i++)
                {
                    float x = tile.x + (Rand(seed + i * 3) - .5f) * .6f;
                    float z = tile.z + (Rand(seed + i * 3 + 1) - .5f) * .55f;
                    g.Rock(Ground(x, z, -.02f), new Vector3(.18f + Rand(seed + i) * .13f, .16f + Rand(seed + i + 9) * .16f, .18f), seed + i, stone);
                }
                if (tile.kind == LandKind.Iron)
                {
                    Vector3 p = Ground(tile.x + .18f, tile.z - .23f);
                    g.Box(p + Vector3.up * .09f, new Vector3(.25f, .18f, .20f), C("77644a"));
                    g.Roof(p + Vector3.up * .18f, .31f, .25f, .11f, C("525c54"));
                }
            }
            else if (tile.kind == LandKind.Grain && tile.homeland != Homeland.Steppe)
            {
                float cultivated = Mathf.PerlinNoise(tile.x * .27f + 37, tile.z * .27f + 8);
                bool field = Rand(seed + 9) < Mathf.Lerp(.08f, .79f, SmoothRange(.40f, .69f, cultivated));
                if (field) Fields(g, tile, seed);
                float woodland = Woodland(tile.x, tile.z);
                if (!field && woodland > .60f && Rand(seed + 15) < .38f)
                    Grove(g, tile, 2 + (woodland > .72f ? 1 : 0), seed);
            }
            else
            {
                float woodland = Woodland(tile.x, tile.z);
                int count = tile.homeland == Homeland.Steppe ? (woodland > .62f && Rand(seed + 19) < .3f ? 1 : 0)
                    : Mathf.RoundToInt(SmoothRange(.36f, .68f, woodland) * (tile.homeland == Homeland.Northeast ? 7 : 6));
                Grove(g, tile, count, seed);
                if (tile.homeland == Homeland.Steppe && tile.id % 7 == 0)
                {
                    Vector3 p = Ground(tile.x, tile.z);
                    g.Rock(p, new Vector3(.18f, .14f, .12f), seed, C("b2a47c"));
                    g.Rock(p + new Vector3(.22f, 0, .1f), new Vector3(.12f, .10f, .11f), seed + 1, C("a69a72"));
                }
            }
        }

        private void Fields(Geometry g, Land tile, int seed)
        {
            fieldCount++;
            float width = .32f + Rand(seed + 2) * .42f;
            float depth = .32f + Rand(seed + 3) * .40f;
            float angle = Mathf.PerlinNoise(tile.x * .18f + 11, tile.z * .18f + 62) * 110 - 55 + (Rand(seed + 6) - .5f) * 30;
            float yaw = angle * Mathf.Deg2Rad, sin = Mathf.Sin(yaw), cos = Mathf.Cos(yaw);
            Vector2 center = new Vector2(tile.x + (Rand(seed + 4) - .5f) * .30f, tile.z + (Rand(seed + 5) - .5f) * .30f);
            float skew = (Rand(seed + 7) - .5f) * .25f;
            Color crop = Color.Lerp(C("858e6b"), C("a19c73"), Rand(seed));
            Color earth = Color.Lerp(C("7b7c61"), crop, .30f);
            g.Quad(FieldPoint(center, width, depth, -.5f, -.5f, skew, sin, cos, .012f),
                FieldPoint(center, width, depth, -.5f, .5f, skew, sin, cos, .012f),
                FieldPoint(center, width, depth, .5f, .5f, skew, sin, cos, .012f),
                FieldPoint(center, width, depth, .5f, -.5f, skew, sin, cos, .012f), earth);
            int rows = 4 + Mathf.FloorToInt(Rand(seed + 1) * 4);
            for (int row = 0; row < rows; row++)
            {
                float a = -.46f + row * .92f / rows, b = a + .61f / rows;
                float low = -.43f + Rand(seed + row * 4) * .045f, high = .43f - Rand(seed + row * 3 + 1) * .045f;
                Color rowColor = Color.Lerp(crop, earth, Rand(seed + row + 11) * .24f);
                g.Quad(FieldPoint(center, width, depth, a, low, skew, sin, cos, .017f),
                    FieldPoint(center, width, depth, a, high, skew, sin, cos, .017f),
                    FieldPoint(center, width, depth, b, high, skew, sin, cos, .017f),
                    FieldPoint(center, width, depth, b, low, skew, sin, cos, .017f), rowColor);
            }
        }

        private Vector3 FieldPoint(Vector2 center, float width, float depth, float u, float v, float skew, float sin, float cos, float lift)
        {
            float x = (u * (.9f + v * .18f) + v * skew) * width, z = v * depth;
            return Ground(center.x + x * cos - z * sin, center.y + x * sin + z * cos, lift);
        }

        private static float Woodland(float x, float z)
        { return Mathf.PerlinNoise(x * .37f + 104, z * .37f + 41); }

        private void Grove(Geometry g, Land tile, int count, int seed)
        {
            // A shared low-frequency density field creates groves and open meadow between them.
            float centerX = tile.x + (Rand(seed + 50) - .5f) * .35f;
            float centerZ = tile.z + (Rand(seed + 51) - .5f) * .35f;
            for (int i = 0; i < count; i++)
            {
                float a = Rand(seed + i * 7 + 2) * Mathf.PI * 2;
                float r = Mathf.Sqrt(Rand(seed + i * 7 + 3)) * .46f;
                Tree(g, centerX + Mathf.Sin(a) * r, centerZ + Mathf.Cos(a) * r, .7f + Rand(seed + i * 7 + 4) * .43f, seed + i);
            }
        }

        private void Tree(Geometry g, float x, float z, float scale, int seed)
        {
            if (WaterDistance(x, z) < .5f) return;
            treeCount++;
            Vector3 p = Ground(x, z);
            g.Box(p + Vector3.up * (.17f * scale), new Vector3(.055f, .34f, .055f) * scale, C("6c6147"));
            Color leaf = Color.Lerp(C("355844"), C("6b7e56"), Rand(seed + 20));
            g.Rock(p + Vector3.up * (.21f * scale), new Vector3(.23f, .38f, .24f) * scale, seed, leaf, true);
            g.Rock(p + new Vector3(-.12f, .29f, .02f) * scale, new Vector3(.18f, .25f, .18f) * scale, seed + 6, Color.Lerp(leaf, C("849260"), .15f), true);
        }

        private void BuildTown(Geometry g, Land tile)
        {
            townCount++;
            Vector3 p = Ground(tile.x, tile.z, .015f);
            float scale = tile.home ? 1.2f : 1.08f;
            if (tile.homeland == Homeland.Steppe)
            {
                g.Disc(p + Vector3.up * .005f, .58f, C("b6ab83"));
                Yurt(g, p, .26f, C("e0cfaa"));
                Yurt(g, p + new Vector3(.31f, 0, .2f), .14f, C("cfc3a2"));
                Yurt(g, p + new Vector3(-.3f, 0, .19f), .15f, C("d8c49b"));
                for (int i = 0; i < 15; i++)
                {
                    float angle = .6f + i * .35f;
                    Vector3 q = p + new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)) * .55f;
                    g.Box(q + Vector3.up * .09f, new Vector3(.045f, .18f, .045f), C("897550"));
                }
            }
            else
            {
                Color wall = tile.homeland == Homeland.Northeast ? C("a79978") : C("c1ad88");
                Color roof = tile.home ? C("795245") : C("43584f");
                g.Box(p + new Vector3(0, .025f, 0), new Vector3(.99f, .05f, .99f) * scale, C("aea483"));
                g.Box(p + new Vector3(0, .14f, .42f) * scale, new Vector3(.9f, .26f, .09f) * scale, wall);
                g.Box(p + new Vector3(-.42f, .14f, 0) * scale, new Vector3(.09f, .26f, .9f) * scale, wall);
                g.Box(p + new Vector3(.42f, .14f, 0) * scale, new Vector3(.09f, .26f, .9f) * scale, wall);
                for (int side = -1; side <= 1; side += 2)
                    g.Box(p + new Vector3(side * .29f, .14f, -.42f) * scale, new Vector3(.31f, .26f, .09f) * scale, wall);
                for (int i = 0; i < 4; i++)
                {
                    Vector3 q = p + new Vector3(i % 2 == 0 ? -.42f : .42f, .25f, i < 2 ? -.42f : .42f) * scale;
                    g.Box(q, new Vector3(.17f, .40f, .17f) * scale, wall);
                    g.Roof(q + Vector3.up * .20f * scale, .26f * scale, .25f * scale, .12f * scale, roof);
                }
                for (int i = 0; i < 8; i++)
                {
                    float x = -.35f + i * .1f;
                    g.Box(p + new Vector3(x, .30f, .42f) * scale, new Vector3(.065f, .07f, .09f) * scale, wall);
                    if (Mathf.Abs(x) > .13f) g.Box(p + new Vector3(x, .30f, -.42f) * scale, new Vector3(.065f, .07f, .09f) * scale, wall);
                }
                // Gate passage stays open; lintel and overhanging roof give it a readable silhouette.
                g.Box(p + new Vector3(0, .29f, -.42f) * scale, new Vector3(.26f, .10f, .16f) * scale, wall);
                g.Roof(p + new Vector3(0, .34f, -.42f) * scale, .37f * scale, .29f * scale, .14f * scale, roof);
                House(g, p + new Vector3(0, .05f, .13f) * scale, .35f * scale, .32f * scale, .27f * scale, wall, roof);
                House(g, p + new Vector3(-.24f, .05f, -.12f) * scale, .18f * scale, .22f * scale, .16f * scale, wall, roof);
                House(g, p + new Vector3(.24f, .05f, -.10f) * scale, .18f * scale, .22f * scale, .17f * scale, wall, roof);
            }
            Banner(g, p + new Vector3(.26f, .31f, .06f), tile.home ? C("a34d37") : C("696f50"), .65f);
        }

        private static void House(Geometry g, Vector3 p, float width, float depth, float height, Color wall, Color roof)
        {
            g.Box(p + Vector3.up * height * .5f, new Vector3(width, height, depth), wall);
            g.Box(p + new Vector3(0, height * .3f, -depth * .503f), new Vector3(width * .27f, height * .6f, .008f), C("5e5949"));
            g.Roof(p + Vector3.up * height, width * 1.3f, depth * 1.25f, height * .47f, roof);
        }

        private static void Yurt(Geometry g, Vector3 p, float radius, Color cloth)
        {
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI / 6, b = (i + 1) * Mathf.PI / 6;
                Vector3 v0 = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * radius;
                Vector3 v1 = new Vector3(Mathf.Sin(b), 0, Mathf.Cos(b)) * radius;
                Vector3 up = Vector3.up * radius * .6f;
                g.Quad(p + v0, p + v1, p + v1 + up, p + v0 + up, cloth);
                g.Triangle(p + v0 + up, p + v1 + up, p + Vector3.up * radius * 1.15f, Color.Lerp(cloth, Color.white, .13f));
            }
            g.Box(p + new Vector3(0, radius * .23f, -radius), new Vector3(radius * .5f, radius * .46f, .012f), C("64513e"));
        }

        private static void Banner(Geometry g, Vector3 p, Color cloth, float height)
        {
            g.Box(p + Vector3.up * height * .5f, new Vector3(.025f, height, .025f), C("68563d"));
            g.Box(p + new Vector3(.105f, height * .79f, 0), new Vector3(.22f, .19f, .018f), cloth);
        }

        public Transform BuildArmy()
        {
            var geometry = new Geometry();
            Banner(geometry, Vector3.zero, C("ad593d"), .52f);
            for (int i = 0; i < 3; i++)
                geometry.Rock(new Vector3(-.12f + i * .12f, 0, -.06f), new Vector3(.055f, .11f, .055f), i + 41, C("5b6656"));
            return Emit("Marching Army", geometry, propMaterial, true).transform;
        }

        public void RefreshOwnership()
        {
            if (ownershipObject != null) { ownershipObject.SetActive(false); MapView.Release(ownershipObject); }
            if (ownershipMesh != null) { meshes.Remove(ownershipMesh); MapView.Release(ownershipMesh); }
            var geometry = new Geometry(); Color border = C("548d7a");
            foreach (var tile in world.lands)
            {
                if (!tile.owned) continue;
                for (int edge = 0; edge < 4; edge++)
                {
                    int dx = edge == 1 ? 1 : edge == 3 ? -1 : 0, dz = edge == 0 ? -1 : edge == 2 ? 1 : 0;
                    var neighbor = Cell(tile.x + dx, tile.z + dz);
                    if (neighbor != null && neighbor.owned) continue;
                    for (int j = 0; j < 4; j++)
                    {
                        float a = -.5f + j * .25f, b = a + .25f;
                        Vector3 p0, p1;
                        if (dx == 0) { p0 = Ground(tile.x + a, tile.z + dz * .49f, .025f); p1 = Ground(tile.x + b, tile.z + dz * .49f, .025f); }
                        else { p0 = Ground(tile.x + dx * .49f, tile.z + a, .025f); p1 = Ground(tile.x + dx * .49f, tile.z + b, .025f); }
                        Vector3 width = Vector3.Cross((p1 - p0).normalized, Vector3.up) * .014f;
                        geometry.Quad(p0 - width, p0 + width, p1 + width, p1 - width, border);
                    }
                }
            }
            ownershipObject = Emit("Owned territory border", geometry, propMaterial, false);
            ownershipMesh = ownershipObject == null ? null : ownershipObject.GetComponent<MeshFilter>().sharedMesh;
        }

        private GameObject Emit(string name, Geometry geometry, Material material, bool shadows)
        {
            if (geometry.vertices.Count == 0) return null;
            var mesh = new Mesh { name = name };
            if (geometry.vertices.Count > 65535) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(geometry.vertices); mesh.SetColors(geometry.colors);
            mesh.SetNormals(geometry.normals); mesh.SetTriangles(geometry.indices, 0); mesh.RecalculateBounds();
            meshes.Add(mesh);
            var obj = new GameObject(name); obj.transform.SetParent(root, false);
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = obj.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
            renderer.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            renderer.receiveShadows = true;
            return obj;
        }

        public string Statistics
        {
            get
            {
                int vertices = 0; long triangles = 0;
                foreach (var mesh in meshes) { vertices += mesh.vertexCount; triangles += mesh.GetIndexCount(0) / 3; }
                return "meshes=" + meshes.Count + ", shared materials=" + materials.Count + ", vertices=" + vertices + ", triangles=" + triangles + ", towns=" + townCount + ", trees=" + treeCount + ", fields=" + fieldCount + ", buildMs=" + buildMilliseconds;
            }
        }

        public void Dispose()
        {
            foreach (var mesh in meshes) MapView.Release(mesh);
            foreach (var material in materials) MapView.Release(material);
            meshes.Clear(); materials.Clear();
        }

        private sealed class Geometry
        {
            public readonly List<Vector3> vertices = new List<Vector3>();
            public readonly List<Vector3> normals = new List<Vector3>();
            public readonly List<Color32> colors = new List<Color32>();
            public readonly List<int> indices = new List<int>();
            public void Vertex(Vector3 p, Color color, Vector3 normal) { vertices.Add(p); colors.Add(color); normals.Add(normal); }
            public void Index(int a, int b, int c) { indices.Add(a); indices.Add(b); indices.Add(c); }
            public void Triangle(Vector3 a, Vector3 b, Vector3 c, Color color) { Triangle(a, b, c, color, color, color); }
            public void Triangle(Vector3 a, Vector3 b, Vector3 c, Color ca, Color cb, Color cc)
            {
                int start = vertices.Count; Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
                Vertex(a, ca, normal); Vertex(b, cb, normal); Vertex(c, cc, normal); Index(start, start + 1, start + 2);
            }
            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color) { Quad(a, b, c, d, color, color, color, color); }
            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color ca, Color cb, Color cc, Color cd)
            { Triangle(a, b, c, ca, cb, cc); Triangle(a, c, d, ca, cc, cd); }
            public void Box(Vector3 p, Vector3 size, Color color)
            {
                Vector3 h = size * .5f;
                Vector3 a = p + new Vector3(-h.x, -h.y, -h.z), b = p + new Vector3(h.x, -h.y, -h.z), c = p + new Vector3(h.x, -h.y, h.z), d = p + new Vector3(-h.x, -h.y, h.z);
                Vector3 up = Vector3.up * size.y;
                Quad(a, a + up, b + up, b, color); Quad(b, b + up, c + up, c, color);
                Quad(c, c + up, d + up, d, color); Quad(d, d + up, a + up, a, color);
                Quad(a + up, d + up, c + up, b + up, color);
            }
            public void Roof(Vector3 p, float width, float depth, float height, Color color)
            {
                Vector3 a = p + new Vector3(-width * .5f, .018f, -depth * .5f), b = p + new Vector3(width * .5f, .018f, -depth * .5f);
                Vector3 c = p + new Vector3(width * .5f, .018f, depth * .5f), d = p + new Vector3(-width * .5f, .018f, depth * .5f);
                Vector3 left = p + new Vector3(-width * .25f, height, 0), right = p + new Vector3(width * .25f, height, 0);
                Quad(a, left, right, b, color); Quad(c, right, left, d, color);
                Triangle(b, right, c, color); Triangle(d, left, a, color);
                Box(p + Vector3.up * height, new Vector3(width * .57f, .027f, .035f), Color.Lerp(color, Color.white, .13f));
                Box(p, new Vector3(width, .026f, depth), Color.Lerp(color, Color.black, .12f));
            }
            public void Disc(Vector3 p, float radius, Color color)
            {
                for (int i = 0; i < 20; i++)
                {
                    float a = i * Mathf.PI * .1f, b = (i + 1) * Mathf.PI * .1f;
                    Triangle(p, p + new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * radius, p + new Vector3(Mathf.Sin(b), 0, Mathf.Cos(b)) * radius, color);
                }
            }
            public void Rock(Vector3 p, Vector3 size, int seed, Color color, bool smooth = false)
            {
                const int sides = 7;
                for (int ring = 0; ring < 3; ring++) for (int i = 0; i < sides; i++)
                {
                    Vector3 a = RockPoint(ring, i, p, size, seed), b = RockPoint(ring, i + 1, p, size, seed);
                    Vector3 c = RockPoint(ring + 1, i + 1, p, size, seed), d = RockPoint(ring + 1, i, p, size, seed);
                    Color face = Color.Lerp(color, Color.white, Rand(seed + i * 4 + ring) * (smooth ? .025f : .07f));
                    if (smooth)
                    {
                        int start = vertices.Count;
                        Vertex(a, face, CrownNormal(a, p, size)); Vertex(b, face, CrownNormal(b, p, size));
                        Vertex(c, face, CrownNormal(c, p, size)); Vertex(d, face, CrownNormal(d, p, size));
                        Index(start, start + 1, start + 2); Index(start, start + 2, start + 3);
                    }
                    else Quad(a, b, c, d, face);
                }
                for (int i = 0; i < sides; i++)
                    Triangle(p + Vector3.up * size.y, RockPoint(3, i, p, size, seed), RockPoint(3, i + 1, p, size, seed), color);
            }
            private static Vector3 CrownNormal(Vector3 v, Vector3 p, Vector3 size)
            {
                Vector3 offset = v - p - Vector3.up * size.y * .43f;
                return new Vector3(offset.x / (size.x * size.x), offset.y / (size.y * size.y * .25f), offset.z / (size.z * size.z)).normalized;
            }
            private static Vector3 RockPoint(int ring, int side, Vector3 p, Vector3 size, int seed)
            {
                float angle = side % 7 * Mathf.PI * 2 / 7 + Rand(seed) * 6;
                float radius = ring == 0 ? .83f : ring == 1 ? 1 : ring == 2 ? .69f : .07f;
                radius *= .88f + Rand(seed + side % 7 * 5) * .23f;
                return p + new Vector3(Mathf.Sin(angle) * size.x * radius, ring / 3f * size.y, Mathf.Cos(angle) * size.z * radius);
            }
        }
    }
}
