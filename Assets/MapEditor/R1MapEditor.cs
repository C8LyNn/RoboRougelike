using System;
using System.Collections;
using UnityEngine;
using RoboRougelike.MapEditor;
using RoboRougelike.R3;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace RoboRougelike.R1
{
    public sealed partial class R1RunController
    {
        public bool MapEditorSession {get;private set;}
        public bool MapEditing {get;private set;}
        public bool MapEditorTesting {get;private set;}
        public MapDraft EditorDraft {get;private set;}
        public MapWorld EditorWorld {get;private set;}
        public MapItemKind EditorKind=MapItemKind.Ground;
        public bool EditorRemove;
        public int EditorRotation;
        public string EditorName="Moja mapa",EditorMessage="Wybierz element i kliknij siatkę.";
        public bool EditorTextFocused;
        GameObject editorGrid,editorGround;
        Vector3 editorCenter;
        float editorYaw=0,editorPitch=60,editorDistance=28;
        bool editorOrbit;

        public bool OpenMapEditor()
        {
            if(Progress?.State!=R1RunState.Ready||!OpenSandboxGarage())return false;
            MapEditorSession=true;EditorDraft=new MapDraft();EditorWorld=gameObject.AddComponent<MapWorld>();
            editorGround=GameObject.Find("P12_Ground");MakeEditorGrid();return ReturnToMapEditorFromGarage();
        }
        void MakeEditorGrid()
        {
            editorGrid=new GameObject("Siatka edytora map");editorGrid.transform.SetParent(transform,false);
            for(int i=-20;i<=20;i++)for(int axis=0;axis<2;axis++)
            {
                var line=GameObject.CreatePrimitive(PrimitiveType.Cube);line.transform.SetParent(editorGrid.transform,false);
                line.GetComponent<Collider>().enabled=false;line.GetComponent<Renderer>().sharedMaterial=palette.Parts[0];
                line.transform.localPosition=axis==0?new Vector3(i*MapWorld.Grid,-.12f,0):new Vector3(0,-.12f,i*MapWorld.Grid);
                line.transform.localScale=axis==0?new Vector3(.035f,.015f,164):new Vector3(164,.015f,.035f);
            }
        }
        public bool ReturnToMapEditorFromGarage()
        {
            if(!MapEditorSession||Progress?.State!=R1RunState.Garage||Transitioning)return false;
            Garage.Close();SetSimulation(false);if(Player!=null)Player.gameObject.SetActive(false);
            if(editorGround!=null)editorGround.SetActive(false);followCamera.enabled=false;
            MapEditing=true;MapEditorTesting=false;editorGrid.SetActive(true);EditorWorld.Build(EditorDraft,palette);UpdateEditorCamera();return true;
        }
        public void MapEditorGarage()
        {
            if(!MapEditing||Transitioning)return;MapEditing=false;editorGrid.SetActive(false);
            if(EditorWorld.Layout!=null)EditorWorld.Layout.gameObject.SetActive(false);
            if(editorGround!=null)editorGround.SetActive(true);Player.gameObject.SetActive(true);followCamera.enabled=true;Garage.Open();
        }
        public bool TestEditedMap()
        {
            if(!MapEditing||Transitioning||VehicleModel==null)return false;
            string error=EditorDraft.TestError()??VehicleModel.DeployError;
            if(error!=null){EditorMessage=error;return false;}
            workshopModel=VehicleModel;testStats=Progress.Stats.Copy();workshopPosition=Player.transform.position;workshopRotation=Player.transform.rotation;
            if(!Progress.BeginTest())return false;
            MapEditing=false;MapEditorTesting=true;editorGrid.SetActive(false);StartCoroutine(BuildEditedMapTest());return true;
        }
        IEnumerator BuildEditedMapTest()
        {
            Transitioning=true;Paused=false;RangeDisabled=false;SetSimulation(false);
            if(Player!=null){Player.gameObject.SetActive(false);Destroy(Player.gameObject);}
            R1EncounterManager.ClearTransientObjects();VehicleModel=workshopModel.DeepCopy();
            EditorWorld.Build(EditorDraft,palette);var spawn=EditorDraft.Spawn;
            Player=R3VehicleAssembly.Build(VehicleModel,testStats,palette,MapWorld.Position(spawn)+Vector3.up*.7f,Quaternion.Euler(0,spawn.rotation*90,0));
            followCamera.enabled=true;followCamera.Configure(Player.transform,Player.CameraPivot);
            yield return null;yield return null;Transitioning=false;SetSimulation(true);
        }
        public bool ReturnFromEditedMapTest()
        {
            if(!MapEditorTesting||Transitioning||workshopModel==null)return false;
            StartCoroutine(RestoreEditedMap());return true;
        }
        IEnumerator RestoreEditedMap()
        {
            Transitioning=true;Paused=false;RangeDisabled=false;SetSimulation(false);
            if(Player!=null){Player.gameObject.SetActive(false);Destroy(Player.gameObject);}
            R1EncounterManager.ClearTransientObjects();VehicleModel=workshopModel;workshopModel=null;Progress.EndTest();
            Player=R3VehicleAssembly.Build(VehicleModel,Progress.Stats,palette,workshopPosition,workshopRotation);
            followCamera.Configure(Player.transform,Player.CameraPivot);Player.gameObject.SetActive(false);followCamera.enabled=false;
            EditorWorld.Build(EditorDraft,palette);editorGrid.SetActive(true);MapEditorTesting=false;MapEditing=true;
            yield return null;yield return null;Transitioning=false;UpdateEditorCamera();
        }
        public void SaveEditedMap()
        {
            if(!MapStorage.TryPath(MapStorage.DirectoryPath,EditorName,out var path)){EditorMessage="Nazwa: litery, cyfry, spacja, - lub _.";return;}
            try{MapStorage.Save(path,EditorDraft);EditorMessage="Zapisano: "+EditorName;}
            catch(Exception e){EditorMessage="Nie udało się zapisać: "+e.Message;}
        }
        public void LoadEditedMap()
        {
            if(!MapStorage.TryPath(MapStorage.DirectoryPath,EditorName,out var path)){EditorMessage="Niepoprawna nazwa mapy.";return;}
            if(MapStorage.Load(path,EditorDraft,out var error)){EditorWorld.Build(EditorDraft,palette);EditorMessage="Wczytano: "+EditorName;}
            else EditorMessage=error;
        }
        public void ChangeEditedMap(bool undo)
        {if((undo?EditorDraft.Undo():EditorDraft.Redo())){EditorWorld.Build(EditorDraft,palette);EditorMessage=undo?"Cofnięto zmianę.":"Ponowiono zmianę.";}}
        void UpdateEditorCamera()
        {
            if(!MapEditing||Camera.main==null)return;
            var rotation=Quaternion.Euler(editorPitch,editorYaw,0);
            Camera.main.transform.SetPositionAndRotation(editorCenter+rotation*Vector3.back*editorDistance,rotation);
        }
        void MapEditorTick()
        {
            if(!MapEditing||Paused||Transitioning)return;
#if ENABLE_INPUT_SYSTEM
            var k=Keyboard.current;if(k!=null&&!EditorTextFocused)
            {
                var delta=new Vector2((k.dKey.isPressed?1:0)-(k.aKey.isPressed?1:0),(k.wKey.isPressed?1:0)-(k.sKey.isPressed?1:0));
                var q=Quaternion.Euler(0,editorYaw,0);editorCenter+=q*new Vector3(delta.x,0,delta.y)*Time.unscaledDeltaTime*editorDistance*.75f;
            }
#endif
            UpdateEditorCamera();
        }
        public void MapEditorPointer(Event e,Vector2 raw)
        {
            if(!MapEditing||Paused||Transitioning||Camera.main==null)return;
            float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);
            Vector2 virtualPos=(raw-new Vector2((Screen.width-1280*scale)*.5f,(Screen.height-720*scale)*.5f))/scale;
            if(e.type==EventType.KeyDown&&string.IsNullOrEmpty(GUI.GetNameOfFocusedControl()))
            {
                if(e.keyCode==KeyCode.V){EditorRotation=(EditorRotation+1)%4;e.Use();return;}
                if(e.control&&e.keyCode==KeyCode.Z){ChangeEditedMap(!e.shift);e.Use();return;}
                if(e.control&&e.keyCode==KeyCode.Y){ChangeEditedMap(false);e.Use();return;}
            }
            if(e.type==EventType.MouseUp&&e.button==1)editorOrbit=false;
            if(virtualPos.x<325||virtualPos.y>675)return;
            if(e.type==EventType.MouseDown&&e.button==1){editorOrbit=true;e.Use();return;}
            if(e.type==EventType.MouseDrag&&e.button==1&&editorOrbit){editorYaw+=e.delta.x*.4f;editorPitch=Mathf.Clamp(editorPitch-e.delta.y*.35f,20,85);UpdateEditorCamera();e.Use();return;}
            if(e.type==EventType.ScrollWheel){editorDistance=Mathf.Clamp(editorDistance+e.delta.y*1.2f,8,90);UpdateEditorCamera();e.Use();return;}
            if(e.type!=EventType.MouseDown||e.button!=0)return;
            GUI.FocusControl(null);EditorTextFocused=false;
            var ray=Camera.main.ScreenPointToRay(new Vector2(raw.x,Screen.height-raw.y));
            if(new Plane(Vector3.up,Vector3.zero).Raycast(ray,out float distance))
            {
                var cell=MapWorld.Cell(ray.GetPoint(distance));
                bool changed=EditorRemove?EditorDraft.Remove(cell.x,cell.y):EditorDraft.Place(EditorKind,cell.x,cell.y,EditorRotation);
                if(changed){EditorWorld.Build(EditorDraft,palette);EditorMessage=EditorRemove?"Usunięto element.":"Umieszczono element.";}
                else EditorMessage="Nie można zmienić tego pola.";
            }
            e.Use();
        }
    }
}
