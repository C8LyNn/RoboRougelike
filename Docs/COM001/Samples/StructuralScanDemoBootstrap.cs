// OPTIONAL SAMPLE. Copy this file under Assets/ in Unity to compile it.
// Put this MonoBehaviour on an empty GameObject in a new empty scene; Play and click SCAN.
// This creates a simulated enemy with exposed modules and an occluded core.
using UnityEngine;
using RoboRougelike.Mechanics.StructuralScan;

public sealed class StructuralScanDemoBootstrap : MonoBehaviour
{
    private StructuralScanController scanner;
    private StructuralScanRegistry registry;
    private GUIStyle labelStyle;

    private void Start()
    {
        registry = new GameObject("COM001 Registry").AddComponent<StructuralScanRegistry>();
        var sensor = new GameObject("Sensor");
        sensor.transform.position = new Vector3(0, 1.5f, 0);
        sensor.transform.rotation = Quaternion.identity;
        var own = new GameObject("Player Sensor Chassis");
        sensor.transform.SetParent(own.transform, true);
        scanner = own.AddComponent<StructuralScanController>();
        scanner.Bind(sensor.transform, own.transform, 1, 1, registry);
        scanner.Configure(new StructuralScanSettings
        {
            rangeMeters = 25,
            fullConeDegrees = 90,
            displaySeconds = 6,
            cooldownSeconds = 2,
            maxRevealedParts = 16
        });
        Block("Enemy Wheel", new Vector3(-2, 1.5f, 8), 2, 2, 11, StructuralPartType.Wheel, Color.gray);
        Block("Enemy Weapon", new Vector3(2, 1.5f, 8), 2, 2, 12, StructuralPartType.Weapon, Color.red);
        Block("Enemy Core", new Vector3(0, 1.5f, 12), 2, 2, 13, StructuralPartType.Core, Color.yellow);
        // A solid barrier occludes the core only (other two parts remain visible).
        var cover = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cover.name = "Cover (hides core)";
        cover.transform.position = new Vector3(0, 1.5f, 6);
        cover.transform.localScale = new Vector3(1.5f, 3f, .5f);
        cover.GetComponent<Renderer>().material.color = Color.black;
        var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Ground";
        ground.transform.position = new Vector3(0, -.5f, 7);
        ground.transform.localScale = new Vector3(26, 1f, 25);
        var cam = Camera.main;
        if (cam == null)
        {
            var go = new GameObject("Demo Camera");
            cam = go.AddComponent<Camera>();
            go.tag = "MainCamera";
        }
        cam.transform.position = new Vector3(0, 7, -12);
        cam.transform.LookAt(new Vector3(0, 1, 9));
    }

    private void Block(string name, Vector3 pos, int owner, int team, int part,
        StructuralPartType type, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.position = pos;
        go.GetComponent<Renderer>().material.color = color;
        var proxy = go.AddComponent<StructuralScanPartProxy>();
        proxy.Configure(registry, owner, team, part, type, 0.6f, type == StructuralPartType.Core ? 1f : 0f);
    }

    private void OnGUI()
    {
        if (scanner == null) return;
        if (labelStyle == null) { labelStyle = new GUIStyle(GUI.skin.label); labelStyle.fontSize = 15; }
        GUILayout.BeginArea(new Rect(15, 15, 500, 420), GUI.skin.box);
        GUILayout.Label("COM-001 Structural Scan / optional demonstration", labelStyle);
        GUILayout.Label("Cooldown: " + scanner.CooldownLeft.ToString("F1") + " sec");
        if (GUILayout.Button("SCAN", GUILayout.Height(35))) scanner.TryScan();
        if (!scanner.HasVisibleContacts) GUILayout.Label("No scan result (or expired / occluded / on cooldown)");
        else
        {
            GUILayout.Label("Enemy owner ID: " + scanner.SelectedTargetOwner);
            var entries = scanner.Contacts;
            for (int i = 0; i < entries.Count; ++i)
            {
                var c = entries[i];
                GUILayout.Label(c.Type + " / part " + c.PartKey + " / HP " +
                    Mathf.RoundToInt(c.HealthFraction * 100) + "% / priority " + c.Priority.ToString("F0"));
            }
            GUILayout.Label("Core behind cover should NOT be displayed.");
        }
        GUILayout.EndArea();
    }
}
