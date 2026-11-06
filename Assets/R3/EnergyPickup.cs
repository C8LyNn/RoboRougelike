using UnityEngine;

namespace RoboRougelike.R3
{
    /// <summary>A physical energy cube produced by a defeated enemy.</summary>
    public sealed class EnergyPickup : MonoBehaviour
    {
        readonly Collider[] contacts=new Collider[128];
        Material material;
        public static void Spawn(Vector3 position)
        {
            var item=GameObject.CreatePrimitive(PrimitiveType.Cube);
            item.name="Kostka energii";item.transform.position=position+Vector3.up*.5f;
            item.transform.localScale=Vector3.one*.35f;
            var pickup=item.AddComponent<EnergyPickup>();
            var shader=Shader.Find("Universal Render Pipeline/Lit");
            if(shader!=null){pickup.material=new Material(shader);pickup.material.SetColor("_BaseColor",new Color(.03f,.48f,1f));item.GetComponent<Renderer>().sharedMaterial=pickup.material;}
            var body=item.AddComponent<Rigidbody>();body.mass=.25f;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            body.AddForce(new Vector3(Random.Range(-1f,1f),2f,Random.Range(-1f,1f)),ForceMode.Impulse);
        }
        void FixedUpdate()
        {
            int count=Physics.OverlapSphereNonAlloc(transform.position,1.1f,contacts,~0,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)
            {
                var assembly=contacts[i]!=null?contacts[i].GetComponentInParent<R3VehicleAssembly>():null;
                if(assembly==null||RoboRougelike.R1.R1RunController.Instance?.Player==null||
                    assembly.gameObject!=RoboRougelike.R1.R1RunController.Instance.Player.gameObject)continue;
                var bank=assembly.GetComponent<VehicleEnergyBank>();
                if(bank==null||bank.Maximum<=0)continue;
                bank.ApplyEnergyPickup();gameObject.SetActive(false);Destroy(gameObject);return;
            }
        }
        void OnDestroy(){if(material!=null)Destroy(material);}
    }
}