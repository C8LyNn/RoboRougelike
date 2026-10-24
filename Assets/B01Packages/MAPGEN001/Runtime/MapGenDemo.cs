using UnityEngine;

namespace B01.MAPGEN001
{
    /// <summary>Standalone graybox playground. Only scene-local actors and physics; no W8 changes.</summary>
    public sealed class MapGenDemo : MonoBehaviour
    {
        public int seed = 12345;
        public MapRecipe recipe = MapRecipe.ObstacleCourse;
        public bool heavyVehicle;
        private MapGenBuilder generator;
        private Rigidbody rover;
        private Camera view;
        private MapGenDemoVisuals demoVisuals;
        private bool forward, reverse, left, right;
        private string notice = "";

        private void Start()
        {
            generator = GetComponent<MapGenBuilder>();
            if (generator == null) generator = gameObject.AddComponent<MapGenBuilder>();
            Build();
        }

        private void Build()
        {
            try
            {
                var profile = heavyVehicle ? MapVehicleProfile.Heavy : MapVehicleProfile.Light;
                var result = generator.Generate(recipe, seed, MapGenConfig.Default, profile);
                if (rover != null) { rover.gameObject.SetActive(false); Destroy(rover.gameObject); }
                var actor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                actor.name = "MAPGEN001_PhysicsDemoVehicle";
                actor.transform.position = result.Start;
                actor.transform.localScale = new Vector3(profile.Width, 1f, profile.Length);
                rover = actor.AddComponent<Rigidbody>();
                rover.mass = heavyVehicle ? 500f : 120f;
                // Graybox driving: yaw only, with damping to avoid endless spin.
                rover.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
                rover.linearDamping = .35f;
                rover.angularDamping = 2f;
                rover.interpolation = RigidbodyInterpolation.Interpolate;
                rover.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                if (view == null)
                {
                    var cameraGo = new GameObject("MAPGEN001_DemoCamera");
                    view = cameraGo.AddComponent<Camera>();
                    cameraGo.AddComponent<AudioListener>();
                    view.nearClipPlane = .15f;
                    var lightGo = new GameObject("MAPGEN001_Sun");
                    var sun = lightGo.AddComponent<Light>();
                    sun.type = LightType.Directional;
                    sun.intensity = 1.2f;
                    lightGo.transform.rotation = Quaternion.Euler(55f, -25f, 0f);
                }
                view.transform.position = result.Start + new Vector3(0, 33, -27);
                view.transform.LookAt(result.Start + new Vector3(0, 0, 18));
                if (demoVisuals == null)
                {
                    demoVisuals = GetComponent<MapGenDemoVisuals>();
                    if (demoVisuals == null) demoVisuals = gameObject.AddComponent<MapGenDemoVisuals>();
                }
                demoVisuals.Show(result, rover, view);
                notice = result.Diagnostics + " / demo only (no challenge win state)";
            }
            catch (System.Exception ex) { notice = "MAPGEN001 FAILED: " + ex.Message; Debug.LogException(ex); }
        }

        private void LateUpdate()
        {
            if (view == null || rover == null) return;
            // Do not lose the physical rover off-screen on long generated courses.
            Vector3 follow = rover.position + new Vector3(0f, 28f, -22f);
            view.transform.position = Vector3.Lerp(view.transform.position, follow,
                1f - Mathf.Exp(-5f * Time.deltaTime));
            view.transform.LookAt(rover.position + new Vector3(0f, 0f, 13f));
        }

        private void FixedUpdate()
        {
            if (rover == null || rover.isKinematic) return;
            float drive = (forward ? 1f : 0f) - (reverse ? 1f : 0f);
            float turn = (right ? 1f : 0f) - (left ? 1f : 0f);
            if (Mathf.Abs(drive) > 0f)
            {
                Vector3 planar = Vector3.ProjectOnPlane(rover.transform.forward, Vector3.up);
                rover.AddForce(planar.normalized * drive * rover.mass * 7f, ForceMode.Force);
            }
            if (Mathf.Abs(turn) > 0f)
                rover.AddTorque(Vector3.up * turn * rover.mass * 2f, ForceMode.Force);
            if (rover.position.y < -6f) { notice = "ROVER FELL: use Reset rover"; rover.linearVelocity = Vector3.zero; }
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(12, 12, 470, 270), GUI.skin.box);
            GUILayout.Label("B01 MAPGEN-001 / isolated Unity 6 demo");
            GUILayout.Label("Recipe: " + recipe + "  Seed: " + seed + "  Vehicle: " + (heavyVehicle ? "HEAVY" : "LIGHT"));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Regenerate same seed")) Build();
            if (GUILayout.Button("New seed")) { seed++; Build(); }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Switch recipe")) { recipe = recipe == MapRecipe.CombatArena ? MapRecipe.ObstacleCourse : MapRecipe.CombatArena; Build(); }
            if (GUILayout.Button("Switch rover")) { heavyVehicle = !heavyVehicle; Build(); }
            if (GUILayout.Button("Reset rover") && rover != null)
            {
                rover.position = generator.Current.Start; rover.rotation = Quaternion.identity;
                rover.linearVelocity = Vector3.zero; rover.angularVelocity = Vector3.zero;
            }
            GUILayout.EndHorizontal();
            GUILayout.Label("Hold movement buttons to drive the physics rover (not an AI/autoplayer).");
            GUILayout.BeginHorizontal();
            left = GUILayout.RepeatButton("TURN LEFT"); forward = GUILayout.RepeatButton("FORWARD");
            reverse = GUILayout.RepeatButton("BACK"); right = GUILayout.RepeatButton("TURN RIGHT");
            GUILayout.EndHorizontal();
            GUILayout.Label(notice);
            GUILayout.EndArea();
        }

        private void OnDestroy()
        {
            if (rover != null) Destroy(rover.gameObject);
            if (view != null) { if (view.GetComponent<AudioListener>() != null) Destroy(view.GetComponent<AudioListener>()); Destroy(view.gameObject); }
        }
    }
}
