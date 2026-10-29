using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace EasternDynasty
{
    public sealed class LandHit : MonoBehaviour { public int id; }

    public sealed partial class MapView : MonoBehaviour
    {
        public Camera viewCamera;
        public World world;
        public int selected = -1;
        public Vector3 focus;
        public float zoom = 7.2f;
        public Rect screenArea;
        public Rect[] blockedRects = new Rect[0];
        public bool inputEnabled;
        public bool edgeScrollEnabled = true;
        public System.Action<int> LandClicked;
        public bool IsDragging { get { return pointer.Dragging || mobileGesture.Dragging || mobileGesture.Pinching; } }
        private Transform mapRoot, army;
        private WorldLandscape landscape;
        private YouzhouLandscape youzhou;
        private readonly List<Material> materials = new List<Material>();
        private LineRenderer selection, route, preview;
        private readonly LineRenderer[] selectionCorners = new LineRenderer[4];
        private readonly Vector3[] outlinePoints = new Vector3[32];
        private readonly Vector3[] routePoints = new Vector3[32];
        private int knownOwned = -1;
        private int knownGuard = -1;
        private int knownCityVersion = -1;
        private readonly MapPointerGesture pointer = new MapPointerGesture();
        private readonly bool reviewMouseTrace = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-edReview") >= 0;
        private int reviewMouseTraceCount;
        private Vector2 reviewPreviousMouse;
        private bool reviewHasPreviousMouse;
        private int previewTarget = -2;
        private List<int> previewPath;
        public bool YouzhouAssetsReady { get { return youzhou != null && youzhou.AssetsReady; } }
        public string MissingYouzhouAssets { get { return youzhou == null ? "Youzhou is not active" : youzhou.MissingAssets; } }
        public string RenderStatistics { get { return youzhou != null ? youzhou.Statistics : landscape != null ? landscape.Statistics : "No map"; } }
        public void AssertYouzhouTerrainGeometry()
        {
            if (youzhou == null) throw new System.InvalidOperationException("S1 terrain is not active.");
            youzhou.AssertResourceGeometry();
        }
        public void AssertYouzhouRiverGeometry(bool requireBridge)
        {
            if (youzhou == null) throw new System.InvalidOperationException("S1 river is not active.");
            youzhou.AssertRiverGeometry(requireBridge);
        }

        public void Setup(World state)
        {
            CancelPointerInput();
            ClearMap(); world = state;
            if (state == null) return;
            mapRoot = new GameObject("Eastern Dynasty Landscape").transform;
            mapRoot.SetParent(transform, false);
            if (viewCamera == null)
            {
                viewCamera = new GameObject("Map Camera").AddComponent<Camera>();
                viewCamera.transform.SetParent(transform, false);
            }
            viewCamera.orthographic = true;
            viewCamera.clearFlags = CameraClearFlags.SolidColor;
            viewCamera.backgroundColor = ColorHex(state.IsYouzhou ? "9b9b70" : "7d8d77");
            viewCamera.nearClipPlane = .1f; viewCamera.farClipPlane = 200;
            if (state.IsYouzhou)
            {
                youzhou = new YouzhouLandscape(state, mapRoot); youzhou.Build();
            }
            else { ConfigureLighting(); landscape = new WorldLandscape(state, mapRoot); landscape.Build(); }
            selection = MakeLine("Selected land", ColorHex("f7ce72"), .038f, true);
            route = MakeLine("March route", ColorHex("ffe2a0"), .038f, false);
            preview = MakeLine("Selected route preview", new Color(.42f, .88f, .90f, .65f), .018f, false);
            if (state.IsYouzhou)
                for (int i = 0; i < selectionCorners.Length; i++)
                    selectionCorners[i] = MakeLine("Selected corner " + i, ColorHex("fff1ba"), .045f, false);
            army = youzhou != null ? youzhou.BuildArmy() : landscape.BuildArmy(); army.gameObject.SetActive(false);
            zoom = state.IsYouzhou ? 4.2f : 7.2f; CenterHome(); RefreshOwnership(); PositionCamera();
            if (youzhou != null) youzhou.UpdateVisible(viewCamera);
            Debug.Log("Landscape: " + RenderStatistics + "; pick colliders=0; picking=ground plane");
        }

        private void ConfigureLighting()
        {
            var sun = new GameObject("Warm afternoon sun").AddComponent<Light>();
            sun.transform.SetParent(mapRoot, false); sun.transform.rotation = Quaternion.Euler(48, -38, 0);
            sun.type = LightType.Directional; sun.color = new Color(1f, .97f, .91f); sun.intensity = .78f;
            sun.shadows = LightShadows.Soft; sun.shadowStrength = .56f; sun.shadowBias = .035f; sun.shadowNormalBias = .2f;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = ColorHex("6e7b83");
            RenderSettings.ambientEquatorColor = ColorHex("5e655c");
            RenderSettings.ambientGroundColor = ColorHex("38382f");
            RenderSettings.ambientIntensity = .65f;
            QualitySettings.shadowDistance = 70;
        }

        private static Color ColorHex(string value)
        { Color color; ColorUtility.TryParseHtmlString("#" + value, out color); return color; }

        private LineRenderer MakeLine(string title, Color color, float width, bool loop)
        {
            var obj = new GameObject(title); obj.transform.SetParent(mapRoot, false);
            var line = obj.AddComponent<LineRenderer>();
            Shader shader = Resources.Load<Shader>(world != null && world.IsYouzhou ? "YouzhouOverlay" : "MapLit");
            var material = new Material(shader != null ? shader : Shader.Find("Unlit/Color"));
            material.color = color; materials.Add(material); line.sharedMaterial = material;
            if (world != null && world.IsYouzhou) material.renderQueue = title.StartsWith("Selected") ? 3030 : 3020;
            line.startWidth = width; line.endWidth = width; line.loop = loop; line.useWorldSpace = true;
            line.numCornerVertices = 3; line.numCapVertices = 3;
            line.shadowCastingMode = ShadowCastingMode.Off; line.receiveShadows = false; line.positionCount = 0;
            return line;
        }

        public bool IsBlocked(Vector2 pixelTopLeft)
        {
            if (blockedRects == null) return false;
            for (int i = 0; i < blockedRects.Length; i++) if (blockedRects[i].Contains(pixelTopLeft)) return true;
            return false;
        }

        // Visual ground anchor; a deterministic pick ray does not pass through a floating label.
        public Vector3 SurfacePoint(Land tile)
        { return new Vector3(tile.x, GroundHeight(tile.x, tile.z) + (youzhou == null ? .035f : 0), tile.z); }

        private float GroundHeight(float x, float z) { return landscape == null ? 0 : landscape.Height(x, z); }

        private bool TryPlanePoint(Vector2 pixelBottomLeft, out Vector3 point)
        {
            point = Vector3.zero;
            if (viewCamera == null) return false;
            Ray ray = viewCamera.ScreenPointToRay(pixelBottomLeft); float distance;
            if (!new Plane(Vector3.up, Vector3.zero).Raycast(ray, out distance)) return false;
            point = ray.GetPoint(distance); return true;
        }

        // Coordinates use Unity screen pixels (bottom-left origin). UI exclusion is
        // handled by inputEnabled / IsBlocked before calling this geometric query.
        public bool TryPickLand(Vector2 pixelBottomLeft, out int id)
        {
            id = -1;
            if (world == null || viewCamera == null || !viewCamera.pixelRect.Contains(pixelBottomLeft)) return false;
            Vector3 point;
            if (!TryPlanePoint(pixelBottomLeft, out point)) return false;
            if (landscape != null)
            {
                // Keep raised legacy terrain compatible without one collider per cell.
                Ray ray = viewCamera.ScreenPointToRay(pixelBottomLeft);
                for (int i = 0; i < 4; i++)
                {
                    float distance;
                    if (!new Plane(Vector3.up, new Vector3(0, GroundHeight(point.x, point.z), 0)).Raycast(ray, out distance)) return false;
                    point = ray.GetPoint(distance);
                }
            }
            var tile = world.TryGetLand(Mathf.FloorToInt(point.x + .5f), Mathf.FloorToInt(point.z + .5f));
            if (tile == null) return false;
            id = tile.id; return true;
        }

        public void CenterHome()
        {
            if (world == null) return;
            var tile = world.lands[world.homeId]; focus = new Vector3(tile.x, 0, tile.z); selected = -1;
            if (selection != null) selection.positionCount = 0;
            foreach (var corner in selectionCorners) if (corner != null) corner.positionCount = 0;
            PositionCamera();
        }

        public void RefreshOwnership()
        {
            if (world == null) return;
            if (youzhou != null) youzhou.RefreshOwnership();
            if (landscape != null) landscape.RefreshOwnership();
            knownOwned = world.OwnedCount;
            knownCityVersion = world.cityVersion;
        }

        public void SetViewport(Rect area,Rect? inputArea=null)
        {
            // A transparent HUD may cover rendered terrain. Edge scrolling and
            // gestures still use the unobstructed map, not the camera's bounds.
            screenArea = inputArea??area;
            if (viewCamera != null && Screen.width > 0 && Screen.height > 0)
                viewCamera.rect = new Rect(area.x / Screen.width, 1 - (area.y + area.height) / Screen.height, area.width / Screen.width, area.height / Screen.height);
        }

        private void PositionCamera()
        {
            if (viewCamera == null || world == null) return;
            focus.x = Mathf.Clamp(focus.x, 0, world.MapWidth - 1); focus.z = Mathf.Clamp(focus.z, 0, world.MapHeight - 1); focus.y = 0;
            viewCamera.transform.rotation = world.IsYouzhou ? YouzhouLandscape.ViewRotation : Quaternion.Euler(51, 0, 0);
            viewCamera.transform.position = focus - viewCamera.transform.forward * 70;
            viewCamera.orthographicSize = zoom;
        }

        // Bounded evidence for desktop reviews; never changes gesture state or input decisions.
        private void TraceReviewMouse(Vector2 point, bool inside, bool drag, Vector3 previousFocus,
            int previousButton, bool previousDragging, Vector2 previousGesturePoint, int previousSelected)
        {
            if (!reviewMouseTrace) return;
            int down = 0, up = 0, held = 0;
            for (int button = 0; button < 3; button++)
            {
                if (Input.GetMouseButtonDown(button)) down |= 1 << button;
                if (Input.GetMouseButtonUp(button)) up |= 1 << button;
                if (Input.GetMouseButton(button)) held |= 1 << button;
            }
            bool moved = reviewHasPreviousMouse && (point - reviewPreviousMouse).sqrMagnitude >= 36;
            bool changedFocus = (focus - previousFocus).sqrMagnitude > .000001f;
            Vector2 previousMouse = reviewPreviousMouse;
            reviewPreviousMouse = point; reviewHasPreviousMouse = true;
            if (reviewMouseTraceCount >= 256 || (down == 0 && up == 0 && held == 0 && previousButton < 0 && pointer.Button < 0 && !moved && !changedFocus)) return;
            reviewMouseTraceCount++;
            Debug.Log("MAP_MOUSE frame=" + Time.frameCount + " time=" + Time.unscaledTime.ToString("F3") +
                " size=" + Screen.width + "x" + Screen.height + " mouseTL=" + point.ToString("F2") +
                " previousMouseTL=" + previousMouse.ToString("F2") + " mouseBL=" + ((Vector2)Input.mousePosition).ToString("F2") +
                " downMask=" + down + " upMask=" + up + " heldMask=" + held +
                " enabled=" + inputEnabled + " focused=" + Application.isFocused + " blocked=" + IsBlocked(point) + " inside=" + inside +
                " button=" + previousButton + ">" + pointer.Button + " dragging=" + previousDragging + ">" + pointer.Dragging +
                " gesturePreviousBL=" + previousGesturePoint.ToString("F2") + " dragApplied=" + drag +
                " focus=" + previousFocus.ToString("F3") + ">" + focus.ToString("F3") + " selected=" + previousSelected + ">" + selected);
        }

        private void Update()
        {
            if (world == null || viewCamera == null || (landscape == null && youzhou == null)) return;
            Vector3 reviewFocusBefore = focus;
            int reviewButtonBefore = pointer.Button, reviewSelectedBefore = selected;
            bool reviewDraggingBefore = pointer.Dragging;
            Vector2 reviewGesturePrevious = new Vector2(pointer.PreviousX, pointer.PreviousY);
            var point = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            bool touchInput = mobileInputMode || Input.touchCount > 0;
            if(touchInput) { pointer.Cancel(); UpdateMobileMapInput(); }
            bool inside = !touchInput && inputEnabled && Application.isFocused && screenArea.Contains(point) && !IsBlocked(point);
            if (inside)
            {
                zoom = Mathf.Clamp(zoom - Input.mouseScrollDelta.y * (world.IsYouzhou ? zoom * .10f : .55f), world.IsYouzhou ? 2.7f : 3.5f, world.IsYouzhou ? 18 : 17);
                if (pointer.Button < 0)
                {
                    for (int button = 0; button < 3; button++)
                        if (Input.GetMouseButtonDown(button)) { pointer.Begin(button, Input.mousePosition.x, Input.mousePosition.y); break; }
                }
            }
            bool drag = pointer.Move(Input.mousePosition.x, Input.mousePosition.y, inside);
            if (drag)
            {
                Vector3 previousPoint, currentPoint;
                if (TryPlanePoint(new Vector2(pointer.PreviousX, pointer.PreviousY), out previousPoint) && TryPlanePoint(Input.mousePosition, out currentPoint)) focus += previousPoint - currentPoint;
            }
            pointer.Remember(Input.mousePosition.x, Input.mousePosition.y);
            if (pointer.Button >= 0 && Input.GetMouseButtonUp(pointer.Button))
            {
                if (pointer.Release(pointer.Button, inside))
                {
                    int id;
                    if (TryPickLand(Input.mousePosition, out id)) { selected = id; if (LandClicked != null) LandClicked(id); }
                }
            }
            else if (pointer.Button >= 0 && !Input.GetMouseButton(pointer.Button)) pointer.Cancel();
            if (inside && pointer.Button < 0)
            {
                float speed = Time.unscaledDeltaTime * zoom;
                Vector3 right = viewCamera.transform.right, up = Vector3.ProjectOnPlane(viewCamera.transform.up, Vector3.up).normalized;
                if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) focus += up * speed;
                if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) focus -= up * speed;
                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) focus -= right * speed;
                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) focus += right * speed;
                if (edgeScrollEnabled && !Input.GetMouseButton(0) && !Input.GetMouseButton(1) && !Input.GetMouseButton(2))
                {
                    float horizontal = MapPointerGesture.EdgeAxis(point.x, screenArea.xMin, screenArea.xMax, 26);
                    float vertical = MapPointerGesture.EdgeAxis(point.y, screenArea.yMin, screenArea.yMax, 26);
                    focus += (right * horizontal - up * vertical) * speed * .75f;
                }
            }
            PositionCamera();
            TraceReviewMouse(point, inside, drag, reviewFocusBefore, reviewButtonBefore, reviewDraggingBefore, reviewGesturePrevious, reviewSelectedBefore);
            if (youzhou != null) youzhou.UpdateVisible(viewCamera);
            if (knownOwned != world.OwnedCount || knownCityVersion != world.cityVersion) RefreshOwnership();
            int guard=world.order!=null && world.order.phase==(int)ArmyPhase.Stationed ? world.order.target : -1;
            if(guard!=knownGuard){knownGuard=guard;RefreshOwnership();}
            if (selected >= 0 && selected < world.lands.Count)
            {
                var tile = world.lands[selected];
                for (int i = 0; i < outlinePoints.Length; i++)
                {
                    int edge = i / 8; float t = (i % 8) / 8f;
                    float x = edge == 0 ? Mathf.Lerp(-.48f, .48f, t) : edge == 1 ? .48f : edge == 2 ? Mathf.Lerp(.48f, -.48f, t) : -.48f;
                    float z = edge == 0 ? -.48f : edge == 1 ? Mathf.Lerp(-.48f, .48f, t) : edge == 2 ? .48f : Mathf.Lerp(.48f, -.48f, t);
                    x += tile.x; z += tile.z;
                    outlinePoints[i] = new Vector3(x, GroundHeight(x, z) + .055f, z);
                }
                selection.positionCount = outlinePoints.Length; selection.SetPositions(outlinePoints);
                if (youzhou != null)
                {
                    float pixel = zoom * 2 / Mathf.Max(1, viewCamera.pixelHeight);
                    selection.startWidth = selection.endWidth = pixel * 1.25f;
                    for (int i = 0; i < selectionCorners.Length; i++)
                    {
                        float sx = i == 0 || i == 3 ? -1 : 1, sz = i < 2 ? -1 : 1;
                        Vector3 c = new Vector3(tile.x + sx * .48f, .06f, tile.z + sz * .48f);
                        var corner = selectionCorners[i]; corner.positionCount = 3;
                        corner.SetPosition(0, c - new Vector3(sx * .23f, 0, 0)); corner.SetPosition(1, c); corner.SetPosition(2, c - new Vector3(0, 0, sz * .23f));
                        corner.startWidth = corner.endWidth = pixel * 3;
                    }
                }
            }
            else { selection.positionCount = 0; foreach (var corner in selectionCorners) if (corner != null) corner.positionCount = 0; }
            if (world.order == null)
            {
                route.positionCount = 0; army.gameObject.SetActive(false);
                if (selected != previewTarget)
                {
                    previewTarget = selected;
                    previewPath = world.IsYouzhou && selected >= 0 && selected != world.homeId ? world.FindMarchPath(world.homeId, selected) : null;
                }
                preview.positionCount = previewPath == null ? 0 : previewPath.Count;
                for (int i = 0; i < preview.positionCount; i++) { var t = world.lands[previewPath[i]]; preview.SetPosition(i, new Vector3(t.x, .082f, t.z)); }
            }
            else
            {
                preview.positionCount = 0; previewTarget = -2;
                var home = world.lands[world.homeId]; var target = world.lands[world.order.target];
                Vector3 a = new Vector3(home.x, 0, home.z), b = new Vector3(target.x, 0, target.z);
                if(world.order.routeVersion>=1 && world.order.path!=null)
                {
                    route.positionCount=world.order.phase==0?world.order.path.Count:0;
                    for(int i=0;i<route.positionCount;i++){var t=world.lands[world.order.path[i]];route.SetPosition(i,new Vector3(t.x,GroundHeight(t.x,t.z)+.09f,t.z));}
                }
                else
                {
                for (int i = 0; i < routePoints.Length; i++)
                {
                    Vector3 p = Vector3.Lerp(a, b, i / (float)(routePoints.Length - 1));
                    p.y = GroundHeight(p.x, p.z) + (youzhou == null ? .22f : .07f); routePoints[i] = p;
                }
                route.positionCount = routePoints.Length; route.SetPositions(routePoints);
                }
                double ax,az;world.ArmyPosition(out ax,out az);
                Vector3 pArmy = new Vector3((float)ax,0,(float)az);
                pArmy.y = GroundHeight(pArmy.x, pArmy.z) + (youzhou == null ? .23f : .08f);
                army.gameObject.SetActive(true); army.position = pArmy;
            }
        }

        private void ClearMap()
        {
            pointer.Cancel(); knownOwned = -1; knownGuard = -1; knownCityVersion = -1; previewTarget = -2; previewPath = null;
            if (mapRoot != null) { mapRoot.gameObject.SetActive(false); Release(mapRoot.gameObject); mapRoot = null; }
            if (landscape != null) { landscape.Dispose(); landscape = null; }
            if (youzhou != null) { youzhou.Dispose(); youzhou = null; }
            foreach (var material in materials) Release(material);
            materials.Clear(); selection = null; route = null; preview = null; army = null;
            for (int i = 0; i < selectionCorners.Length; i++) selectionCorners[i] = null;
        }

        internal static void Release(Object value)
        { if (value != null) { if (Application.isPlaying) Object.Destroy(value); else Object.DestroyImmediate(value); } }
        private void OnDestroy() { ClearMap(); }
    }
}
