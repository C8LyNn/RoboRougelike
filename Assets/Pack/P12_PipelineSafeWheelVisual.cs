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

    // Enemy tread visuals rotate separately from the collider and damageable
    // mounting block, so animation cannot change physical wheel contact.
    public sealed class P12EnemyWheelVisual : MonoBehaviour
    {
        private Renderer source,visual;
        private MaterialPropertyBlock tint;
        private float radius;

        public void Configure(Renderer sourceRenderer,float wheelRadius)
        {
            source=sourceRenderer;visual=GetComponent<Renderer>();
            radius=Mathf.Max(.01f,wheelRadius);tint=new MaterialPropertyBlock();
        }

        private void LateUpdate()
        {
            if(source==null||visual==null)return;
            source.GetPropertyBlock(tint);visual.SetPropertyBlock(tint);
            var body=source.GetComponentInParent<Rigidbody>();
            if(body==null)return;
            float speed=Vector3.Dot(body.GetPointVelocity(source.transform.position),body.transform.forward);
            transform.Rotate(Vector3.up,(speed/radius)*Mathf.Rad2Deg*Time.deltaTime,Space.Self);
        }
    }
}
