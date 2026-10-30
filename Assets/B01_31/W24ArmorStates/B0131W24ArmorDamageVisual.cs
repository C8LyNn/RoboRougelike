using UnityEngine;

namespace RoboRougelike.B0131.Art
{
    // VISUAL-ONLY adapter: never mutates part HP, ownership, save state, economy or colliders.
    public enum ArmorVisualPhase { Ok = 0, Damaged = 1, Destroyed = 2 }

    [DisallowMultipleComponent]
    public sealed class B0131W24ArmorDamageVisual : MonoBehaviour
    {
        [SerializeField] private GameObject ok;
        [SerializeField] private GameObject damaged;
        [SerializeField] private GameObject destroyed;
        [SerializeField] private ArmorVisualPhase initialVisual = ArmorVisualPhase.Ok;

        public ArmorVisualPhase CurrentVisual { get; private set; } = ArmorVisualPhase.Ok;

        public bool ConfigureVisuals(GameObject okObject, GameObject damagedObject, GameObject destroyedObject)
        {
            // Reject invalid refs BEFORE replacing the last valid configuration.
            if (okObject == null || damagedObject == null || destroyedObject == null ||
                okObject == damagedObject || okObject == destroyedObject || damagedObject == destroyedObject)
                return false;

            ok = okObject;
            damaged = damagedObject;
            destroyed = destroyedObject;
            return SetVisualPhase(initialVisual);
        }

        public bool SetVisualPhase(ArmorVisualPhase phase)
        {
            if (ok == null || damaged == null || destroyed == null ||
                ok == damaged || ok == destroyed || damaged == destroyed ||
                (int)phase < 0 || (int)phase > 2)
                return false;
            // Visual state only. NEVER invoke gameplay damage/ledger/repair state.
            ok.SetActive(phase == ArmorVisualPhase.Ok);
            damaged.SetActive(phase == ArmorVisualPhase.Damaged);
            destroyed.SetActive(phase == ArmorVisualPhase.Destroyed);
            CurrentVisual = phase;
            return true;
        }

        private void Awake()
        {
            // This is read-only with respect to combat authority and save data.
            SetVisualPhase(initialVisual);
        }
    }
}
