using UnityEngine;
using System.Collections.Generic;
using RoboRougelike.R1;
using RoboRougelike.R3;
using RogueMachine.P12PipelineSafe;

namespace RoboRougelike.R6
{
    [DefaultExecutionOrder(-500)]
    public sealed class R6TestRange : MonoBehaviour
    {
        public Transform Layout {get;private set;}
        public P12PipelineSafeEnemyTurret TrainingTarget {get;private set;}
        public bool TrainingFireRequested {get;private set;}
        readonly List<P12PipelineSafeEnemyTurret> creativeEnemies=new List<P12PipelineSafeEnemyTurret>();
        public int CreativeEnemyCount => creativeEnemies.FindAll(e=>e!=null&&!e.IsDefeated).Count;
        public IEnumerable<P12PipelineSafeEnemyTurret> ActiveCreativeEnemies
        {get {foreach(var enemy in creativeEnemies)if(enemy!=null&&!enemy.IsDefeated&&enemy.gameObject.activeInHierarchy)yield return enemy;}}
        public bool SpawnEnemy(P12PipelineSafeEnemyTurret template,Transform player,R3EnemyArchetype archetype,bool active)
        {
            if(Layout==null||template==null||player==null)return false;
            Vector3 spawn=Vector3.zero;bool found=false;
            for(int i=0;i<12;i++)
            {
                var direction=Quaternion.Euler(0,i*30,0)*player.forward;direction.y=0;
                if(direction.sqrMagnitude<.1f)direction=Vector3.forward;
                var p=player.position+direction.normalized*15;
                p.x=Mathf.Clamp(p.x,-140,140);p.z=Mathf.Clamp(p.z,-140,140);p.y=0;
                if(!Physics.CheckBox(p+Vector3.up*2,new Vector3(3,1.8f,3),Quaternion.identity,~0,QueryTriggerInteraction.Ignore))
                {spawn=p;found=true;break;}
            }
            if(!found)return false;
            var enemy=Instantiate(template,spawn,Quaternion.identity,Layout);
            enemy.name="Przeciwnik kreatywny • "+R3EnemyProfiles.Name(archetype);
            enemy.ConfigureArchetype(archetype);enemy.ConfigureVariant(RoboRougelike.R5.R5EnemyVariant.Normal);
            enemy.ConfigureSalvage(null,"creative-target");enemy.gameObject.SetActive(true);
            enemy.ConfigureEncounter(player,1,1,1,1);enemy.ConfigureMovement(4,12,.5f);
            var energy=enemy.GetComponent<VehicleEnergyBank>()??enemy.gameObject.AddComponent<VehicleEnergyBank>();
            energy.ConfigureEnemy(enemy.Core);enemy.EquipAdditionalGun();enemy.AttachPrimaryGunArt();
            enemy.GetComponent<R6VehicleDrive>()?.RefreshParts();
            enemy.gameObject.AddComponent<EnemySelfDestruct>().Configure(enemy,player);
            RoboRougelike.WeaponIntegration.RRLEmpVehicleAdapter.Configure(enemy.transform);
            energy.Refresh();energy.Recharge(energy.Maximum);enemy.SetCombatActive(active);
            creativeEnemies.Add(enemy);return true;
        }
        public void SetCreativeEnemiesActive(bool active)
        {foreach(var enemy in creativeEnemies)if(enemy!=null&&!enemy.IsDefeated)enemy.SetCombatActive(active);}
        public void ClearEnemies()
        {
            foreach(var enemy in creativeEnemies)if(enemy!=null){enemy.gameObject.SetActive(false);Destroy(enemy.gameObject);}
            creativeEnemies.Clear();
        }
        public void SetTrainingTarget(P12PipelineSafeEnemyTurret target)
        {TrainingTarget=target;SetTrainingFire(false);}
        public void SetTrainingFire(bool active)
        {TrainingFireRequested=active&&TrainingTarget!=null&&!TrainingTarget.IsDefeated;RefreshTrainingFire();}
        void RefreshTrainingFire()
        {
            var run=R1RunController.Instance;
            if(TrainingTarget!=null)TrainingTarget.SetCombatActive(TrainingFireRequested&&!TrainingTarget.IsDefeated&&
                run!=null&&run.Progress.State==R1RunState.TestDrive&&!run.Paused&&!run.Transitioning);
        }
        void Update()=>RefreshTrainingFire();
        Material material;
        public void Build(R3VisualPalette palette,bool creative=false)
        {
            Clear();Layout=new GameObject("Poligon R6").transform;
            Layout.SetParent(transform,false);material=palette.Parts[0];
            float edge=creative?150:40;
            if(creative)Box("Podłoże kreatywnego poligonu",new Vector3(0,-.5f,0),new Vector3(300,1,300));
            Box("Bariera północna",new Vector3(0,2,edge),new Vector3(edge*2+2,4,2));
            Box("Bariera południowa",new Vector3(0,2,-edge),new Vector3(edge*2+2,4,2));
            Box("Bariera zachodnia",new Vector3(-edge,2,0),new Vector3(2,4,edge*2));
            Box("Bariera wschodnia",new Vector3(edge,2,0),new Vector3(2,4,edge*2));
            Box("Ściana zderzeniowa",new Vector3(23,2,-4),new Vector3(3,4,18));
            for(int i=0;i<4;i++)Box("Slalom "+(i+1),new Vector3(i%2==0?-6:6,1,-10+i*7),new Vector3(2,2,2));
            for(int i=0;i<3;i++)Target(new Vector3((i-1)*13,.65f,22));
            foreach(int side in new[]{-1,1})
            {
                var ramp=GameObject.CreatePrimitive(PrimitiveType.Cube);ramp.name="Rampa terenu • poligon";
                ramp.transform.SetParent(Layout,false);ramp.transform.localPosition=new Vector3(side*18,1.53f,10);
                ramp.transform.localRotation=Quaternion.Euler(18,0,0);ramp.transform.localScale=new Vector3(8,.35f,11);
                ramp.GetComponent<Renderer>().sharedMaterial=material;
            }
            RoboRougelike.MOV009Integration.MOV009TerrainCourse.BuildPracticeCourse(Layout,material);
            RoboRougelike.MOVSeries.MOVSeriesRange.Build(Layout,palette);
            var dummy=R3VehicleAssembly.Build(new R3VehicleModel(R3VehicleCatalog.Create(R3VehicleKind.Standard)),new R1RunStats(),palette,new Vector3(-18,.7f,-5),Quaternion.identity);
            dummy.name="Pojazd do przepychania";dummy.transform.SetParent(Layout,true);dummy.Controller.SetGameplayInputEnabled(false);
            foreach(var r in dummy.GetComponentsInChildren<Renderer>())Tint(r,new Color(.8f,.32f,.17f));
            Physics.SyncTransforms();
        }
        void Target(Vector3 position)
        {
            var target=new GameObject("Cel blokowy");target.transform.SetParent(Layout,false);target.transform.position=position;
            target.AddComponent<Rigidbody>().isKinematic=true;P12_PipelineSafePlayerBlock core=null;
            for(int x=-2;x<=2;x++)for(int y=0;y<3;y++)
            {
                var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=x==0&&y==0?"Rdzeń celu":"Pancerz celu";
                obj.transform.SetParent(target.transform,false);obj.transform.localPosition=new Vector3(x*.55f,y*.55f,0);obj.transform.localScale=Vector3.one*.52f;
                obj.GetComponent<Renderer>().sharedMaterial=material;Tint(obj.GetComponent<Renderer>(),x==0&&y==0?Color.cyan:new Color(.9f,.55f,.16f));
                var hp=obj.AddComponent<P12PipelineSafeDamageable>();hp.Configure(x==0&&y==0?300:100,true);
                hp.SetBaseTint(x==0&&y==0?Color.cyan:new Color(.9f,.55f,.16f));
                var block=obj.AddComponent<P12_PipelineSafePlayerBlock>();if(x==0&&y==0)core=block;
            }
            var graph=target.AddComponent<P12PipelineSafeVehicleStructure>();graph.SetDesignatedCore(core);graph.RebuildNow();
        }
        void Box(string name,Vector3 position,Vector3 size)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;obj.transform.SetParent(Layout,false);
            obj.transform.localPosition=position;obj.transform.localScale=size;obj.GetComponent<Renderer>().sharedMaterial=material;
            Tint(obj.GetComponent<Renderer>(),new Color(.28f,.38f,.4f));
        }
        static void Tint(Renderer renderer,Color color)
        {var props=new MaterialPropertyBlock();props.SetColor("_BaseColor",color);renderer.SetPropertyBlock(props);renderer.GetComponent<P12PipelineSafeDamageable>()?.SetBaseTint(color);}
        public void Clear(){ClearEnemies();SetTrainingFire(false);TrainingTarget=null;if(Layout!=null){Layout.gameObject.SetActive(false);Destroy(Layout.gameObject);Layout=null;}}
        void OnDestroy(){Clear();}
    }
}
