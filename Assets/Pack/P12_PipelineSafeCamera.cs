using System;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace RogueMachine.P12PipelineSafe
{
    public class P12PipelineSafeCamera : MonoBehaviour
    {
        [SerializeField] private Transform targetRoot;
        [SerializeField] private Transform pivot;
        [SerializeField] private float distance = 7.8f;
        [SerializeField] private float minDistance = 3f;
        [SerializeField] private float maxDistance = 12f;
        [SerializeField] private float mouseSensitivity = 0.12f;
        [SerializeField] private float minPitch = -5f;
        [SerializeField] private float maxPitch = 58f;
        [SerializeField] private float zoomSpeed = 0.015f;
        [SerializeField] private float cameraRadius = 0.28f;
        [SerializeField] private float wallPadding = 0.18f;

        private float yaw;
        private Vector3 smoothPivot,pivotVelocity;
        private bool pivotInitialized;
        private float pitch = 16f;
        private bool cursorLocked = true;
        private bool gameplayInputEnabled = true;
        public Transform Pivot => pivot;
        public Transform TargetRoot => targetRoot;

        public void SetGameplayInputEnabled(bool active)
        {
            gameplayInputEnabled = active;
            cursorLocked = active;
            ApplyCursor();
        }

        public void Configure(Transform root, Transform cameraPivot)
        {
            targetRoot = root;
            pivot = cameraPivot;
            pivotInitialized=false;
        }

        private void Start()
        {
            if (targetRoot != null)
                yaw = targetRoot.eulerAngles.y;

            ApplyCursor();
            SnapNow();
        }

        private void Update()
        {
            if (!gameplayInputEnabled) return;
#if ENABLE_INPUT_SYSTEM
            bool escape =
                Keyboard.current != null &&
                Keyboard.current.escapeKey.wasPressedThisFrame;

            Vector2 mouseDelta =
                Mouse.current != null ?
                Mouse.current.delta.ReadValue() :
                Vector2.zero;

            float scroll =
                Mouse.current != null ?
                Mouse.current.scroll.ReadValue().y :
                0f;
#else
            bool escape = Input.GetKeyDown(KeyCode.Escape);
            Vector2 mouseDelta =
                new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 8f;
            float scroll = Input.mouseScrollDelta.y * 120f;
#endif

            if (escape && RoboRougelike.R1.R1RunController.Instance == null)
            {
                cursorLocked = !cursorLocked;
                ApplyCursor();
            }

            if (!cursorLocked)
                return;

            yaw += mouseDelta.x * mouseSensitivity;
            pitch -= mouseDelta.y * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            if (Mathf.Abs(scroll) > 0.01f)
            {
                distance = Mathf.Clamp(
                    distance - scroll * zoomSpeed,
                    minDistance,
                    maxDistance
                );
            }
        }

        private void LateUpdate()
        {
            SnapNow();
        }

        private void SnapNow()
        {
            if (pivot == null || targetRoot == null)
                return;

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 direction = rotation * Vector3.back;
            if(!pivotInitialized){smoothPivot=pivot.position;pivotVelocity=Vector3.zero;pivotInitialized=true;}
            else smoothPivot=Vector3.SmoothDamp(smoothPivot,pivot.position,ref pivotVelocity,RoboRougelike.R6.R6Tuning.Current.CameraSmoothSeconds,Mathf.Infinity,Time.deltaTime);

            float safeDistance =
                FindSafeDistance(
                    smoothPivot,
                    direction,
                    distance
                );

            transform.position =
                smoothPivot +
                direction * safeDistance;

            transform.rotation = rotation;
        }

        private float FindSafeDistance(
            Vector3 origin,
            Vector3 direction,
            float requestedDistance)
        {
            RaycastHit[] hits = Physics.SphereCastAll(
                origin,
                cameraRadius,
                direction,
                requestedDistance,
                ~0,
                QueryTriggerInteraction.Ignore
            );

            Array.Sort(
                hits,
                (a, b) => a.distance.CompareTo(b.distance)
            );

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == null)
                    continue;

                Transform t = hit.collider.transform;
                if(hit.collider.GetComponentInParent<RoboRougelike.R3.R3Debris>()!=null)continue;

                if (t == targetRoot || t.IsChildOf(targetRoot))
                    continue;

                return Mathf.Clamp(
                    hit.distance - wallPadding,
                    0.65f,
                    requestedDistance
                );
            }

            return requestedDistance;
        }

        private void ApplyCursor()
        {
            Cursor.lockState =
                cursorLocked ?
                CursorLockMode.Locked :
                CursorLockMode.None;

            Cursor.visible = !cursorLocked;
        }
    }
}
