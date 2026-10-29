using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace EasternDynasty
{
    // One logical tile is one world unit. Artwork is a camera-facing plane whose
    // ground centre matches the tile centre, not the bottom of its transparent canvas.
    internal sealed class YouzhouLandscape : IDisposable
    {
        private const int ChunkSize = 8;
        private static readonly string[] ArtNames = { "grain", "pasture", "stone", "iron", "city", "mountain", "forest" };
        // Calibrated against the delivered PNGs: their ground spans 96-99% of the
        // canvas, rather than the 85% requested in the generation brief.
        private static readonly float[] ArtWidths = { 1.36f, 1.36f, 1.36f, 1.38f, 1.45f, 1.38f, 1.38f };
        // S1 city derivative: ground side corners near (18,562)/(1512,557) on 1536x1024.
        private static readonly float[] GroundPivots = { .426f, .417f, .412f, .425f, .453f, .410f, .365f };
        private static readonly float[] VerticalScales = { 1, 1, 1, 1, .96f, 1, 1 };
        private const string ReferenceTextureId = "reference.youzhou.resources";
        private static readonly Vector2[][] ReferenceUvs = {
            SourceQuad(150,144,229,185,315,140,232,101),
            SourceQuad(154,48,233,91,317,43,237,3),
            SourceQuad(4,234,78,272,154,231,80,194),
            SourceQuad(76,193,155,231,230,187,155,148)
        };
        public static readonly Quaternion ViewRotation = Quaternion.Euler(30, 45, 0);
        private static readonly Vector3 ArtRight = ViewRotation * Vector3.right;
        private static readonly Vector3 ArtUp = ViewRotation * Vector3.up;
        private readonly World world;
        private readonly Transform root;
        private readonly List<Mesh> meshes = new List<Mesh>();
        private readonly List<Material> materials = new List<Material>();
        private readonly List<Chunk> chunks = new List<Chunk>();
        private readonly Plane[] frustum = new Plane[6];
        private readonly List<string> missing = new List<string>();
        private readonly float[] artAspects = new float[ArtNames.Length];
        private Material groundMaterial, spriteMaterial, overlayMaterial, bridgeMaterial;
        private Material referenceMaterial;
        private Texture2D atlas, bankDistanceTexture;
        private byte[] bankDistances;
        private Rect[] artUV;
        private int visibleChunks;
        private long buildMilliseconds;

        private sealed class Chunk
        {
            public int x, z;
            public Bounds bounds;
            public Transform root;
            public Mesh ownership;
            public Mesh resources;
            public Mesh bridges;
        }

        public YouzhouLandscape(World state, Transform parent) { world = state; root = parent; }
        public bool AssetsReady { get { return missing.Count == 0 && atlas != null && referenceMaterial != null && referenceMaterial.mainTexture != null; } }
        public string MissingAssets { get { return string.Join(", ", missing.ToArray()); } }
        public int VisibleChunkCount { get { return visibleChunks; } }

        public void Build()
        {
            var timer = System.Diagnostics.Stopwatch.StartNew();
            groundMaterial = MaterialFor("YouzhouGround");
            LoadRiverBanks();
            spriteMaterial = MaterialFor("YouzhouSprite");
            overlayMaterial = MaterialFor("YouzhouOverlay");
            bridgeMaterial = MaterialFor("YouzhouBridge");
            // Bridges belong below territory outlines and selected-cell feedback.
            bridgeMaterial.renderQueue = 2020;
            LoadAtlas();
            spriteMaterial.mainTexture = atlas;
            referenceMaterial=MaterialFor("YouzhouResource");
            referenceMaterial.mainTexture=GameAssets.LoadTexture(ReferenceTextureId);
            var skirt = new Geometry();
            skirt.GroundRect(-6, -6, world.MapWidth + 5, world.MapHeight + 5, -.02f, C("9b9b70"), new Vector2(0, 1));
            var skirtMaterial=MaterialFor("YouzhouGround");skirtMaterial.renderQueue=1800;
            Emit("Youzhou distant earth", skirt, skirtMaterial, root);
            for (int z = 0; z < world.MapHeight; z += ChunkSize)
                for (int x = 0; x < world.MapWidth; x += ChunkSize)
                    chunks.Add(new Chunk { x = x, z = z, bounds = new Bounds(new Vector3(x + 3.5f, .5f, z + 3.5f), new Vector3(11, 5, 11)) });
            timer.Stop(); buildMilliseconds = timer.ElapsedMilliseconds;
            if (!AssetsReady) Debug.LogWarning("Youzhou temporary art placeholders: " + MissingAssets);
        }

        private Material MaterialFor(string resource)
        {
            Shader shader = Resources.Load<Shader>(resource);
            if (shader == null) throw new InvalidOperationException("Required map shader missing: " + resource);
            if (!shader.isSupported) throw new InvalidOperationException("Map shader is unsupported: " + resource);
            var material = new Material(shader) { name = resource + " shared" };
            materials.Add(material); return material;
        }

        private void LoadRiverBanks()
        {
            // Generated display data from the actual immutable map geography, not
            // an imported painting or another source of passability/route rules.
            bankDistances = YouzhouRiverLayout.BuildBankDistances(world);
            bankDistanceTexture = new Texture2D(world.MapWidth * YouzhouRiverLayout.SamplesPerCell,
                world.MapHeight * YouzhouRiverLayout.SamplesPerCell, TextureFormat.R8, false, true)
            { name = "Youzhou actual river bank distances", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            bankDistanceTexture.LoadRawTextureData(bankDistances);
            bankDistanceTexture.Apply(false, false);
            groundMaterial.SetTexture("_BankDistance", bankDistanceTexture);
            groundMaterial.SetVector("_RiverMap", new Vector4(world.MapWidth, world.MapHeight, YouzhouRiverLayout.MaxBankDistance, 1));
        }

        private void LoadAtlas()
        {
            var copies = new Texture2D[ArtNames.Length];
            try
            {
                for (int i = 0; i < copies.Length; i++)
                {
                    var source = GameAssets.LoadTexture("map.youzhou." + ArtNames[i]);
                    if (source == null)
                    {
                        missing.Add(ArtNames[i]); copies[i] = Placeholder(i); artAspects[i] = 1.5f;
                    }
                    else
                    {
                        artAspects[i] = source.width / (float)source.height;
                        copies[i] = ReadableCopy(source);
                    }
                }
                atlas = new Texture2D(2, 2, TextureFormat.RGBA32, true, false) { name = "Youzhou runtime art atlas", filterMode = FilterMode.Trilinear, wrapMode = TextureWrapMode.Clamp, anisoLevel = 1 };
                artUV = atlas.PackTextures(copies, 8, 2048, false);
                atlas.Apply(true, true);
            }
            finally { foreach (var copy in copies) MapView.Release(copy); }
        }

        // Works with normal non-readable imported textures. One upload/readback per
        // source at setup, never while panning; authoring PNGs are left untouched.
        private static Texture2D ReadableCopy(Texture2D source)
        {
            int width = Mathf.Min(512, source.width);
            int height = Mathf.Max(1, Mathf.RoundToInt(width * source.height / (float)source.width));
            var target = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var previous = RenderTexture.active;
            try
            {
                Graphics.Blit(source, target);
                RenderTexture.active = target;
                var result = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
                result.ReadPixels(new Rect(0, 0, width, height), 0, 0); result.Apply();
                return result;
            }
            finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target); }
        }

        private static Texture2D Placeholder(int index)
        {
            var texture = new Texture2D(96, 64, TextureFormat.RGBA32, false);
            var pixels = new Color32[96 * 64];
            Color tint = index == 4 ? C("98694c") : index == 2 || index == 3 ? C("777369") : C("85935b");
            for (int y = 0; y < 64; y++) for (int x = 0; x < 96; x++)
            {
                float d = Mathf.Abs(x - 47.5f) / 42f + Mathf.Abs(y - 22f) / 20f;
                if (d < 1) pixels[y * 96 + x] = tint * (d > .91f ? .68f : 1);
            }
            texture.SetPixels32(pixels); texture.Apply(); return texture;
        }

        public void UpdateVisible(Camera camera)
        {
            spriteMaterial.SetFloat("_Overview",Mathf.InverseLerp(7f,15f,camera.orthographicSize));
            referenceMaterial.SetFloat("_Overview",Mathf.InverseLerp(7f,15f,camera.orthographicSize));
            GeometryUtility.CalculateFrustumPlanes(camera, frustum);
            visibleChunks = 0;
            foreach (var chunk in chunks)
            {
                bool visible = GeometryUtility.TestPlanesAABB(frustum, chunk.bounds);
                if (visible && chunk.root == null) BuildChunk(chunk);
                if (chunk.root != null && chunk.root.gameObject.activeSelf != visible) chunk.root.gameObject.SetActive(visible);
                if (visible) visibleChunks++;
            }
        }

        private void BuildChunk(Chunk chunk)
        {
            chunk.root = new GameObject("Youzhou chunk " + chunk.x + "," + chunk.z).transform;
            chunk.root.SetParent(root, false);
            var ground = new Geometry(); var art = new Geometry(); var references=new Geometry(); var bridges = new Geometry();
            for (int z = chunk.z; z < Mathf.Min(world.MapHeight, chunk.z + ChunkSize); z++)
                for (int x = chunk.x; x < Mathf.Min(world.MapWidth, chunk.x + ChunkSize); x++)
                {
                    var tile = world.TryGetLand(x, z);
                    if (tile == null) continue;
                    bool water = YouzhouRiverLayout.IsWater(tile);
                    // Shared meadow continues between resource patches instead of
                    // making every resource kind a different solid checkerboard.
                    Color tint = C("92976e");
                    bool district = world.IsCityDistrict(tile.id);
                    if (district) tint = C("92966a");
                    ground.GroundRect(x - .5f, z - .5f, x + .5f, z + .5f, 0, tint, new Vector2(water ? 1 : 0, 0));
                    if (tile.bridge) { AddBridge(bridges, tile); continue; }
                    if (tile.kind == LandKind.Water) continue;
                    // The city has an actual nine-cell district. Keep its apron
                    // clear instead of presenting buildable city plots as farmland.
                    if (district && !tile.home) continue;
                    int image = ArtIndex(tile);
                    YouzhouTerrainLayout.Patch patch;
                    if(YouzhouTerrainLayout.TryPatch(world,tile,out patch)) AddReferenceTile(references,patch);
                    else AddArt(art, new Vector3(x, .015f, z), image, Color.white, tile.home ? 1.7f : 1);
                }
            Emit("Continuous meadow and water", ground, groundMaterial, chunk.root);
            Emit("Resource and city art", art, spriteMaterial, chunk.root);
            chunk.resources=Emit("Historical resource patches", references, referenceMaterial, chunk.root).GetComponent<MeshFilter>().sharedMesh;
            if (bridges.Count > 0) chunk.bridges = Emit("Raised crossings and river supports", bridges, bridgeMaterial, chunk.root).GetComponent<MeshFilter>().sharedMesh;
            var territory = new Geometry(); FillOwnership(chunk, territory);
            var ownerObject = Emit("Territory fill and edges", territory, overlayMaterial, chunk.root);
            chunk.ownership = ownerObject.GetComponent<MeshFilter>().sharedMesh;
        }

        private int ArtIndex(Land tile)
        {
            if (tile.home || tile.capital) return 4;
            if (tile.kind == LandKind.Grain) return 0;
            if (tile.kind == LandKind.Pasture) return 1;
            if (tile.kind == LandKind.Stone) return 2;
            if (tile.kind == LandKind.Iron) return 3;
            // Forest is a wooded mountain-edge variant, not a new resource or passable tile.
            if (tile.kind == LandKind.Mountain)
            {
                int adjacent = 0;
                for (int k = 0; k < 4; k++)
                {
                    var n = world.TryGetLand(tile.x + (k == 0 ? 1 : k == 1 ? -1 : 0), tile.z + (k == 2 ? 1 : k == 3 ? -1 : 0));
                    if (n != null && n.kind == LandKind.Mountain) adjacent++;
                }
                return adjacent <= 2 && (tile.x * 7 + tile.z * 11) % 3 == 0 ? 6 : 5;
            }
            return 1;
        }
        // Measured source-pixel corners in the local RAG's original 320x320 resource image.
        // Rendering UVs leave the archived image untouched; levels are gameplay overlays, not source claims.
        private static void AddReferenceTile(Geometry geometry,YouzhouTerrainLayout.Patch patch)
        {
            geometry.TexturedQuad(new Vector3(patch.xMin,.032f,patch.zMax),new Vector3(patch.xMin,.032f,patch.zMin),new Vector3(patch.xMax,.032f,patch.zMin),new Vector3(patch.xMax,.032f,patch.zMax),Color.white,ReferenceUvs[patch.image]);
        }
        private static Vector2[] SourceQuad(params float[] pixels)
        {
            var result=new Vector2[4];
            for(int i=0;i<4;i++)result[i]=new Vector2(pixels[i*2]/320f,1-pixels[i*2+1]/320f);
            // Keep screenshot ownership outlines from looking like live player ownership.
            Vector2 centre=(result[0]+result[1]+result[2]+result[3])*.25f;
            for(int i=0;i<4;i++)result[i]=Vector2.Lerp(centre,result[i],.96f);
            return result;
        }

        private void AddArt(Geometry geometry, Vector3 centre, int image, Color color, float scale = 1)
        {
            float width = ArtWidths[image] * scale;
            float height = width / artAspects[image] * VerticalScales[image];
            Vector3 horizontal = ArtRight * width, vertical = ArtUp * height;
            // City ground is a taller diamond than the other images; its vertical
            // correction aligns the footprint with the same 2:1 projected grid.
            Vector3 bottomLeft = centre - horizontal * .5f - vertical * GroundPivots[image];
            geometry.Quad(bottomLeft, bottomLeft + horizontal, bottomLeft + horizontal + vertical, bottomLeft + vertical, color, artUV[image], new Vector2(image>=4?1:0,0));
        }

        private void AddBridge(Geometry geometry, Land tile)
        {
            // S1's actual crossings run east/west. Calculate the entire connected
            // span so adjoining cells and separately built chunks share one arch.
            float start, end;
            if(!YouzhouRiverLayout.TryBridgeSpan(world,tile,out start,out end))return;
            int left=Mathf.RoundToInt(start+.5f),right=Mathf.RoundToInt(end-.5f);
            float x = tile.x, z = tile.z;
            if (tile.x == left) BridgeBox(geometry, start - .07f, start + .08f, z - .28f, z + .28f, .015f, .045f, C("95876b"));
            if (tile.x == right) BridgeBox(geometry, end - .08f, end + .07f, z - .28f, z + .28f, .015f, .045f, C("95876b"));
            // A single central pier per joint; shallow sides stay visibly open.
            if (tile.x < right) BridgeBox(geometry, x + .44f, x + .56f, z - .25f, z + .25f, .015f,
                YouzhouRiverLayout.BridgeDeckHeight(x + .5f, start, end) - .025f, C("706b59"));
            for (int i = 0; i < 8; i++)
            {
                float a = x - .5f + i * .125f, b = a + .125f;
                float ha = YouzhouRiverLayout.BridgeDeckHeight(a, start, end), hb = YouzhouRiverLayout.BridgeDeckHeight(b, start, end);
                float halfWidth=YouzhouRiverLayout.BridgeDeckHalfWidth;
                BridgeStrip(geometry, a, b, z - halfWidth, z + halfWidth, ha - .026f, hb - .026f, .026f, C("66513b"));
                BridgeStrip(geometry, a + .006f, b - .006f, z - halfWidth, z + halfWidth, ha, hb, .009f,
                    i % 3 == 0 ? C("ab8c61") : C("bfa479"));
                for (int side = -1; side <= 1; side += 2)
                {
                    float railZ = z + side * YouzhouRiverLayout.BridgeRailOffset;
                    float railHalf=YouzhouRiverLayout.BridgeRailHalfWidth,postHalf=YouzhouRiverLayout.BridgePostHalfWidth;
                    BridgeStrip(geometry, a, b, railZ - railHalf, railZ + railHalf,
                        ha + YouzhouRiverLayout.BridgeRailRise, hb + YouzhouRiverLayout.BridgeRailRise, YouzhouRiverLayout.BridgeRailThickness, C("958467"));
                    if (i % 2 == 0)
                        BridgeBox(geometry, a - postHalf, a + postHalf, railZ - postHalf, railZ + postHalf,
                            ha, ha + YouzhouRiverLayout.BridgePostHeight, C("c0af8a"));
                }
            }
            if (tile.x == right) for (int side = -1; side <= 1; side += 2)
                BridgeBox(geometry, end - YouzhouRiverLayout.BridgePostHalfWidth, end + YouzhouRiverLayout.BridgePostHalfWidth,
                    z + side * YouzhouRiverLayout.BridgeRailOffset - YouzhouRiverLayout.BridgePostHalfWidth,
                    z + side * YouzhouRiverLayout.BridgeRailOffset + YouzhouRiverLayout.BridgePostHalfWidth,
                    YouzhouRiverLayout.BridgeDeckHeight(end,start,end),
                    YouzhouRiverLayout.BridgeDeckHeight(end,start,end) + YouzhouRiverLayout.BridgePostHeight, C("c0af8a"));
        }

        private static void BridgeBox(Geometry g, float x0, float x1, float z0, float z1, float bottom, float top, Color color)
        { BridgeStrip(g, x0, x1, z0, z1, bottom, bottom, top - bottom, color); }

        private static void BridgeStrip(Geometry g, float x0, float x1, float z0, float z1, float h0, float h1, float thickness, Color color)
        {
            var a = new Vector3(x0,h0,z0); var b = new Vector3(x1,h1,z0);
            var c = new Vector3(x1,h1,z1); var d = new Vector3(x0,h0,z1);
            var rise = Vector3.up * thickness; var uv = new Rect(0,0,1,1);
            g.Quad(a,b,b+rise,a+rise,color*.72f,uv,Vector2.zero);
            g.Quad(b,c,c+rise,b+rise,color*.82f,uv,Vector2.zero);
            g.Quad(c,d,d+rise,c+rise,color*.88f,uv,Vector2.zero);
            g.Quad(d,a,a+rise,d+rise,color*.76f,uv,Vector2.zero);
            g.Quad(a+rise,b+rise,c+rise,d+rise,color,uv,Vector2.zero);
        }

        public void RefreshOwnership()
        {
            foreach (var chunk in chunks)
            {
                if (chunk.ownership == null) continue;
                var geometry = new Geometry(); FillOwnership(chunk, geometry);
                geometry.Apply(chunk.ownership);
            }
        }

        private void FillOwnership(Chunk chunk, Geometry geometry)
        {
            // The ordinary-map reference uses readable green territory edges. A
            // light tint preserves the source artwork; gold is reserved for selection.
            Color fill = new Color(.54f, .79f, .24f, .055f), edge = C("b7e16a");
            Color inner = new Color(.64f, .86f, .40f, .56f), glow = new Color(.45f, .73f, .22f, .23f);
            Color cityEdge = C("d5ed81");
            for (int z = chunk.z; z < Mathf.Min(world.MapHeight, chunk.z + ChunkSize); z++)
                for (int x = chunk.x; x < Mathf.Min(world.MapWidth, chunk.x + ChunkSize); x++)
                {
                    var tile = world.TryGetLand(x, z); if (tile == null || !tile.owned) continue;
                    geometry.GroundRect(x - .485f, z - .485f, x + .485f, z + .485f, .045f, fill, Vector2.zero);
                    bool district = world.IsCityDistrict(tile.id);
                    for (int k = 0; k < 4; k++)
                    {
                        int dx = k == 0 ? 1 : k == 1 ? -1 : 0, dz = k == 2 ? 1 : k == 3 ? -1 : 0;
                        var neighbor = world.TryGetLand(x + dx, z + dz);
                        bool outer = neighbor == null || !neighbor.owned;
                        bool cityBoundary = district && (neighbor == null || !world.IsCityDistrict(neighbor.id));
                        if (!outer && !cityBoundary && k % 2 != 0) continue;
                        Color color = cityBoundary ? cityEdge : outer ? edge : inner;
                        float width = outer || cityBoundary ? .018f : .006f;
                        if (outer || cityBoundary) AddTerritoryEdge(geometry, x, z, dx, dz, .057f, .047f, glow);
                        AddTerritoryEdge(geometry, x, z, dx, dz, width, .049f, color);
                        if (outer) AddTerritoryEdge(geometry, x, z, dx, dz, .005f, .051f, new Color(.83f, .96f, .63f, .92f));
                    }
                    if (world.IsGuarded(tile.id))
                    {
                        // Cyan corner marks distinguish defensive coverage from ownership.
                        for (int sx = -1; sx <= 1; sx += 2) for (int sz = -1; sz <= 1; sz += 2)
                        {
                            float ax = x + sx * .39f, az = z + sz * .39f;
                            var guarded = new Color(.28f, .87f, .94f, .88f);
                            geometry.GroundRect(Mathf.Min(ax, ax - sx * .15f), az - .012f, Mathf.Max(ax, ax - sx * .15f), az + .012f, .056f, guarded, Vector2.zero);
                            geometry.GroundRect(ax - .012f, Mathf.Min(az, az - sz * .15f), ax + .012f, Mathf.Max(az, az - sz * .15f), .056f, guarded, Vector2.zero);
                        }
                    }
                }
        }

        private static void AddTerritoryEdge(Geometry geometry, float x, float z, int dx, int dz, float halfWidth, float y, Color color)
        {
            if (dx != 0) geometry.GroundRect(x + dx * .49f - halfWidth, z - .50f, x + dx * .49f + halfWidth, z + .50f, y, color, Vector2.zero);
            else geometry.GroundRect(x - .50f, z + dz * .49f - halfWidth, x + .50f, z + dz * .49f + halfWidth, y, color, Vector2.zero);
        }

        // Called only by the read-only runtime validation. Check the meshes the
        // renderer actually owns, not synthetic UI targets or a second drawing.
        public void AssertResourceGeometry()
        {
            if (referenceMaterial == null || referenceMaterial.shader.name != "EasternDynasty/YouzhouResource"
                || !referenceMaterial.shader.isSupported || referenceMaterial.mainTexture == null)
                throw new InvalidOperationException("Resource terrain shader/texture unavailable.");
            int examined = 0;
            foreach (var chunk in chunks)
            {
                if (chunk.resources == null) continue;
                Vector3[] vertices = chunk.resources.vertices;
                Vector2[] coords = chunk.resources.uv, local = chunk.resources.uv2;
                int index = 0;
                for (int z = chunk.z; z < Math.Min(world.MapHeight, chunk.z + ChunkSize); z++)
                    for (int x = chunk.x; x < Math.Min(world.MapWidth, chunk.x + ChunkSize); x++)
                    {
                        YouzhouTerrainLayout.Patch patch;
                        if (!YouzhouTerrainLayout.TryPatch(world, world.TryGetLand(x,z), out patch)) continue;
                        var expected = new[] { new Vector3(patch.xMin,.032f,patch.zMax), new Vector3(patch.xMin,.032f,patch.zMin),
                            new Vector3(patch.xMax,.032f,patch.zMin), new Vector3(patch.xMax,.032f,patch.zMax) };
                        if (index + 4 > vertices.Length || local.Length != vertices.Length || coords.Length != vertices.Length)
                            throw new InvalidOperationException("Terrain patch missing visible mesh vertices or feather coordinates.");
                        for (int i = 0; i < 4; i++)
                            if ((vertices[index+i]-expected[i]).sqrMagnitude > .000001f
                                || (coords[index+i]-ReferenceUvs[patch.image][i]).sqrMagnitude > .000001f)
                                throw new InvalidOperationException("Terrain patch changed its land coordinate or source identity.");
                        if (local[index] != Vector2.zero || local[index+2] != Vector2.one)
                            throw new InvalidOperationException("Terrain feather coordinates were replaced by atlas coordinates.");
                        index += 4; examined++;
                    }
                if (index != vertices.Length) throw new InvalidOperationException("Extra terrain patches cover a river, city or mountain.");
            }
            if (examined == 0) throw new InvalidOperationException("No actual resource terrain meshes were examined.");
        }

        public void AssertRiverGeometry(bool requireBridge)
        {
            int resolution = YouzhouRiverLayout.SamplesPerCell;
            if (bankDistanceTexture == null || bankDistanceTexture.width != world.MapWidth * resolution
                || bankDistanceTexture.height != world.MapHeight * resolution || bankDistanceTexture.mipmapCount != 1
                || bankDistanceTexture.filterMode != FilterMode.Bilinear || bankDistanceTexture.wrapMode != TextureWrapMode.Clamp
                || groundMaterial.GetTexture("_BankDistance") != bankDistanceTexture
                || groundMaterial.GetVector("_RiverMap") != new Vector4(world.MapWidth, world.MapHeight, YouzhouRiverLayout.MaxBankDistance, 1))
                throw new InvalidOperationException("River material lost its real map distance field or world alignment.");
            if (bridgeMaterial.shader.name != "EasternDynasty/YouzhouBridge" || !bridgeMaterial.shader.isSupported
                || bridgeMaterial.renderQueue >= overlayMaterial.renderQueue)
                throw new InvalidOperationException("Bridge structure shader or selection overlay order is unavailable.");
            // Compare real uploaded bytes at every water/bridge cell centre and
            // edge. This detects flipped, gamma-corrected or stale map textures.
            foreach (var tile in world.lands)
            {
                if (!YouzhouRiverLayout.IsWater(tile)) continue;
                foreach (int offset in new[] { 0, resolution / 2, resolution - 1 })
                {
                    int px = tile.x * resolution + offset, pz = tile.z * resolution + resolution / 2;
                    float actual = bankDistanceTexture.GetPixel(px,pz).r * YouzhouRiverLayout.MaxBankDistance;
                    float expected = YouzhouRiverLayout.BankDistance(world, (px + .5f) / resolution - .5f, (pz + .5f) / resolution - .5f);
                    if (Mathf.Abs(actual - expected) > YouzhouRiverLayout.MaxBankDistance / 255f + .0001f)
                        throw new InvalidOperationException("Uploaded river field disagrees with actual bank geometry.");
                }
            }
            int bridges = 0;
            foreach (var chunk in chunks)
            {
                if (chunk.root == null) continue;
                int count = 0;
                for (int z=chunk.z;z<Math.Min(world.MapHeight,chunk.z+ChunkSize);z++)
                    for (int x=chunk.x;x<Math.Min(world.MapWidth,chunk.x+ChunkSize);x++)
                        if(world.TryGetLand(x,z).bridge) count++;
                if (count == 0 && chunk.bridges != null || count > 0 && chunk.bridges == null)
                    throw new InvalidOperationException("Bridge mesh does not match actual crossing cells.");
                if (count == 0) continue;
                if (chunk.bridges.vertexCount < count * 800 || chunk.bridges.bounds.min.y < 0
                    || chunk.bridges.bounds.max.y < .17f || chunk.bridges.bounds.max.y > .20f)
                    throw new InvalidOperationException("Bridge deck, support or raised rail geometry is missing.");
                foreach (var vertex in chunk.bridges.vertices)
                {
                    var tile=world.TryGetLand(Mathf.FloorToInt(vertex.x+.5f),Mathf.FloorToInt(vertex.z+.5f));
                    // End abutments overlap dry banks by at most .07 units. The
                    // span may never fabricate a crossing over another water cell.
                    if (tile==null || YouzhouRiverLayout.IsWater(tile)&&!tile.bridge)
                        throw new InvalidOperationException("Bridge artwork extends into a non-crossing water cell.");
                    float shift=vertex.y*Mathf.Sqrt(1.5f);
                    var projected=world.TryGetLand(Mathf.FloorToInt(vertex.x+shift+.5f),Mathf.FloorToInt(vertex.z+shift+.5f));
                    if(projected==null||YouzhouRiverLayout.IsWater(projected)&&!projected.bridge)
                        throw new InvalidOperationException("Visible bridge structure projects onto a non-crossing water pick cell.");
                }
                bridges += count;
            }
            if (requireBridge && bridges == 0) throw new InvalidOperationException("No real bridge mesh was examined in the river view.");
        }

        public Transform BuildArmy()
        {
            var geometry = new Geometry();
            Vector3 r = ArtRight, u = ArtUp;
            // Small grouped infantry silhouettes make the unit readable at tactical zoom.
            for(int row=0;row<3;row++)for(int col=0;col<3;col++)
            {
                Vector3 foot=r*((col-1)*.11f)+u*(row*.045f-.08f);
                geometry.Quad(foot-r*.028f,foot+r*.028f,foot+r*.023f+u*.12f,foot-r*.023f+u*.12f,C("493c30"),new Rect(0,0,1,1),Vector2.zero);
                geometry.Quad(foot-r*.023f+u*.115f,foot+r*.023f+u*.115f,foot+r*.017f+u*.155f,foot-r*.017f+u*.155f,C("b5a384"),new Rect(0,0,1,1),Vector2.zero);
                geometry.Quad(foot+r*.034f,foot+r*.042f,foot+r*.042f+u*.21f,foot+r*.034f+u*.21f,C("dbca9e"),new Rect(0,0,1,1),Vector2.zero);
            }
            geometry.Quad(-r * .012f, r * .012f, r * .012f + u * .48f, -r * .012f + u * .48f, C("e0c79b"), new Rect(0, 0, 1, 1), Vector2.zero);
            geometry.Quad(u * .27f, r * .23f + u * .30f, r * .23f + u * .47f, u * .47f, C("a43d2a"), new Rect(0, 0, 1, 1), Vector2.zero);
            return Emit("Marching army banner", geometry, overlayMaterial, root).transform;
        }

        private GameObject Emit(string name, Geometry geometry, Material material, Transform parent)
        {
            var mesh = new Mesh { name = name }; geometry.Apply(mesh); meshes.Add(mesh);
            var obj = new GameObject(name); obj.transform.SetParent(parent, false);
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = obj.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
            return obj;
        }

        public string Statistics
        {
            get
            {
                int vertices = 0; long triangles = 0;
                foreach (var mesh in meshes) { vertices += mesh.vertexCount; triangles += mesh.GetIndexCount(0) / 3; }
                return "chunks=" + chunks.Count + ", visible=" + visibleChunks + ", built meshes=" + meshes.Count + ", shared materials=" + materials.Count
                    + ", vertices=" + vertices + ", triangles=" + triangles + ", art=" + (ArtNames.Length - missing.Count) + "/" + ArtNames.Length
                    + ", atlas=" + (atlas == null ? "none" : atlas.width + "x" + atlas.height) + ", reference=" + (referenceMaterial != null && referenceMaterial.mainTexture != null ? "S1-RES-004" : "missing")
                    + ", terrain=" + YouzhouTerrainLayout.Style + ", river=" + YouzhouRiverLayout.Style
                    + ", city=reference derivative, setupMs=" + buildMilliseconds + ", bridge art=raised original geometry";
            }
        }

        public void Dispose()
        {
            foreach (var mesh in meshes) MapView.Release(mesh);
            foreach (var material in materials) MapView.Release(material);
            MapView.Release(atlas); MapView.Release(bankDistanceTexture);
            meshes.Clear(); materials.Clear(); chunks.Clear(); atlas = null; bankDistanceTexture = null; bankDistances = null;
        }

        private static Color C(string hex) { Color value; ColorUtility.TryParseHtmlString("#" + hex, out value); return value; }

        private sealed class Geometry
        {
            private readonly List<Vector3> vertices = new List<Vector3>();
            private readonly List<Color32> colors = new List<Color32>();
            private readonly List<Vector2> uv = new List<Vector2>(), data = new List<Vector2>();
            private readonly List<int> indices = new List<int>();
            public int Count { get { return vertices.Count; } }
            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color color, Rect texture, Vector2 extra)
            {
                int start = vertices.Count;
                vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
                for (int i = 0; i < 4; i++) { colors.Add(color); data.Add(extra); }
                uv.Add(new Vector2(texture.xMin, texture.yMin)); uv.Add(new Vector2(texture.xMax, texture.yMin));
                uv.Add(new Vector2(texture.xMax, texture.yMax)); uv.Add(new Vector2(texture.xMin, texture.yMax));
                indices.Add(start); indices.Add(start + 1); indices.Add(start + 2); indices.Add(start); indices.Add(start + 2); indices.Add(start + 3);
            }
            public void GroundRect(float x0, float z0, float x1, float z1, float y, Color color, Vector2 extra)
            { Quad(new Vector3(x0, y, z0), new Vector3(x1, y, z0), new Vector3(x1, y, z1), new Vector3(x0, y, z1), color, new Rect(0, 0, 1, 1), extra); }
            public void TexturedQuad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Color color,Vector2[] coords)
            {
                int first=vertices.Count;vertices.AddRange(new[]{a,b,c,d});
                var local=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(0,1)};
                for(int i=0;i<4;i++){colors.Add(color);data.Add(local[i]);uv.Add(coords[i]);}
                indices.AddRange(new[]{first,first+1,first+2,first,first+2,first+3});
            }
            public void Apply(Mesh mesh)
            {
                mesh.Clear(); mesh.SetVertices(vertices); mesh.SetColors(colors); mesh.SetUVs(0, uv); mesh.SetUVs(1, data);
                mesh.SetTriangles(indices, 0); mesh.RecalculateBounds();
            }
        }
    }
}
