using UnityEngine;

namespace RoboRougelike.VFX001
{
    /// <summary>Standalone visual-only showcase; no gameplay integration or input package needed.</summary>
    public sealed class Vfx001Showcase : MonoBehaviour
    {
        private VfxPoolManager _pool;
        private GameObject _vehicle;
        private Transform _thrusterFlame;
        private ThrusterVfxController _thruster;
        private WeaponRecoilAnimator _recoil;
        private bool _thrustOn;
        private PersistentDamageFxController _damageSmoke;
        private EngineExhaustTrailController _exhaustTrail;
        private GroundContactVfxController _groundFx;
        private bool _critical;
        private bool _skid;
        private Vector3 _origin;
        private int _stressCount = 24;
        private ArmorScarController _scars;
        private BurningModuleFxController _burning;
        private DetachedModuleVfxController _detachedFx;
        private VfxCoreDestructionDirector _coreDirector;
        private bool _burningOn, _detachedOn;
        private int _sequenceId;
        private readonly GUIContent _guiTitle = new GUIContent("RoboRougelike — VFX001 v5 (demo)");
        private VfxMechanicalJolt _jolt;
        private VfxHeatImpactController _heat;
        private VfxCameraImpulseReceiver _shake;
        private Vector2 _scroll;

        private void Start()
        {
            _origin = new Vector3(0f, 1.6f, 0f);
            var root = new GameObject("Pooled VFX");
            _pool = root.AddComponent<VfxPoolManager>();
            _coreDirector = gameObject.AddComponent<VfxCoreDestructionDirector>();
            _coreDirector.Bind(_pool);
            BuildVehicle();
            var demoCamera = Camera.main;
            if (demoCamera != null)
            {
                var rig = new GameObject("VFX Camera Impulse Rig - demo only");
                rig.transform.SetPositionAndRotation(demoCamera.transform.position, demoCamera.transform.rotation);
                demoCamera.transform.SetParent(rig.transform, true);
                _shake = rig.AddComponent<VfxCameraImpulseReceiver>();
                _shake.Bind(demoCamera.transform);
            }
        }

