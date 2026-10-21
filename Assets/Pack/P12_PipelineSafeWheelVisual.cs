using UnityEngine;

namespace RogueMachine.P12PipelineSafe
{
    public class P12PipelineSafeWheelVisual : MonoBehaviour
    {
        private P12PipelineSafeTankController tank;
        private float radius = 0.43f;

        public void Configure(
            P12PipelineSafeTankController controller,
            float wheelRadius)
        {
            tank = controller;
            radius = Mathf.Max(0.01f, wheelRadius);
        }

        private void LateUpdate()
        {
            if (tank == null)
                return;

            float degreesPerSecond =
                (tank.CurrentForwardSpeed / radius) *
                Mathf.Rad2Deg;

            transform.Rotate(
                Vector3.up,
                degreesPerSecond * Time.deltaTime,
                Space.Self
            );
        }
    }
}
