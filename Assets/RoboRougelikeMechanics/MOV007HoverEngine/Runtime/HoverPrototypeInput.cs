using UnityEngine;

namespace RoboRougelike.Mechanics.MOV007
{
    /// <summary>OPTIONAL standalone demo adapter. Production should wire its own Input Actions.</summary>
    [DisallowMultipleComponent]
    public sealed class HoverPrototypeInput : MonoBehaviour
    {
        public HoverVehicleController controller;
        private void Awake()
        {
            if (controller == null) controller = GetComponent<HoverVehicleController>();
        }
        private void Update()
        {
            if (controller == null) return;
#if ENABLE_LEGACY_INPUT_MANAGER
            Vector2 move = Vector2.zero;
            if (Input.GetKey(KeyCode.W)) move.y += 1f;
            if (Input.GetKey(KeyCode.S)) move.y -= 1f;
            if (Input.GetKey(KeyCode.D)) move.x += 1f;
            if (Input.GetKey(KeyCode.A)) move.x -= 1f;
            float turn = (Input.GetKey(KeyCode.E) ? 1f : 0f) - (Input.GetKey(KeyCode.Q) ? 1f : 0f);
            controller.SetMoveInput(move);
            controller.SetTurnInput(turn);
            controller.SetBrakeInput(Input.GetKey(KeyCode.Space));
#else
            // No legacy input enabled: use Codex's Input System adapter instead.
            controller.ClearInput();
#endif
        }
        private void OnDisable() { if (controller != null) controller.ClearInput(); }
    }
}
