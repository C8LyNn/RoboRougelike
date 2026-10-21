using System;
using UnityEngine;

namespace RoboRougelike.MapSystemR4
{
    // Drag into a blank scene or use Tools > RoboRougelike > MAP R4 > Create Demo.
    // Debug sandbox only; no production game scene or AI integration.
    public sealed class MapR4DemoController : MonoBehaviour
    {
        [SerializeField] private TextAsset mapJson;
        [SerializeField] private MapR4Mode mode = MapR4Mode.Escape;
        [SerializeField] private Material roadMaterial;
        [SerializeField] private Material nodeMaterial;
        [SerializeField] private bool spawnDemoRover = true;
        [SerializeField] private float survivalSeconds = 30f;
        [SerializeField] private float maxSeconds = 180f;
        // R5: explicit obstacle layers, MUST exclude road mesh and all player colliders.
        [SerializeField] private LayerMask traversalObstacleLayers;
        [SerializeField] private bool drawR6TurnCandidate = true;
        [SerializeField] private string r6IncomingEdge = "start_junction";
        [SerializeField] private string r6OutgoingEdge = "junction_lower";
        [SerializeField] private string r6JunctionNode = "junction";

        private MapR4BuildResult _build;
        private MapR4StateController _states;
        private MapR4ChallengeRuntime _challenge;
        private GameObject _rover;
        private GameObject _ownedCamera;
        private GameObject _ownedLight;
        private string _validationMessage;

        public MapR4ChallengeRuntime Challenge => _challenge;
        public string ValidationMessage => _validationMessage;
        public MapR4StateController MapState => _states;
        public MapR4BuildResult MapBuild => _build;

        private void Start() { BuildDemo(); }
        private void Update() { _challenge?.Tick(Time.deltaTime); }
        private void OnDestroy() { Cleanup(); }

        [ContextMenu("R5 / Build demo")]
        public void BuildDemo()
        {
            Cleanup();
            if (mapJson == null) mapJson = Resources.Load<TextAsset>("MAP_R4_quarry_03");
            if (mapJson == null) { _validationMessage = "Missing MAP_R4_quarry_03 resource"; Debug.LogError(_validationMessage, this); return; }
            try
            {
                var map = MapR4MapData.Parse(mapJson.text);
                var profile = new MapR4VehicleProfile
                {
                    id = "demo_standard", width = 2.6f, height = 2f, minTurnRadius = 4.5f,
                    maxSlopeDeg = 16f, capabilities = new[] {"ground"}
                };
                var endAnchor = mode == MapR4Mode.TimeAttack ? "checkpoint_upper" : mode == MapR4Mode.Escape ? "exit_east" : "arena_middle";
                var targetNode = FindAnchorNode(map, endAnchor);
                var startNode = FindAnchorNode(map, "spawn_west");
                var state = mode == MapR4Mode.Escape ? "bridge_collapsed" : "intact";
                if (string.IsNullOrEmpty(targetNode) || !MapR6RouteValidator.CanReachWithTurns(map, state, startNode, targetNode, profile, out _))
                    throw new InvalidOperationException("G1a: demo route inaccessible");
                _build = MapR4GeometryBuilder.Build(map, transform, roadMaterial, nodeMaterial);
                _states = new MapR4StateController(map, _build);
                if (!_states.TrySetState(state)) throw new InvalidOperationException("Unknown map state");
                _challenge = new MapR4ChallengeRuntime(mode, maxSeconds, survivalSeconds);
                _challenge.Begin();
                CreateAnchorTriggers(map);
                CreateBasicViewIfNeeded();
                if (spawnDemoRover) SpawnRover();
                _validationMessage = "R6 G0/G1a+ geometric candidate; physical driving NOT proven";
            }
            catch (Exception ex)
            {
                _validationMessage = ex.Message;
                Debug.LogError("MAP R4: " + ex, this);
                Cleanup();
            }
        }

