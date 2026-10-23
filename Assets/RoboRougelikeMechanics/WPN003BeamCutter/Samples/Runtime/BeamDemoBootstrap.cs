using UnityEngine;

namespace RoboRougelike.Mechanics.BeamCutter.Samples
{
    /// <summary>
    /// Add this to an EMPTY scene GameObject and Play. Builds a static target and a test cutter.
    /// Automatically fires for 2 seconds / rests for 1 second. Input-System agnostic.
    /// </summary>
    public sealed class BeamDemoBootstrap : MonoBehaviour
    {
        private BeamCutterModule _cutter;
        private LineRenderer _line;
        private float _elapsed;

        private void Start()
        {
            GameObject shooter = new GameObject("WPN003 Demo Shooter");
            shooter.transform.position = new Vector3(0f, 1f, 0f);
            Rigidbody body = shooter.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeAll;
            GameObject emitter = new GameObject("Beam Emitter");
            emitter.transform.SetParent(shooter.transform, false);
            emitter.transform.localPosition = Vector3.zero;
            GameObject muzzle = new GameObject("Muzzle +Z");
            muzzle.transform.SetParent(emitter.transform, false);
            _cutter = emitter.AddComponent<BeamCutterModule>();
            BeamDemoDamageSink sink = emitter.AddComponent<BeamDemoDamageSink>();
            _cutter.SetAutomaticSimulation(false);
            _cutter.Bind(body, muzzle.transform, shooter.transform, sink);
            _line = emitter.AddComponent<LineRenderer>();
            Shader lineShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (lineShader == null) lineShader = Shader.Find("Sprites/Default");
            if (lineShader != null) _line.material = new Material(lineShader);
            _line.startWidth = 0.065f;
            _line.endWidth = 0.035f;
            _line.startColor = Color.cyan;
            _line.endColor = Color.white;
            _cutter.SetLineRenderer(_line);

            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = "WPN003 Demo Target (demo HP only)";
            block.transform.position = new Vector3(0f, 1f, 9f);
            block.transform.localScale = new Vector3(2f, 2f, 1f);
            block.AddComponent<BeamDemoBlock>().SetHp(250f);
            Debug.Log("WPN003 Demo: beam fires 2s, rests 1s. Target is a sample, NOT R6 damage.");
        }

        private void FixedUpdate()
        {
            if (_cutter == null) return;
            _elapsed += Time.fixedDeltaTime;
            _cutter.SetTriggerHeld(_elapsed % 3f < 2f);
            _cutter.ManualFixedStep(Time.fixedDeltaTime);
        }
    }
}
