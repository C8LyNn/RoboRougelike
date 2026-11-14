using RoboRougelike.R3;
using UnityEngine;

namespace RoboRougelike.R1
{
    public sealed partial class R1RunController
    {
        // Isolated from all battle arenas; this is provisional blockout art.
        static readonly Vector3 SafeZoneOrigin=new Vector3(3000,0,3000);
        public static Vector3 SafeZoneSpawn=>SafeZoneOrigin+new Vector3(0,.7f,-12);
        GameObject safeZoneRoot,safeZoneVendor,safeZoneArena;
        bool safeZoneArenaWasActive;
        public bool SafeZoneTrading {get;private set;}
        public bool NearSafeZoneVendor
        {
            get
            {
                if(Progress?.State!=R1RunState.SafeZone||Player==null||safeZoneVendor==null)return false;
                var body=Player.GetComponent<Rigidbody>();if(body==null)return false;
                var difference=body.position-safeZoneVendor.transform.position;
                return Mathf.Abs(difference.y)<5f&&new Vector2(difference.x,difference.z).sqrMagnitude<=36f;
            }
        }
        GameObject SafeZonePrimitive(string title,PrimitiveType primitive,Vector3 position,Vector3 scale,Color color,bool solid=true)
        {
            var item=GameObject.CreatePrimitive(primitive);item.name=title;
            item.transform.SetParent(safeZoneRoot.transform,false);item.transform.localPosition=position;item.transform.localScale=scale;
            var collider=item.GetComponent<Collider>();if(collider!=null)collider.enabled=solid;
            var renderer=item.GetComponent<Renderer>();renderer.sharedMaterial=palette.Parts[(int)R3PartKind.Armor];
            var tint=new MaterialPropertyBlock();tint.SetColor("_BaseColor",color);renderer.SetPropertyBlock(tint);
            return item;
        }
        void BuildSafeZone()
        {
            safeZoneRoot=new GameObject("Bezpieczna strefa • prototyp");safeZoneRoot.transform.position=SafeZoneOrigin;
            SafeZonePrimitive("Podłoga strefy",PrimitiveType.Cube,new Vector3(0,-.5f,0),new Vector3(80,1,80),new Color(.25f,.34f,.38f));
            var walls=new Color(.13f,.2f,.24f);
            SafeZonePrimitive("Ściana strefy N",PrimitiveType.Cube,new Vector3(0,2,40),new Vector3(82,4,1),walls);
            SafeZonePrimitive("Ściana strefy S",PrimitiveType.Cube,new Vector3(0,2,-40),new Vector3(82,4,1),walls);
            SafeZonePrimitive("Ściana strefy W",PrimitiveType.Cube,new Vector3(-40,2,0),new Vector3(1,4,80),walls);
            SafeZonePrimitive("Ściana strefy E",PrimitiveType.Cube,new Vector3(40,2,0),new Vector3(1,4,80),walls);
            safeZoneVendor=SafeZonePrimitive("Sprzedawca bezpiecznej strefy",PrimitiveType.Capsule,new Vector3(0,1.2f,10),new Vector3(1.2f,1.2f,1.2f),new Color(.96f,.68f,.18f));
            SafeZonePrimitive("Stoisko sprzedawcy",PrimitiveType.Cube,new Vector3(0,.65f,12),new Vector3(5,1.3f,1.3f),new Color(.16f,.55f,.59f));
        }
        public bool EnterSafeZone()
        {
            if(Paused||QuitRequested||Progress?.State!=R1RunState.Garage||!SafeVendorAvailable||Garage==null||Player==null||followCamera==null||palette==null)return false;
            Transitioning=true;SetSimulation(false);Garage.Close();
            safeZoneArena=Arena?.Layout?.gameObject;safeZoneArenaWasActive=safeZoneArena!=null&&safeZoneArena.activeSelf;
            if(safeZoneArena!=null)safeZoneArena.SetActive(false);
            if(!Progress.BeginSafeZone()){ClearSafeZone();Transitioning=false;Garage.Open();return false;}
            BuildSafeZone();ResetEmergencyRescue();Player.MoveTo(SafeZoneSpawn,Quaternion.identity);
            Player.transform.SetPositionAndRotation(SafeZoneSpawn,Quaternion.identity);
            var body=Player.GetComponent<Rigidbody>();if(body!=null&&!body.isKinematic){body.linearVelocity=Vector3.zero;body.angularVelocity=Vector3.zero;}
            Physics.SyncTransforms();followCamera.Configure(Player.transform,Player.CameraPivot);
            Transitioning=false;SetSimulation(true);return true;
        }
        public bool OpenSafeZoneTrade()
        {
            if(Transitioning||Paused||QuitRequested||SafeZoneTrading||!NearSafeZoneVendor)return false;
            SafeZoneTrading=true;SetSimulation(false);return true;
        }
        public bool CloseSafeZoneTrade()
        {
            if(!SafeZoneTrading)return false;
            SafeZoneTrading=false;if(!Paused&&!Transitioning&&!QuitRequested&&Progress?.State==R1RunState.SafeZone)SetSimulation(true);
            return true;
        }
        public bool ReturnFromSafeZone()
        {
            if(Transitioning||Paused||QuitRequested||SafeZoneTrading||Progress?.State!=R1RunState.SafeZone||Player==null)return false;
            Transitioning=true;SetSimulation(false);Player.GetComponent<R3VehicleAssembly>()?.Capture();
            Player.GetComponent<R3DeployableService>()?.ClearSpawned();R1EncounterManager.ClearTransientObjects();
            ClearSafeZone();if(!Progress.EndSafeZone()){Transitioning=false;return false;}
            Garage.Open();Transitioning=false;Garage.Message="Wrócono do garażu z bezpiecznej strefy.";return true;
        }
        void ClearSafeZone()
        {
            SafeZoneTrading=false;
            if(safeZoneRoot!=null){safeZoneRoot.SetActive(false);Destroy(safeZoneRoot);safeZoneRoot=null;safeZoneVendor=null;}
            if(safeZoneArena!=null)safeZoneArena.SetActive(safeZoneArenaWasActive);
            safeZoneArena=null;
        }
    }
}