        private void BuildVehicle()
        {
            _vehicle = new GameObject("Block Vehicle — Visual Demo Only");
            _vehicle.transform.position = _origin;
            _scars = _vehicle.AddComponent<ArmorScarController>();
            _heat = _vehicle.AddComponent<VfxHeatImpactController>();
            for (int x = -2; x <= 2; x++)
                for (int z = -1; z <= 1; z++)
                {
                    var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    block.name = "ArmorBlock";
                    block.transform.SetParent(_vehicle.transform, false);
                    block.transform.localPosition = new Vector3(x * .8f, 0f, z * .8f);
                    block.transform.localScale = Vector3.one * .73f;
                }
            var nozzle = new GameObject("ExhaustSocket");
            nozzle.transform.SetParent(_vehicle.transform, false);
            nozzle.transform.localPosition = new Vector3(0f, 0f, -1.9f);
            _thrusterFlame = GameObject.CreatePrimitive(PrimitiveType.Cylinder).transform;
            _thrusterFlame.name = "Flame Visual";
            _thrusterFlame.SetParent(nozzle.transform, false);
            _thrusterFlame.localPosition = Vector3.zero;
            _thrusterFlame.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Destroy(_thrusterFlame.GetComponent<Collider>());
            var flameRenderer = _thrusterFlame.GetComponent<MeshRenderer>();
            if (flameRenderer != null) flameRenderer.sharedMaterial = VfxMaterialLibrary.Flame;
            _thruster = nozzle.AddComponent<ThrusterVfxController>();
            _thruster.Bind(_thrusterFlame);
            var trailRoot = new GameObject("Exhaust VFX - world space");
            trailRoot.transform.SetParent(nozzle.transform, false);
            trailRoot.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            _exhaustTrail = trailRoot.AddComponent<EngineExhaustTrailController>();
            var smokeSocket = new GameObject("Damage Smoke Socket").transform;
            smokeSocket.SetParent(_vehicle.transform, false);
            smokeSocket.localPosition = new Vector3(0f, 1.15f, 0f);
            _damageSmoke = smokeSocket.gameObject.AddComponent<PersistentDamageFxController>();
            var burningSocket = new GameObject("Cosmetic Fire Socket").transform;
            burningSocket.SetParent(_vehicle.transform, false);
            burningSocket.localPosition = new Vector3(.7f, 1.0f, 0f);
            _burning = burningSocket.gameObject.AddComponent<BurningModuleFxController>();
            var detachedSocket = new GameObject("Detached Part Cosmetic Socket").transform;
            detachedSocket.SetParent(_vehicle.transform, false);
            detachedSocket.localPosition = new Vector3(-1.5f, .8f, .3f);
            _detachedFx = detachedSocket.gameObject.AddComponent<DetachedModuleVfxController>();
            var contactRoot = new GameObject("Ground Contact FX Root - demo");
            contactRoot.transform.SetParent(_vehicle.transform, false);
            contactRoot.transform.localPosition = new Vector3(0f, -1.47f, 0f);
            var left = new GameObject("Left track socket").transform;
            left.SetParent(contactRoot.transform, false);
            left.localPosition = new Vector3(-1.5f, 0f, 0f);
            var right = new GameObject("Right track socket").transform;
            right.SetParent(contactRoot.transform, false);
            right.localPosition = new Vector3(1.5f, 0f, 0f);
            _groundFx = contactRoot.AddComponent<GroundContactVfxController>();
            _groundFx.Bind(left, right);

            var turret = new GameObject("TurretSocket");
            turret.transform.SetParent(_vehicle.transform, false);
            turret.transform.localPosition = new Vector3(0f, .55f, .4f);
            var barrel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barrel.name = "Visual Recoil Child";
            barrel.transform.SetParent(turret.transform, false);
            barrel.transform.localPosition = new Vector3(0f, 0f, .55f);
            barrel.transform.localScale = new Vector3(.35f, .35f, 1.2f);
            _recoil = turret.AddComponent<WeaponRecoilAnimator>();
            _recoil.Bind(barrel.transform);
            var frame = new GameObject("Mechanical Damage - decorative only");
            frame.transform.SetParent(_vehicle.transform, false);
            frame.transform.localPosition = new Vector3(-1.1f, .72f, .2f);
            var joltVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            joltVisual.name = "Decorative Jolt Shell";
            joltVisual.transform.SetParent(frame.transform, false);
            joltVisual.transform.localScale = new Vector3(.65f,.3f,.65f);
            Destroy(joltVisual.GetComponent<Collider>());
            _jolt = frame.AddComponent<VfxMechanicalJolt>();
            _jolt.Bind(joltVisual.transform);
        }