        [ContextMenu("R5 / Audit obstacle layers (G1b, not driving QA)")]
        public void AuditR5Obstacles()
        {
            if (traversalObstacleLayers.value == 0)
            {
                Debug.LogWarning("R5 G1b NOT RUN: select obstacle-only layers excluding roads and rover.", this);
                return;
            }
            if (mapJson == null) mapJson = Resources.Load<TextAsset>("MAP_R4_quarry_03");
            if (mapJson == null) { Debug.LogWarning("R5 G1b: no map JSON", this); return; }
            var map = MapR4MapData.Parse(mapJson.text);
            var profile = new MapR4VehicleProfile
            {
                id = "demo_standard", width = 2.6f, height = 2f, length = 3.5f,
                minTurnRadius = 4.5f, maxSlopeDeg = 16f, capabilities = new[] { "ground" }
            };
            var points = new System.Collections.Generic.Dictionary<string, Vector3>(StringComparer.Ordinal);
            foreach (var node in map.nodes) points[node.id] = node.position;
            int clear = 0, blocked = 0;
            foreach (var edge in map.edges)
            {
                var check = MapR5TraversalProbe.CheckEdge(
                    points[edge.fromNode], points[edge.toNode], edge, profile, traversalObstacleLayers);
                if (check.IsClear) clear++;
                else { blocked++; Debug.LogWarning("R5 G1b: " + edge.id + " " + check.Status + " " + check.Reason, this); }
            }
            Debug.Log($"R5 G1b obstacle-only probe: clear={clear}, blocked-or-invalid={blocked}. " +
                "Not a full physics driving or turning test.", this);
        }

        private static string FindAnchorNode(MapR4MapData map, string anchorId)
        {
            foreach (var anchor in map.anchors)
                if (anchor.id == anchorId) return anchor.nodeId;
            return null;
        }

        private void CreateAnchorTriggers(MapR4MapData map)
        {
            foreach (var anchor in map.anchors)
            {
                if (anchor.kind != "checkpoint" && anchor.kind != "finish" && anchor.kind != "exit") continue;
                var go = _build.Anchors[anchor.id].gameObject;
                var trigger = go.AddComponent<SphereCollider>();
                trigger.isTrigger = true;
                trigger.radius = 5f;
                go.AddComponent<MapR4AnchorTrigger>().Initialize(this, anchor.id, 5f);
                var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                marker.name = "DEBUG_MARKER_" + anchor.id;
                marker.transform.SetParent(go.transform, false);
                marker.transform.localPosition = new Vector3(0f, 0.15f, 0f);
                marker.transform.localScale = new Vector3(4f, 0.08f, 4f);
                var markerCollider = marker.GetComponent<Collider>();
                if (markerCollider != null) markerCollider.enabled = false;
                // Visible marker is debug-only; anchor trigger remains the source of truth.
            }
        }

        private void CreateBasicViewIfNeeded()
        {
            if (Camera.main == null)
            {
                _ownedCamera = new GameObject("MAP_R4_DEBUG_CAMERA");
                var camera = _ownedCamera.AddComponent<Camera>();
                camera.fieldOfView = 66f;
                _ownedCamera.tag = "MainCamera";
                _ownedCamera.transform.position = new Vector3(10f, 140f, -130f);
                _ownedCamera.transform.LookAt(new Vector3(0f, 0f, 0f));
            }
            if (FindObjectsByType<Light>(FindObjectsSortMode.None).Length == 0)
            {
                _ownedLight = new GameObject("MAP_R4_DEBUG_LIGHT");
                var light = _ownedLight.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.2f;
                _ownedLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }
        }

        private void SpawnRover()
        {
            if (!_build.Anchors.TryGetValue("spawn_west", out var spawn)) return;
            _rover = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _rover.name = "R4_DEMO_ROVER_WASD_NOT_PRODUCTION";
            _rover.transform.position = spawn.position + Vector3.up * 1.5f;
            _rover.transform.localScale = new Vector3(2.4f, 1.3f, 3.5f);
            _rover.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            var body = _rover.AddComponent<Rigidbody>();
            body.mass = 1000f;
            body.linearDamping = 0.8f;
            body.angularDamping = 4f;
            var recorder = _rover.AddComponent<MapR6DriveEvidenceRecorder>();
            recorder.SetMapState(_states == null ? "unknown" : _states.StateId);
            _rover.AddComponent<MapR4PrototypeDriver>();
        }

        public void OnDemoAnchor(string anchorId) { _challenge?.NotifyAnchor(anchorId); }

        [ContextMenu("R4 / Mark boss defeated (simulated)")]
        public void DemoDefeatBoss() { _challenge?.NotifyBossDefeated(); }

