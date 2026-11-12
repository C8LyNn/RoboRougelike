using System;
using System.Collections;
using System.Linq;
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
        public bool EditorSelect;
        public MapItem EditorSelected {get;private set;}
        public int EditorRotation;
        public int EditorScaleX=1,EditorScaleY=1,EditorScaleZ=1;
        public string EditorName="Moja mapa",EditorMessage="Wybierz element i kliknij siatkę.";
        string pendingMapOverwrite,pendingMapOverwriteSnapshot;
        string savedMapSnapshot,pendingMapLoadPath,pendingMapLoadSnapshot,pendingMapExitSnapshot;
        bool pendingMapQuit;
        public bool MapLoadPending=>pendingMapLoadPath!=null;
        public bool MapExitPending=>pendingMapExitSnapshot!=null;
        public bool MapReturnPending=>MapExitPending&&!pendingMapQuit;
        public bool MapQuitPending=>MapExitPending&&pendingMapQuit;
        void CancelMapLoad(){pendingMapLoadPath=null;pendingMapLoadSnapshot=null;pendingMapExitSnapshot=null;}
        public bool MapOverwritePending => pendingMapOverwrite!=null;
        public void CancelMapOverwrite(){pendingMapOverwrite=null;pendingMapOverwriteSnapshot=null;CancelMapLoad();}
        public bool EditorTextFocused;
        GameObject editorGrid,editorGround;
        Vector3 editorCenter;
        float editorYaw=0,editorPitch=60,editorDistance=28;
        bool editorOrbit;

        public bool OpenMapEditor()
        {
            if(Progress?.State!=R1RunState.Ready||!OpenSandboxGarage())return false;
            CancelMapOverwrite();MapEditorSession=true;EditorDraft=new MapDraft();savedMapSnapshot=EditorDraft.ToJson();EditorWorld=gameObject.AddComponent<MapWorld>();
            editorGround=GameObject.Find("P12_Ground");MakeEditorGrid();return ReturnToMapEditorFromGarage();
        }
        void MakeEditorGrid()
        {
            editorGrid=new GameObject("Siatka edytora map");editorGrid.transform.SetParent(transform,false);
            for(int i=-MapDraft.MaxCoordinate;i<=MapDraft.MaxCoordinate;i++)for(int axis=0;axis<2;axis++)
            {
                var line=GameObject.CreatePrimitive(PrimitiveType.Cube);line.transform.SetParent(editorGrid.transform,false);
                line.GetComponent<Collider>().enabled=false;line.GetComponent<Renderer>().sharedMaterial=palette.Parts[0];
                line.transform.localPosition=axis==0?new Vector3(i*MapWorld.Grid,.015f,0):new Vector3(0,.015f,i*MapWorld.Grid);
                float width=(MapDraft.MaxCoordinate*2+1)*MapWorld.Grid;
                line.transform.localScale=axis==0?new Vector3(.035f,.015f,width):new Vector3(width,.015f,.035f);
            }
        }
        public bool ReturnToMapEditorFromGarage()
        {
            if(!MapEditorSession||Progress?.State!=R1RunState.Garage||Transitioning)return false;
            pendingMapExitSnapshot=null;Garage.Close();SetSimulation(false);if(Player!=null)Player.gameObject.SetActive(false);
            if(editorGround!=null)editorGround.SetActive(false);followCamera.enabled=false;
            MapEditing=true;MapEditorTesting=false;editorGrid.SetActive(true);EditorWorld.Build(EditorDraft,palette);UpdateEditorCamera();return true;
        }
        public void MapEditorGarage()
        {
            if(!MapEditing||Transitioning)return;pendingMapExitSnapshot=null;MapEditing=false;editorGrid.SetActive(false);EditorWorld.HideGhost();
            if(EditorWorld.Layout!=null)EditorWorld.Layout.gameObject.SetActive(false);
            if(editorGround!=null)editorGround.SetActive(true);Player.gameObject.SetActive(true);followCamera.enabled=true;Garage.Open();
        }
        public string MapTestReadiness()
        {
            if(EditorDraft==null)return "Brak otwartej mapy.";
            if(VehicleModel==null)return "Przygotuj pojazd w garażu.";
            return EditorDraft.TestError()??VehicleModel.DeployError;
        }
        public bool TestEditedMap()
        {
            if(!MapEditing||Transitioning||VehicleModel==null)return false;
            string error=MapTestReadiness();
            if(error!=null){EditorMessage=error;return false;}
            workshopModel=VehicleModel;testStats=Progress.Stats.Copy();workshopPosition=Player.transform.position;workshopRotation=Player.transform.rotation;
            if(!Progress.BeginTest())return false;pendingMapExitSnapshot=null;
            MapEditing=false;MapEditorTesting=true;editorGrid.SetActive(false);EditorWorld.HideGhost();StartCoroutine(BuildEditedMapTest());return true;
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
            if(!MapEditorTesting||Transitioning||workshopModel==null)return false;pendingMapExitSnapshot=null;
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
        public void ReturnFromMapEditorToMenu()
        {
            if(!MapEditing||Transitioning||EditorDraft==null)return;
            RestartRun();
        }
        static bool NeedsMapExitConfirmation(string current,string saved,string pending,bool pendingQuit,bool quit)
            =>current!=saved&&(pending!=current||pendingQuit!=quit);
        bool ConfirmMapEditorExit(bool quit)
        {
            if(!MapEditorSession||EditorDraft==null)return true;
            string current=EditorDraft.ToJson();
            if(NeedsMapExitConfirmation(current,savedMapSnapshot,pendingMapExitSnapshot,pendingMapQuit,quit))
            {
                pendingMapExitSnapshot=current;pendingMapQuit=quit;
                EditorMessage=quit?"Niezapisane zmiany mapy zostaną utracone. Wybierz ponownie wyjście z gry, aby potwierdzić.":
                    "Niezapisane zmiany mapy zostaną utracone. Wybierz ponownie powrót do menu, aby potwierdzić.";
                return false;
            }
            pendingMapExitSnapshot=null;return true;
        }
        public void SaveEditedMap()
        {
            CancelMapLoad();
            try
            {
                if(!MapStorage.TryPath(MapStorage.DirectoryPath,EditorName,out var path))
                {pendingMapOverwrite=null;pendingMapOverwriteSnapshot=null;EditorMessage="Niepoprawna lub kolidująca nazwa mapy.";return;}
                bool overwrite=System.IO.File.Exists(path);
                string current=EditorDraft.ToJson();
                if(overwrite&&(pendingMapOverwrite!=path||pendingMapOverwriteSnapshot!=current))
                {pendingMapOverwrite=path;pendingMapOverwriteSnapshot=current;EditorMessage="Mapa istnieje. Kliknij ponownie Zapisz, aby ją nadpisać.";return;}
                pendingMapOverwrite=null;pendingMapOverwriteSnapshot=null;MapStorage.Save(path,EditorDraft,overwrite);savedMapSnapshot=EditorDraft.ToJson();EditorMessage="Zapisano: "+EditorName;
            }
            catch(Exception e){pendingMapOverwrite=null;pendingMapOverwriteSnapshot=null;EditorMessage="Nie udało się zapisać: "+e.Message;}
        }
        public void LoadEditedMap()
        {
            pendingMapOverwrite=null;pendingMapOverwriteSnapshot=null;
            string path,current;
            try
            {
                if(!MapStorage.TryPath(MapStorage.DirectoryPath,EditorName,out path)){CancelMapLoad();EditorMessage="Niepoprawna nazwa mapy.";return;}
                current=EditorDraft.ToJson();
            }
            catch(Exception e){CancelMapLoad();EditorMessage="Nie udało się wczytać: "+e.Message;return;}
            if(System.IO.File.Exists(path)&&current!=savedMapSnapshot&&
                (pendingMapLoadPath!=path||pendingMapLoadSnapshot!=current))
            {
                pendingMapLoadPath=path;pendingMapLoadSnapshot=current;
                EditorMessage="Niezapisane zmiany zostaną utracone. Kliknij ponownie Wczytaj, aby potwierdzić.";return;
            }
            CancelMapLoad();
            if(MapStorage.Load(path,EditorDraft,out var error)){savedMapSnapshot=EditorDraft.ToJson();EditorSelected=null;EditorWorld.Build(EditorDraft,palette);EditorMessage="Wczytano: "+EditorName;}
            else EditorMessage=error;
        }
        public void ChangeEditedMap(bool undo)
        {if((undo?EditorDraft.Undo():EditorDraft.Redo())){CancelMapOverwrite();EditorSelected=null;EditorWorld.Build(EditorDraft,palette);EditorMessage=undo?"Cofnięto zmianę.":"Ponowiono zmianę.";}}
        public void ToggleMapFloor()
        {if(EditorDraft.SetGlobalFloor(!EditorDraft.GlobalFloor)){CancelMapOverwrite();EditorSelected=null;EditorWorld.Build(EditorDraft,palette);EditorMessage=EditorDraft.GlobalFloor?"Włączono podłoże całej mapy.":"Wyłączono podłoże całej mapy.";}}
        public void SetMapTool(bool select,bool remove)
        {EditorSelect=select;EditorRemove=remove;EditorSelected=null;EditorWorld.HideGhost();}
        public void SelectMapAt(int x,int z)
        {
            EditorSelected=EditorDraft.ItemAt(x,z);
            if(EditorSelected!=null&&EditorDraft.GlobalFloor&&EditorSelected.kind==MapItemKind.Ground)EditorSelected=null;
            if(EditorSelected==null)EditorMessage="Na tym polu nie ma elementu.";
            else{EditorRotation=EditorSelected.rotation;EditorScaleX=EditorSelected.scaleX;EditorScaleY=EditorSelected.scaleY;EditorScaleZ=EditorSelected.scaleZ;EditorMessage="Zaznaczono element. Zmień skalę lub obrót.";}
            RefreshMapSelection();
        }
        void RefreshMapSelection()
        {
            if(EditorSelect&&EditorSelected!=null&&EditorDraft.Items.Contains(EditorSelected))EditorWorld.ShowSelection(EditorSelected,palette);
            else EditorWorld.HideGhost();
        }
        public void ChangeMapScale(int axis,int direction)
        {
            if(axis<0||axis>2)return;
            int x=EditorScaleX,y=EditorScaleY,z=EditorScaleZ;
            if(axis==0)x=Mathf.Clamp(x+direction,1,MapDraft.MaxScale);
            if(axis==1)y=Mathf.Clamp(y+direction,1,MapDraft.MaxScale);
            if(axis==2)z=Mathf.Clamp(z+direction,1,MapDraft.MaxScale);
            if(x==EditorScaleX&&y==EditorScaleY&&z==EditorScaleZ)return;
            if(EditorSelect)
            {
                if(EditorSelected==null||!EditorDraft.Items.Contains(EditorSelected)){EditorSelected=null;EditorMessage="Najpierw zaznacz element.";return;}
                if(!EditorDraft.Resize(EditorSelected,EditorRotation,x,y,z)){EditorMessage="Nie można zmienić skali: pole zajęte lub granica mapy.";return;}
                CancelMapOverwrite();EditorWorld.Build(EditorDraft,palette);RefreshMapSelection();EditorMessage="Zmieniono skalę zaznaczonego elementu.";
            }
            EditorScaleX=x;EditorScaleY=y;EditorScaleZ=z;
        }
        public void RotateMapItem()
        {
            int rotation=(EditorRotation+1)%4;
            if(EditorSelect)
            {
                if(EditorSelected==null||!EditorDraft.Items.Contains(EditorSelected)){EditorSelected=null;EditorMessage="Najpierw zaznacz element.";return;}
                if(!EditorDraft.Resize(EditorSelected,rotation,EditorScaleX,EditorScaleY,EditorScaleZ)){EditorMessage="Nie można obrócić: pole zajęte lub granica mapy.";return;}
                CancelMapOverwrite();EditorWorld.Build(EditorDraft,palette);RefreshMapSelection();EditorMessage="Obrócono zaznaczony element.";
            }
            EditorRotation=rotation;
        }
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
                if(e.keyCode==KeyCode.V){RotateMapItem();e.Use();return;}
                if(e.control&&e.keyCode==KeyCode.Z){ChangeEditedMap(!e.shift);e.Use();return;}
                if(e.control&&e.keyCode==KeyCode.Y){ChangeEditedMap(false);e.Use();return;}
            }
            if(e.type==EventType.MouseUp&&e.button==1)editorOrbit=false;
            if(virtualPos.x<325||virtualPos.y>615){RefreshMapSelection();return;}
            if(e.type==EventType.MouseDown&&e.button==1){editorOrbit=true;e.Use();return;}
            if(e.type==EventType.MouseDrag&&e.button==1&&editorOrbit){EditorWorld.HideGhost();editorYaw+=e.delta.x*.4f;editorPitch=Mathf.Clamp(editorPitch-e.delta.y*.35f,20,85);UpdateEditorCamera();e.Use();return;}
            if(e.type==EventType.ScrollWheel){editorDistance=Mathf.Clamp(editorDistance+e.delta.y*1.2f,8,90);UpdateEditorCamera();e.Use();return;}
            bool click=e.type==EventType.MouseDown&&e.button==0;
            if(!click&&e.type!=EventType.Repaint&&e.type!=EventType.MouseMove)return;
            if(click){GUI.FocusControl(null);EditorTextFocused=false;}
            var ray=Camera.main.ScreenPointToRay(new Vector2(raw.x,Screen.height-raw.y));
            var hits=Physics.RaycastAll(ray,1000,~0,QueryTriggerInteraction.Ignore);
            float nearest=float.PositiveInfinity;Vector3 point=default;bool hasPoint=false;MapItem hitItem=null;
            foreach(var hit in hits)
                if(EditorWorld.Layout!=null&&hit.collider.transform.IsChildOf(EditorWorld.Layout)&&hit.distance<nearest)
                {nearest=hit.distance;point=hit.point;hasPoint=true;hitItem=hit.collider.GetComponentInParent<MapItemReference>()?.Item;}
            if(!hasPoint&&new Plane(Vector3.up,Vector3.zero).Raycast(ray,out float distance)){point=ray.GetPoint(distance);hasPoint=true;}
            if(hasPoint)
            {
                var cell=MapWorld.Cell(point);
                if((EditorRemove||EditorSelect)&&hitItem!=null)cell=new Vector2Int(hitItem.x,hitItem.z);
                if(EditorSelect)RefreshMapSelection();
                else if(EditorRemove)EditorWorld.HideGhost();
                else
                {
                    var item=new MapItem{kind=EditorKind,x=cell.x,z=cell.y,rotation=EditorRotation,scaleX=EditorScaleX,scaleY=EditorScaleY,scaleZ=EditorScaleZ};
                    bool valid=EditorDraft.CanPlace(EditorKind,cell.x,cell.y,EditorRotation,EditorScaleX,EditorScaleY,EditorScaleZ);
                    EditorWorld.ShowGhost(item,valid,palette);
                }
                if(click)
                {
                    if(EditorSelect)
                    {
                        SelectMapAt(cell.x,cell.y);
                    }
                    else
                    {
                        bool changed=EditorRemove?EditorDraft.Remove(cell.x,cell.y):EditorDraft.Place(EditorKind,cell.x,cell.y,EditorRotation,EditorScaleX,EditorScaleY,EditorScaleZ);
                        if(changed){CancelMapOverwrite();EditorSelected=null;EditorWorld.Build(EditorDraft,palette);EditorMessage=EditorRemove?"Usunięto element.":"Umieszczono element.";}
                        else EditorMessage="Nie można zmienić tego pola.";
                    }
                }
            }
            else EditorWorld.HideGhost();
            if(click)e.Use();
        }
    }
}