        private void Update()
        {
            // A visual-only mock vehicle: a small lateral slide generates a visible trail.
            // This is NOT movement logic for the actual RoboRougelike vehicle.
            if (_vehicle == null) return;
            Vector3 target = _origin + (_skid ? new Vector3(Mathf.Sin(Time.time * 1.5f) * 1.2f, 0f, 0f) : Vector3.zero);
            _vehicle.transform.position = Vector3.Lerp(_vehicle.transform.position, target, Time.deltaTime * 5f);
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(12f, 12f, 330f, Mathf.Min(Screen.height - 24f, 720f)), GUI.skin.box);
            _scroll = GUILayout.BeginScrollView(_scroll);
            GUILayout.Label(_guiTitle);
            if (GUILayout.Button("Metal impact")) Hit(ImpactMaterialType.Metal);
            if (GUILayout.Button("V5: Mechanical impact jolt") && _jolt != null)
                _jolt.React(1f,Vector3.back);
            if (GUILayout.Button("V5: Heat impact glow") && _heat != null)
                _heat.PlayHeat(_origin + new Vector3(.4f,.1f,-1.2f), Vector3.back, 1f);
            if (GUILayout.Button("V5: Camera explosion impulse") && _shake != null)
                _shake.Accept(new VfxEvent(VfxEventKind.CoreDestroyed, _origin, Vector3.up,ImpactMaterialType.Core,1f));
            if (GUILayout.Button("Rubber impact")) Hit(ImpactMaterialType.Rubber);
            if (GUILayout.Button("Energy impact")) Hit(ImpactMaterialType.Energy);
            if (GUILayout.Button("Engine explosion")) Explosion(false);
            if (GUILayout.Button("CORE EXPLOSION")) Explosion(true);
            if (GUILayout.Button("Core destruction sequence") && _coreDirector != null)
                _coreDirector.Begin(_origin + Vector3.up * .7f, Vector3.up, 1f, ++_sequenceId, 49);
            if (GUILayout.Button("Armor impact scar") && _scars != null)
                _scars.AddScar(_origin + new Vector3(Random.Range(-1.4f, 1.4f),0f,-1.17f),
                    Vector3.back, ImpactMaterialType.Metal, .9f, ++_sequenceId);
            if (GUILayout.Button("Clear armor scars") && _scars != null) _scars.ClearScars();
            if (GUILayout.Button(_burningOn ? "Extinguish cosmetic fire" : "Ignite cosmetic fire"))
            {
                _burningOn = !_burningOn;
                if (_burning != null) _burning.SetVisualState(_burningOn ? .95f : 0f, _burningOn, false);
            }
            if (GUILayout.Button(_detachedOn ? "Stop detached trail" : "Detached debris trail"))
            {
                _detachedOn = !_detachedOn;
                if (_detachedFx != null) _detachedFx.SetDetachedMotion(_detachedOn, _detachedOn ? 9f : 0f);
            }
            if (GUILayout.Button("Detach FX")) Emit(VfxEventKind.Detached, ImpactMaterialType.Metal, .8f);
            if (GUILayout.Button("Landing dust")) Emit(VfxEventKind.LandingDust, ImpactMaterialType.Default, .85f);
            if (GUILayout.Button("Weapon recoil") && _recoil != null) _recoil.PlayRecoil();
            if (GUILayout.Button(_thrustOn ? "Throttle OFF" : "Throttle ON"))
            {
                _thrustOn = !_thrustOn;
                if (_thruster != null) _thruster.SetThrust01(_thrustOn ? 1f : 0f);
                if (_exhaustTrail != null) _exhaustTrail.SetThrust01(_thrustOn ? 1f : 0f);
            }
            if (GUILayout.Button(_critical ? "Repair module / stop smoke" : "Damage module / smoke"))
            {
                _critical = !_critical;
                if (_damageSmoke != null) _damageSmoke.SetDamage01(_critical ? .95f : .05f);
            }
            if (GUILayout.Button(_skid ? "Stop skid marks" : "Enable skid marks"))
            {
                _skid = !_skid;
                if (_groundFx != null) _groundFx.SetMotion(_skid ? 5f : 0f, _skid ? 1f : 0f, _skid);
            }
            if (GUILayout.Button("Burst stress test")) for (int i = 0; i < _stressCount; i++) Hit(ImpactMaterialType.Metal);
            if (GUILayout.Button("Clear all FX") && _pool != null) _pool.ClearAll();
            if (_pool != null) GUILayout.Label("Active: " + _pool.ActiveCount + "   Created: " + _pool.CreatedCount + "   Idle: " + _pool.PooledCount);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void Hit(ImpactMaterialType material) => Emit(VfxEventKind.Hit, material, .8f);
        private void Explosion(bool core) => Emit(core ? VfxEventKind.CoreDestroyed : VfxEventKind.Explosion,
            core ? ImpactMaterialType.Core : ImpactMaterialType.Engine, .9f);
        private void Emit(VfxEventKind kind, ImpactMaterialType material, float intensity)
        {
            if (_pool == null) return;
            var p = _origin + new Vector3(Random.Range(-.7f, .7f), .7f, Random.Range(-.5f, .6f));
            _pool.Emit(new VfxEvent(kind, p, Vector3.up, material, intensity));
        }
        private void OnDestroy() { if (_pool != null) Destroy(_pool.gameObject); }
    }
}
