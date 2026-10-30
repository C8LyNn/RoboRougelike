using UnityEngine;
using B01.LevelChallenges.SweepOrb;

namespace B0125.SweepOrb.R2
{
    /// <summary>
    /// Isolated Unity 6 URP-capable real Rigidbody demonstration of immutable
    /// SWEEPORB001/R1 C#9 source rules. This is NOT the canonical game.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SweepOrbUnityDemo : MonoBehaviour
    {
        [SerializeField] private float driveForce = 48f;
        [SerializeField] private int maximumHealth = 100;
        [SerializeField] private int damagePerSweep = 10;

        private SweepController controller;
        private Rigidbody vehicle;
        private GameObject movingOrb;
        private GameObject floor;
        private Renderer orbRenderer;
        private Vector3 previousVehiclePosition;
        private Vector2 drive;
        private SweepResult latest;
        private int health;
        private int hits;
        private bool paused;
        private bool heavy;

        public Rigidbody ActiveRigidbody => vehicle;
        public int Health => health;
        public int Hits => hits;
        public bool Paused => paused;
        public bool Heavy => heavy;
        public double Elapsed => controller == null ? 0 : controller.Elapsed;
        public SweepState State => controller == null ? SweepState.Telegraph : latest.State;

        private void Awake()
        {
            controller = new SweepController(new SweepConfig(
                "W11_SWEEPORB_R2", 28, 40, 36, 40, 1, 1, 2, 3, 0));
            BuildIsolatedPhysicalArena();
            Restart();
        }

        private void BuildIsolatedPhysicalArena()
        {
            floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "W11 Demo Floor - physical BoxCollider";
            floor.transform.SetParent(transform, false);
            floor.transform.position = new Vector3(32f, -0.25f, 40f);
            floor.transform.localScale = new Vector3(30f, .5f, 24f);
            // CreatePrimitive(Cube) provides an actual BoxCollider.
            GameObject rig = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            rig.name = "W11 Player Light or Heavy Rigidbody";
            rig.transform.SetParent(transform, false);
            rig.transform.position = SpawnPosition();
            vehicle = rig.AddComponent<Rigidbody>();
            vehicle.mass = 1.5f;
            vehicle.useGravity = true;
            vehicle.interpolation = RigidbodyInterpolation.Interpolate;
            vehicle.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            vehicle.constraints = RigidbodyConstraints.FreezeRotationX |
                                  RigidbodyConstraints.FreezeRotationZ;

            movingOrb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            movingOrb.name = "W11 Visual Orb - source swept rule controls damage";
            movingOrb.transform.SetParent(transform, false);
            movingOrb.transform.localScale = new Vector3(2f, 2f, 2f);
            // Avoid misleading duplicate physics hits; C#9 swept rule is the authority.
            Collider collider = movingOrb.GetComponent<Collider>();
            collider.enabled = false;
            orbRenderer = movingOrb.GetComponent<Renderer>();
            ApplyVisual(SweepState.Telegraph);
        }

        private static Vector3 SpawnPosition() => new Vector3(28f, 1.1f, 40f);

        public void SetVehicle(bool useHeavy)
        {
            heavy = useHeavy;
            if (vehicle == null) return;
            vehicle.mass = heavy ? 12f : 1.5f;
            // Two physically different CapsuleCollider envelopes, not cosmetic mass only.
            vehicle.transform.localScale = heavy
                ? new Vector3(1.8f, 1.0f, 1.6f)
                : Vector3.one;
            Restart();
        }

        public void SetPaused(bool value)
        {
            paused = value;
            if (vehicle != null)
            {
                vehicle.isKinematic = value;
                if (!value) previousVehiclePosition = vehicle.position;
            }
        }

        public void SetDrive(float x, float z)
        {
            drive = Vector2.ClampMagnitude(new Vector2(
                Mathf.Clamp(x, -1f, 1f), Mathf.Clamp(z, -1f, 1f)), 1f);
        }

        public void Restart()
        {
            if (controller == null || vehicle == null) return;
            paused = false;
            drive = Vector2.zero;
            health = maximumHealth;
            hits = 0;
            controller.Reset();
            vehicle.isKinematic = false;
            vehicle.linearVelocity = Vector3.zero;
            vehicle.angularVelocity = Vector3.zero;
            vehicle.position = SpawnPosition();
            vehicle.rotation = Quaternion.identity;
            previousVehiclePosition = vehicle.position;
            UpdateOrbPosition();
        }

        private void FixedUpdate()
        {
            if (controller == null || vehicle == null || paused) return;
            double delta = Time.fixedDeltaTime;
            // Never jump over whole active phases when a host changes fixedDeltaTime.
            if (delta <= 0 || delta > 0.25) return; // fail closed, no damage
            Vector3 current = vehicle.position;
            latest = controller.Advance(new SweepSample(delta,
                previousVehiclePosition.x, previousVehiclePosition.z,
                current.x, current.z, health > 0, false));
            previousVehiclePosition = current;
            if (latest.DamageRequest && health > 0)
            {
                health = Mathf.Max(0, health - damagePerSweep);
                hits++;
                if (health == 0) drive = Vector2.zero;
            }
            if (health > 0 && drive.sqrMagnitude > 0)
            {
                // Real rigidbody acceleration (mass affects movement).
                vehicle.AddForce(new Vector3(drive.x, 0, drive.y) * driveForce,
                    ForceMode.Force);
            }
            UpdateOrbPosition();
        }

        private void UpdateOrbPosition()
        {
            if (movingOrb == null || controller == null) return;
            controller.OrbPosition(controller.Elapsed, out double x, out double z);
            movingOrb.transform.position = new Vector3((float)x, 1f, (float)z);
            ApplyVisual(controller.StateAt(controller.Elapsed));
        }

        private void ApplyVisual(SweepState state)
        {
            if (orbRenderer == null) return;
            // Plain material color is intentionally renderer-agnostic for source demo;
            // no claim of integrated URP VFX/shaders.
            orbRenderer.material.color = state == SweepState.Telegraph
                ? Color.yellow
                : state == SweepState.Moving ? Color.red : Color.green;
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(12, 12, 280, 270), GUI.skin.box);
            GUILayout.Label("B01-25 SWEEPORB R2 - ISOLATED UNITY DEMO");
            GUILayout.Label("HP " + health + "/" + maximumHealth +
                            " | Sweeps hit " + hits);
            GUILayout.Label("Phase " + State + " | t=" +
                            Elapsed.ToString("F2") + "s");
            GUILayout.Label("Rig " + (heavy ? "HEAVY 12kg" : "LIGHT 1.5kg"));
            if (GUILayout.Button(heavy ? "Switch LIGHT" : "Switch HEAVY"))
                SetVehicle(!heavy);
            if (GUILayout.Button(paused ? "RESUME" : "PAUSE"))
                SetPaused(!paused);
            if (GUILayout.Button("RESTART ATTEMPT")) Restart();
            float x = 0, z = 0;
            if (GUILayout.RepeatButton("Drive +X")) x = 1;
            if (GUILayout.RepeatButton("Drive -X")) x = -1;
            if (GUILayout.RepeatButton("Drive +Z")) z = 1;
            if (GUILayout.RepeatButton("Drive -Z")) z = -1;
            SetDrive(x, z);
            GUILayout.EndArea();
        }
    }
}
