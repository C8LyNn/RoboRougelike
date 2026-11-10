using UnityEngine;

namespace RoboRougelike.R3
{
    /// <summary>A physical energy cube produced by a defeated enemy.</summary>
    public sealed class EnergyPickup : MonoBehaviour
    {
        const int MaxContactBuffer=4096;
        Collider[] contacts=new Collider[128];
        int previousContacts;
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
            while(true)
            {
                int count=Physics.OverlapSphereNonAlloc(transform.position,1.1f,contacts,~0,QueryTriggerInteraction.Ignore);
                if(TryCollect(contacts,count))return;
                if(count<contacts.Length)
                {
                    if(previousContacts>count)System.Array.Clear(contacts,count,previousContacts-count);
                    previousContacts=count;return;
                }
                if(contacts.Length>=MaxContactBuffer)
                {
                    System.Array.Clear(contacts,0,contacts.Length);previousContacts=0;
                    var all=Physics.OverlapSphere(transform.position,1.1f,~0,QueryTriggerInteraction.Ignore);
                    TryCollect(all,all.Length);return;
                }
                System.Array.Resize(ref contacts,Mathf.Min(contacts.Length*2,MaxContactBuffer));
            }
        }
        bool TryCollect(Collider[] hits,int count)
        {
            for(int i=0;i<count;i++)
            {
                var assembly=hits[i]!=null?hits[i].GetComponentInParent<R3VehicleAssembly>():null;
                if(assembly==null||RoboRougelike.R1.R1RunController.Instance?.Player==null||
                    assembly.gameObject!=RoboRougelike.R1.R1RunController.Instance.Player.gameObject)continue;
                var bank=assembly.GetComponent<VehicleEnergyBank>();
                if(bank==null||bank.Maximum<=0)continue;
                bank.ApplyEnergyPickup();gameObject.SetActive(false);Destroy(gameObject);return true;
            }
            return false;
        }
        void OnDestroy(){if(material!=null)Destroy(material);}
    }
}
