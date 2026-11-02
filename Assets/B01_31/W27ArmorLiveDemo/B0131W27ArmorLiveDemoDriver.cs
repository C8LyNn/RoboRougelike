#nullable disable
using RogueMachine.P12PipelineSafe;
using UnityEngine;

namespace RoboRougelike.B0131.Art
{
    /// <summary>
    /// ISOLATED MANUAL DEMO ONLY. Never attached to combat/garage canonical
    /// prefabs. The buttons intentionally call the original game P12
    /// ApplyDamage / RestoreHealthRatio on a *demo-created* disposable part.
    /// Neither this nor W25 owns authentic gameplay authority or Save/Load.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class B0131W27ArmorLiveDemoDriver : MonoBehaviour
    {
        [SerializeField] private P12PipelineSafeDamageable testPart;
        [SerializeField] private B0131W25P12DamageVisualBinding visualBinder;

        public bool IsReady => testPart != null && visualBinder != null;
        public bool BindDemo(P12PipelineSafeDamageable source,
                             B0131W25P12DamageVisualBinding binder)
        {
            if (source == null || binder == null) return false;
            testPart = source;
            visualBinder = binder;
            return true;
        }
        public bool Damage(float amount)
        {
            if (!IsReady || testPart.IsDead ||
                float.IsNaN(amount) || float.IsInfinity(amount) || amount <= 0f)
                return false;
            // Explicit isolated scene actor; real original P12 damage pipeline.
            testPart.ApplyDamage(amount, testPart.transform.position, new Vector3(0f, 0f, 1f));
            visualBinder.RefreshVisual();
            return true;
        }
        public bool RepairToFull()
        {
            if (!IsReady || testPart.IsDead) return false;
            testPart.RestoreHealthRatio(1f);
            return visualBinder.RefreshVisual();
        }
        private void OnGUI()
        {
            if (!IsReady) return;
            GUILayout.BeginArea(new Rect(14, 14, 300, 230), GUI.skin.box);
            GUILayout.Label("B01-31 W27 / ISOLATED ARMOR DEMO");
            GUILayout.Label("P12 real HP: " + testPart.CurrentHp.ToString("F1")
                            + " / " + testPart.MaxHp.ToString("F1"));
            GUILayout.Label("Visual: " + (testPart.IsDead ? "DESTROYED"
                            : visualBinder.DamageThreshold.ToString("P0") + " threshold"));
            if (GUILayout.Button("Hit 40 HP")) Damage(40f);
            if (GUILayout.Button("Hit 20 HP")) Damage(20f);
            if (GUILayout.Button("Repair to 100% (living only)")) RepairToFull();
            if (GUILayout.Button("Lethal hit (demo P12 part)")) Damage(1000f);
            GUILayout.Label("No save, currency, rig IDs or canonical combat affected.");
            GUILayout.EndArea();
        }
    }
}
