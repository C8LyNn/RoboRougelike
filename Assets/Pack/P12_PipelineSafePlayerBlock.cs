using UnityEngine;

namespace RogueMachine.P12PipelineSafe
{
    [DisallowMultipleComponent]
    public class P12_PipelineSafePlayerBlock : MonoBehaviour
    {
        [SerializeField] private Vector3Int logicalGridPosition;
        [SerializeField] private bool logicalGridAssigned;
        [SerializeField] private bool isCore;
        [SerializeField] private bool detachedFromVehicle;

        public Vector3Int LogicalGridPosition =>
            logicalGridPosition;

        public bool LogicalGridAssigned =>
            logicalGridAssigned;

        public bool IsCore =>
            isCore;

        public bool DetachedFromVehicle =>
            detachedFromVehicle;

        private void Awake()
        {
            EnsureStructureSystem();
        }

        private void Start()
        {
            EnsureStructureSystem();
        }

        public void AssignLogicalGrid(
            Vector3Int gridPosition)
        {
            if (logicalGridAssigned)
                return;

            logicalGridPosition =
                gridPosition;

            logicalGridAssigned =
                true;
        }

        public void ForceLogicalGrid(
            Vector3Int gridPosition)
        {
            logicalGridPosition =
                gridPosition;

            logicalGridAssigned =
                true;
        }

        public void SetCore(
            bool value)
        {
            isCore =
                value;

            if (value)
                detachedFromVehicle =
                    false;
        }

        public void MarkDetached()
        {
            detachedFromVehicle =
                true;

            isCore =
                false;
        }

        private void EnsureStructureSystem()
        {
            P12PipelineSafeTankController controller =
                GetComponentInParent<
                    P12PipelineSafeTankController
                >();

            if (controller == null)
                return;

            P12PipelineSafeVehicleStructure structure =
                controller.GetComponent<
                    P12PipelineSafeVehicleStructure
                >();

            if (structure == null)
            {
                structure =
                    controller.gameObject.AddComponent<
                        P12PipelineSafeVehicleStructure
                    >();
            }
        }
    }
}
