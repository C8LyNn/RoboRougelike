using RoboRougelike.R1;
using RoboRougelike.R4;
using UnityEngine;

namespace RoboRougelike.R3
{
    // A world representation of one item already present in the encounter's
    // canonical ledger. The ledger, not this object, transfers ownership.
    public sealed class R3PhysicalLoot : MonoBehaviour
    {
        const int MaxContacts=4096;
        Collider[] contacts=new Collider[128];
        R4SalvageLedger ledger;
        string itemId;
        Material material;
        public string ItemId=>itemId;
        public static void Spawn(Vector3 position,R4SalvageLedger source,string id)
        {
            if(source==null||string.IsNullOrWhiteSpace(id))return;
            var item=GameObject.CreatePrimitive(PrimitiveType.Cube);item.name="Łup — część";
            item.transform.position=position+Vector3.up*.5f;item.transform.localScale=Vector3.one*.35f;
            var pickup=item.AddComponent<R3PhysicalLoot>();pickup.ledger=source;pickup.itemId=id;
            var shader=Shader.Find("Universal Render Pipeline/Lit");
            if(shader!=null){pickup.material=new Material(shader);pickup.material.SetColor("_BaseColor",new Color(1f,.67f,.09f));item.GetComponent<Renderer>().sharedMaterial=pickup.material;}
            var body=item.AddComponent<Rigidbody>();body.mass=.25f;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            body.AddForce(new Vector3(Random.Range(-1f,1f),2f,Random.Range(-1f,1f)),ForceMode.Impulse);
            if(!source.MarkPhysicalDrop(id)){item.SetActive(false);Destroy(item);}
        }
        void FixedUpdate()
        {
            var run=R1RunController.Instance;
            if(ledger==null||run==null||run.Salvage!=ledger||run.Progress?.State!=R1RunState.Combat||run.Player==null)return;
            while(true)
            {
                int count=Physics.OverlapSphereNonAlloc(transform.position,1.1f,contacts,~0,QueryTriggerInteraction.Ignore);
                if(TryCollect(contacts,count,run))return;
                if(count<contacts.Length){System.Array.Clear(contacts,0,count);return;}
                if(contacts.Length>=MaxContacts)
                {System.Array.Clear(contacts,0,contacts.Length);var all=Physics.OverlapSphere(transform.position,1.1f,~0,QueryTriggerInteraction.Ignore);TryCollect(all,all.Length,run);return;}
                System.Array.Resize(ref contacts,Mathf.Min(contacts.Length*2,MaxContacts));
            }
        }
        bool TryCollect(Collider[] hits,int count,R1RunController run)
        {
            for(int i=0;i<count;i++)
            {
                var assembly=hits[i]!=null?hits[i].GetComponentInParent<R3VehicleAssembly>():null;
                if(assembly==null||assembly.gameObject!=run.Player.gameObject||assembly.Model!=run.VehicleModel)continue;
                if(!ledger.TryCollectPhysical(assembly.Model,itemId))return false;
                gameObject.SetActive(false);Destroy(gameObject);return true;
            }
            return false;
        }
        void OnDestroy(){if(material!=null)Destroy(material);}
    }
}