        [ContextMenu("R6 / Print selected junction evaluation")]
        public void PrintR6JunctionEvaluation()
        {
            if (mapJson == null) mapJson = Resources.Load<TextAsset>("MAP_R4_quarry_03");
            if (mapJson == null) return;
            var profile = new MapR4VehicleProfile
            {
                id="demo_standard", width=2.6f,height=2f,length=3.5f,
                minTurnRadius=4.5f,maxSlopeDeg=16f,sideMargin=.35f,verticalMargin=.25f
            };
            var plan = MapR6JunctionPlanner.Plan(MapR4MapData.Parse(mapJson.text),
                r6IncomingEdge,r6OutgoingEdge,r6JunctionNode,profile);
            Debug.Log($"R6 junction: valid={plan.Valid}, reason={plan.Reason}, " +
                $"radius={plan.MinMeasuredRadius:F2}, slope={plan.MaxMeasuredSlopeDeg:F2}",this);
        }

        [ContextMenu("R4 / Collapse bridge")]
        public void DemoCollapseBridge()
        {
            if (_states == null || !_states.TrySetState("bridge_collapsed")) return;
            if (_rover != null)
            {
                var recorder = _rover.GetComponent<MapR6DriveEvidenceRecorder>();
                if (recorder != null) recorder.SetMapState("bridge_collapsed");
            }
        }

        private void OnGUI()
        {
            if (!Application.isPlaying) return;
            GUI.Box(new Rect(10, 10, 470, 137), "MAP R6 — JUNCTION/DRIVE QA SANDBOX");
            GUI.Label(new Rect(22, 37, 445, 22), "Mode: " + mode + " | State: " + (_challenge == null ? "missing" : _challenge.Status.ToString()));
            GUI.Label(new Rect(22, 60, 445, 22), "Elapsed: " + (_challenge == null ? "0" : _challenge.ElapsedSeconds.ToString("F1")) + " s | WASD to drive");
            GUI.Label(new Rect(22, 83, 445, 22), _challenge == null ? _validationMessage : _challenge.Resolution);
            if (GUI.Button(new Rect(22, 107, 150, 28), "Simulate boss defeat")) DemoDefeatBoss();
            if (GUI.Button(new Rect(180, 107, 140, 28), "Collapse bridge")) DemoCollapseBridge();
            if (GUI.Button(new Rect(330, 107, 136, 28), "Reset demo")) BuildDemo();
        }

        private static void DestroyDemoObject(GameObject obj)
        {
            if (obj == null) return;
            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }

        private void OnDrawGizmos()
        {
            if (_build == null) return;
            if (drawR6TurnCandidate)
            {
                if (mapJson == null) mapJson = Resources.Load<TextAsset>("MAP_R4_quarry_03");
                if (mapJson != null)
                {
                    var map = MapR4MapData.Parse(mapJson.text);
                    var profile = new MapR4VehicleProfile
                    {
                        id = "demo_standard", width = 2.6f, height = 2f, length = 3.5f,
                        minTurnRadius = 4.5f, maxSlopeDeg = 16f,
                        sideMargin = .35f, verticalMargin = .25f
                    };
                    var plan = MapR6JunctionPlanner.Plan(map, r6IncomingEdge, r6OutgoingEdge, r6JunctionNode, profile);
                    MapR6JunctionGizmos.Draw(plan);
                }
            }
            foreach (var anchor in _build.Anchors)
            {
                if (anchor.Value == null) continue;
                Gizmos.color = anchor.Key.Contains("exit") || anchor.Key.Contains("finish") ? Color.green : Color.cyan;
                Gizmos.DrawWireSphere(anchor.Value.position, 3f);
            }
        }

        private void Cleanup()
        {
            DestroyDemoObject(_ownedCamera);
            DestroyDemoObject(_ownedLight);
            _ownedCamera = null;
            _ownedLight = null;
            if (_rover != null)
            {
                if (Application.isPlaying) Destroy(_rover);
                else DestroyImmediate(_rover);
                _rover = null;
            }
            if (_build == null) return;
            if (_build.Root != null)
            {
                if (Application.isPlaying) Destroy(_build.Root);
                else DestroyImmediate(_build.Root);
            }
            MapR4GeometryBuilder.DisposeMeshes(_build);
            _build = null;
            _states = null;
            _challenge = null;
        }
    }
}
