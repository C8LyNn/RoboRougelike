using UnityEngine;

namespace B01.MAPGEN001
{
    /// <summary>
    /// Demo-only, non-authoritative visualization. Never adds gameplay colliders
    /// or modifies the planner, route validator, or generated physical geometry.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MapGenDemoVisuals : MonoBehaviour
    {
        private Transform trackedRover;
        private Camera trackedCamera;
        private static readonly Vector3 CameraOffset = new Vector3(0f, 24f, -19f);

        public void Show(MapResult result, Rigidbody rover, Camera view)
        {
            if (result == null || result.GeneratedRoot == null || rover == null || view == null)
                return;

            trackedRover = rover.transform;
            trackedCamera = view;
            var root = result.GeneratedRoot.transform;
            AddGroundMarker(root, result.Start, "START_GREEN", new Color(.12f, .84f, .40f), 1.40f);
            AddGroundMarker(root, result.Finish, "FINISH_CYAN", new Color(.13f, .77f, .95f), 1.40f);

            if (result.Spawns != null)
                for (int i = 0; i < result.Spawns.Length; i++)
                    AddGroundMarker(root, result.Spawns[i], "ENEMY_RED_" + i,
                        new Color(.90f, .26f, .31f), .55f);

            if (result.CheckpointDescriptors != null)
                for (int i = 0; i < result.CheckpointDescriptors.Length; i++)
                    AddGroundMarker(root, result.CheckpointDescriptors[i], "CHECKPOINT_YELLOW_" + i,
                        new Color(1f, .76f, .18f), .80f);

            SnapCamera();
        }

        private static void AddGroundMarker(Transform root, Vector3 worldPosition,
            string markerName, Color color, float radius)
        {
            // A flat cylinder is a VISUAL label, not another test obstacle.
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "VISUAL_ONLY_" + markerName;
            var hitbox = marker.GetComponent<Collider>();
            if (hitbox != null)
            {
                hitbox.enabled = false; // synchronous, also during the Destroy delay
                Destroy(hitbox);
            }

            marker.transform.SetParent(root, false);
            Vector3 local = root.InverseTransformPoint(worldPosition);
            local.y = .07f; // floor top of the current graybox primitives = local Y 0
            marker.transform.localPosition = local;
            marker.transform.localRotation = Quaternion.identity;
            marker.transform.localScale = new Vector3(radius, .025f, radius);

            var renderer = marker.GetComponent<Renderer>();
            if (renderer != null)
            {
                // Supports both Standard-like and URP _BaseColor properties.
                // The shared physical geometry and shared materials are unchanged.
                var properties = new MaterialPropertyBlock();
                properties.SetColor("_Color", color);
                properties.SetColor("_BaseColor", color);
                renderer.SetPropertyBlock(properties);
            }
        }

        private void SnapCamera()
        {
            if (trackedRover == null || trackedCamera == null) return;
            trackedCamera.transform.position = trackedRover.position + CameraOffset;
            trackedCamera.transform.LookAt(trackedRover.position + Vector3.forward * 7f);
        }

        private void LateUpdate()
        {
            if (trackedRover == null || trackedCamera == null) return;
            Vector3 destination = trackedRover.position + CameraOffset;
            float smoothing = 1f - Mathf.Exp(-5f * Time.deltaTime);
            trackedCamera.transform.position = Vector3.Lerp(
                trackedCamera.transform.position, destination, smoothing);
            trackedCamera.transform.LookAt(trackedRover.position + Vector3.forward * 7f);
        }
    }
}
