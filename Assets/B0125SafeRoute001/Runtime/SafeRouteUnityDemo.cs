using System.Collections.Generic;
using UnityEngine;
using B01.LevelChallenges.SafeRoute;

namespace B0125.SafeRoute
{
    /// <summary>
    /// Physically constructs a standalone 15x11 Unity6 traversal scenario.
    /// Original BFS selects conservative Light/Heavy safe routes for both gate
    /// and hazard states. Real BoxCollider moving doors and hazard force are
    /// independently visible; NOT an integrated canonical player controller.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SafeRouteUnityDemo : MonoBehaviour
    {
        private const float CellSize=2f;
        private LevelGrid level;
        private SafeRoutePlan current;
        private RouteAudit audit;
        private Rigidbody player;
        private readonly List<Rigidbody> gates=new List<Rigidbody>();
        private readonly List<Collider> obstacles=new List<Collider>();
        private readonly List<Collider> hazardPads=new List<Collider>();
        private readonly List<Renderer> hazardPaint=new List<Renderer>();
        private readonly List<GameObject> routeMarkers=new List<GameObject>();
        private bool heavy,gateDesiredOpen,hazardActive,paused,physicalGateClear;
        private Vector2 drive;
        private int respawns;
        private Rigidbody physicalCrate;
        private Collider crateCollider;
        private Rigidbody auxiliaryCrate;
        private Collider auxiliaryCollider;
        private GridCell observedCrateCell;
        private GridCell observedAuxiliaryCell;
        private Vector3 observedCratePose;
        private Vector3 observedAuxiliaryPose;
        private bool occupancyUncertain;
        private int replanRevision;
        private GridCell[] occupiedCells=System.Array.Empty<GridCell>();
        private int rejectedCrateMoves;
        public Rigidbody PhysicalCrate=>physicalCrate;
        public Rigidbody AuxiliaryCrate=>auxiliaryCrate;
        public GridCell AuxiliaryCrateCell=>observedAuxiliaryCell;
        public bool OccupancyUncertain=>occupancyUncertain;
        public int ReplanRevision=>replanRevision;
        public GridCell CrateCell=>observedCrateCell;
        public int RejectedCrateMoves=>rejectedCrateMoves;
        public int PhysicalBlockedCells=>occupiedCells.Length;
        private GridCell routeOrigin;
        private bool hasRouteOrigin;
        public GridCell RouteOrigin=>routeOrigin;
        public bool CurrentRouteFromPlayer=>current!=null&&current.Reachable&&
            current.Cells.Length>0&&current.Cells[0].Equals(routeOrigin);
        public Rigidbody Player=>player;
        public SafeRoutePlan CurrentPlan=>current;
        public RouteAudit Audit=>audit;
        public bool Heavy=>heavy;
        public bool GatePhysicallyClear=>physicalGateClear;
        public bool HazardActive=>hazardActive;
        public int Respawns=>respawns;
        public int GateCount=>gates.Count;
        public int WallCount=>obstacles.Count;
        public int HazardPadCount=>hazardPads.Count;

        private Vector3 Center(GridCell p,float y=0)
        {
            return new Vector3((p.X-level.Width/2)*CellSize,y,
                (p.Z-level.Height/2)*CellSize);
        }
        // Inverse of Center(): exact Unity world-to-cell mapping. A player
        // physically exiting the bounded level yields a fail-closed no-route.
        private GridCell ActualPlayerCell()
        {
            Vector3 pos=player.position;
            return new GridCell(Mathf.RoundToInt(pos.x/CellSize)+level.Width/2,
                Mathf.RoundToInt(pos.z/CellSize)+level.Height/2);
        }
        private GameObject Slab(string name,Vector3 center,Vector3 size,Color color)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name=name;go.transform.SetParent(transform,false);
            go.transform.position=center;go.transform.localScale=size;
            go.GetComponent<Renderer>().material.color=color;
            return go;
        }
        private void Awake()
        {
            level=SafeRouteExample.Create();
            audit=SafeRoutePlanner.Audit(level);
            if(!audit.AllReachable)
                throw new System.InvalidOperationException(
                    "Level cannot be staged: one Light/Heavy gate/hazard state softlocks");
            Slab("Actual level-wide floor",Vector3.down*.25f,
                new Vector3(level.Width*CellSize,.5f,level.Height*CellSize),Color.gray);
            for(int z=0;z<level.Height;z++)
            for(int x=0;x<level.Width;x++)
            {
                char tile=level.At(x,z);
                var p=new GridCell(x,z);
                if(tile=='#')
                {
                    var wall=Slab("PHYSICAL_WALL_"+x+"_"+z,Center(p,1.4f),
                        new Vector3(CellSize,2.8f,CellSize),Color.gray);
                    obstacles.Add(wall.GetComponent<BoxCollider>());
                }
                else if(tile=='G')
                {
                    var door=Slab("REAL_KINEMATIC_GATE_"+x+"_"+z,Center(p,1.4f),
                        new Vector3(CellSize,2.8f,CellSize),Color.red);
                    var body=door.AddComponent<Rigidbody>();
                    body.isKinematic=true;body.useGravity=false;
                    body.interpolation=RigidbodyInterpolation.Interpolate;
                    gates.Add(body);
                }
                else if(tile=='H')
                {
                    var pad=Slab("PHYSICAL_REPULSION_HAZARD_"+x+"_"+z,
                        Center(p,.055f),new Vector3(CellSize,.11f,CellSize),
                        Color.yellow);
                    hazardPads.Add(pad.GetComponent<BoxCollider>());
                    hazardPaint.Add(pad.GetComponent<Renderer>());
                }
            }
            observedCrateCell=new GridCell(3,8);
            var box=Slab("R3 GENUINE PHYSICAL MOVABLE CRATE",Center(observedCrateCell,.8f),
                new Vector3(1.25f,1.6f,1.25f),Color.magenta);
            crateCollider=box.GetComponent<BoxCollider>();
            physicalCrate=box.AddComponent<Rigidbody>();
            physicalCrate.isKinematic=true;physicalCrate.useGravity=false;
            observedAuxiliaryCell=new GridCell(11,8);
            var second=Slab("R4 SECOND REAL KINEMATIC BLOCKING CRATE",
                Center(observedAuxiliaryCell,.8f),
                new Vector3(1.25f,1.6f,1.25f),Color.blue);
            auxiliaryCollider=second.GetComponent<BoxCollider>();
            auxiliaryCrate=second.AddComponent<Rigidbody>();
            auxiliaryCrate.isKinematic=true;auxiliaryCrate.useGravity=false;
            var unit=GameObject.CreatePrimitive(PrimitiveType.Capsule);
            unit.name="Dynamic physical Light / Heavy test rig";
            unit.transform.SetParent(transform,false);
            player=unit.AddComponent<Rigidbody>();
            player.useGravity=true;
            player.interpolation=RigidbodyInterpolation.Interpolate;
            player.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            player.constraints=RigidbodyConstraints.FreezeRotationX|
                RigidbodyConstraints.FreezeRotationZ;
            SetHeavy(false);
        }
        public void SetHeavy(bool value)
        {
            heavy=value;
            if(player==null)return;
            player.mass=value?12f:1.5f;
            player.transform.localScale=value?new Vector3(1.8f,1f,1.8f):Vector3.one;
            Restart();
        }
        public void SetDrive(float horizontal,float vertical)
        {
            drive=Vector2.ClampMagnitude(new Vector2(
                Mathf.Clamp(horizontal,-1,1),Mathf.Clamp(vertical,-1,1)),1f);
        }
        public void TeleportTo(GridCell p)
        {
            if(!level.Contains(p.X,p.Z))throw new System.ArgumentOutOfRangeException();
            player.linearVelocity=Vector3.zero;player.angularVelocity=Vector3.zero;
            var where=Center(p,1.2f);
            player.position=where;player.transform.position=where;
            Physics.SyncTransforms();
        }
        public void SetPaused(bool value)
        {
            paused=value;
            if(player==null)return;
            if(value){player.linearVelocity=Vector3.zero;player.angularVelocity=Vector3.zero;}
            player.isKinematic=value;
            if(!value)player.WakeUp();
        }
        public void Restart()
        {
            if(player==null||level==null)return;
            paused=false;drive=Vector2.zero;respawns=0;hasRouteOrigin=false;
            rejectedCrateMoves=0;
            observedCrateCell=new GridCell(3,8);
            observedAuxiliaryCell=new GridCell(11,8);
            if(physicalCrate!=null)
            {
                var stage=Center(observedCrateCell,.8f);
                physicalCrate.position=stage;physicalCrate.transform.position=stage;
                Physics.SyncTransforms();
            }
            if(auxiliaryCrate!=null)
            {
                var secondary=Center(observedAuxiliaryCell,.8f);
                auxiliaryCrate.position=secondary;auxiliaryCrate.transform.position=secondary;
                Physics.SyncTransforms();
            }
            gateDesiredOpen=false;physicalGateClear=false;hazardActive=true;
            player.isKinematic=false;
            foreach(var gate in gates)
            {
                gate.position=new Vector3(gate.position.x,1.4f,gate.position.z);
                gate.transform.position=gate.position;
            }
            TeleportTo(level.Start);
            PaintHazards();
            Replan();
        }
        // R4: original finite physical broadphase snapshot for TWO separate
        // kinematic Rigidbody+BoxCollider props. Unknown/overflowed observations
        // must NEVER make an obsolete "safe" route appear.
        private GridCell[] SamplePhysicalBlockers(Collider excluded=null)
        {
            occupancyUncertain=false;
            if(crateCollider==null||auxiliaryCollider==null||
               !crateCollider.enabled||!auxiliaryCollider.enabled)
            {
                occupancyUncertain=true;
                return System.Array.Empty<GridCell>();
            }
            var buffer=new Collider[256];
            var cells=new List<GridCell>();
            bool primaryDetected=excluded==crateCollider;
            bool auxiliaryDetected=excluded==auxiliaryCollider;
            Physics.SyncTransforms();
            for(int z=0;z<level.Height;z++)
            for(int x=0;x<level.Width;x++)
            {
                var cell=new GridCell(x,z);
                int hits=Physics.OverlapBoxNonAlloc(Center(cell,.8f),
                    new Vector3(.8f,.72f,.8f),buffer,Quaternion.identity,
                    ~0,QueryTriggerInteraction.Ignore);
                if(hits>=buffer.Length)
                {
                    occupancyUncertain=true;
                    return System.Array.Empty<GridCell>();
                }
                bool occupied=false;
                for(int i=0;i<hits;i++)
                {
                    if(buffer[i]==crateCollider && excluded!=crateCollider)
                    {primaryDetected=true;occupied=true;}
                    if(buffer[i]==auxiliaryCollider && excluded!=auxiliaryCollider)
                    {auxiliaryDetected=true;occupied=true;}
                }
                if(occupied)cells.Add(cell);
            }
            if(!primaryDetected||!auxiliaryDetected)
            {
                occupancyUncertain=true;
                return System.Array.Empty<GridCell>();
            }
            return cells.ToArray();
        }
        public bool TryMoveCrate(GridCell proposed)=>
            TryMovePhysicalCrate(physicalCrate,crateCollider,auxiliaryCollider,proposed);
        public bool TryMoveAuxiliaryCrate(GridCell proposed)=>
            TryMovePhysicalCrate(auxiliaryCrate,auxiliaryCollider,crateCollider,proposed);
        // Check the COMBINED real currently observed occupancy of the other
        // crate with the proposed mover footprint; a safe solo move is NOT
        // necessarily safe when two physical objects coexist.
        private bool TryMovePhysicalCrate(Rigidbody mover,Collider movingCollider,
            Collider otherCollider,GridCell proposed)
        {
            if(level==null||mover==null||movingCollider==null||otherCollider==null)
                return false;
            if(!level.Contains(proposed.X,proposed.Z)||
                level.At(proposed.X,proposed.Z)!='.')
            {rejectedCrateMoves++;return false;}
            var otherCells=SamplePhysicalBlockers(movingCollider);
            if(occupancyUncertain)
            {rejectedCrateMoves++;return false;}
            var hypothetical=new List<GridCell>(otherCells){proposed};
            if(!PhysicalObstacleRouter.AuditAllEight(level,hypothetical).AllReachable)
            {rejectedCrateMoves++;return false;}
            var target=new Bounds(Center(proposed,.8f),new Vector3(1.25f,1.6f,1.25f));
            var hull=player.GetComponent<Collider>();
            if(hull!=null&&hull.bounds.Intersects(target))
            {rejectedCrateMoves++;return false;}
            if(otherCollider.bounds.Intersects(target))
            {rejectedCrateMoves++;return false;}
            foreach(var wall in obstacles)
                if(wall.bounds.Intersects(target))
                {rejectedCrateMoves++;return false;}
            foreach(var gate in gates)
                if(gate.GetComponent<Collider>().bounds.Intersects(target))
                {rejectedCrateMoves++;return false;}
            mover.position=target.center;
            mover.transform.position=target.center;
            Physics.SyncTransforms();
            Replan();
            return true;
        }
        public void ToggleGate()
        {
            gateDesiredOpen=!gateDesiredOpen;
            if(!gateDesiredOpen){physicalGateClear=false;Replan();}
        }
        public void ToggleHazard(){hazardActive=!hazardActive;PaintHazards();Replan();}
        private void PaintHazards()
        {
            foreach(var renderer in hazardPaint)
                renderer.material.color=hazardActive?Color.red:Color.green;
        }
        private void FixedUpdate()
        {
            if(player==null||paused)return;
            float dt=Time.fixedDeltaTime;
            if(dt<=0||dt>.05f)return;
            foreach(var gate in gates)
            {
                float target=gateDesiredOpen?5.3f:1.4f;
                float next=Mathf.MoveTowards(gate.position.y,target,dt*4f);
                gate.MovePosition(new Vector3(gate.position.x,next,gate.position.z));
            }
            bool visible=gateDesiredOpen&&gates.Count>0;
            foreach(var gate in gates)
                visible=visible&&gate.position.y>4.2f;
            if(visible!=physicalGateClear)
            {physicalGateClear=visible;Replan();}
            if(drive.sqrMagnitude>.0001f)
                player.AddForce(new Vector3(drive.x,0,drive.y)*48f,ForceMode.Force);
            if(hazardActive)
            {
                var hull=player.GetComponent<Collider>();
                if(hull!=null)
                {
                    foreach(var pad in hazardPads)
                    {
                        Vector3 p=player.position;
                        if(pad.Raycast(new Ray(p+Vector3.up*.1f,Vector3.down),out _,
                           hull.bounds.extents.y+.35f))
                        {
                            player.AddForce(-Vector3.forward*38f,ForceMode.Force);
                            break;
                        }
                    }
                }
            }
            if(player.position.y< -6)
            {
                respawns++;
                player.linearVelocity=Vector3.zero;
                TeleportTo(level.Start);
            }
            // Fix W23 R1 bug: route was always from design SPAWN even when
            // Light/Heavy had traversed the level. Update only on cell change.
            // Detect BOTH foreign-script/physics Rigidbody pose changes,
            // including within the same grid cell before/after a tile crossing.
            if(!hasRouteOrigin||!ActualPlayerCell().Equals(routeOrigin)||
               (physicalCrate.position-observedCratePose).sqrMagnitude>.0004f||
               (auxiliaryCrate.position-observedAuxiliaryPose).sqrMagnitude>.0004f)
                Replan();
        }
        public void Replan()
        {
            if(level==null)return;
            int radius=heavy?1:0;
            // Replan from the real physical player's position, including after
            // teleport or entering a previously unseen map cell.
            routeOrigin=ActualPlayerCell();
            hasRouteOrigin=true;
            observedCratePose=physicalCrate.position;
            observedAuxiliaryPose=auxiliaryCrate.position;
            observedCrateCell=new GridCell(
                Mathf.RoundToInt(observedCratePose.x/CellSize)+level.Width/2,
                Mathf.RoundToInt(observedCratePose.z/CellSize)+level.Height/2);
            observedAuxiliaryCell=new GridCell(
                Mathf.RoundToInt(observedAuxiliaryPose.x/CellSize)+level.Width/2,
                Mathf.RoundToInt(observedAuxiliaryPose.z/CellSize)+level.Height/2);
            occupiedCells=SamplePhysicalBlockers();
            current=occupancyUncertain
                ?new SafeRoutePlan(false,System.Array.Empty<GridCell>(),0)
                :PhysicalObstacleRouter.FindFrom(level,routeOrigin,radius,
                    new RouteConditions(physicalGateClear,hazardActive),occupiedCells);
            replanRevision++;
            foreach(var old in routeMarkers)
                if(old!=null)Destroy(old);
            routeMarkers.Clear();
            if(!current.Reachable)return;
            // Visible waypoint markers (colliders immediately disabled).
            foreach(var p in current.Cells)
            {
                var marker=Slab("SAFE_ROUTE_WAYPOINT_"+p.X+"_"+p.Z,
                    Center(p,.14f),new Vector3(.32f,.08f,.32f),Color.cyan);
                marker.GetComponent<Collider>().enabled=false;
                routeMarkers.Add(marker);
            }
        }
        public bool PhysicallyClearCurrentPlan()
        {
            if(current==null||!current.Reachable)return false;
            Physics.SyncTransforms();
            float margin=heavy?2.8f:.8f;
            foreach(var p in current.Cells)
            {
                var hit=Physics.OverlapBox(Center(p,1.25f),
                    new Vector3(margin,.65f,margin),Quaternion.identity,
                    ~0,QueryTriggerInteraction.Ignore);
                foreach(var collider in hit)
                {
                    if(obstacles.Contains(collider)||collider==crateCollider||
                       collider==auxiliaryCollider)return false;
                    // Authoritative physical obstruction check: an actually
                    // overlapping gate collider blocks even if a script state
                    // previously marked all gates raised.
                    if(gates.Exists(g=>g.GetComponent<Collider>()==collider))
                        return false;
                }
            }
            return true;
        }
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(12,12,430,480),GUI.skin.box);
            GUILayout.Label("B01-25 SAFEROUTE001 — COLLIDER-AWARE ROUTE GUIDE");
            GUILayout.Label("Light / Heavy: "+(heavy?"HEAVY, 3x3 clearance":"LIGHT, 1x1 clearance"));
            GUILayout.Label("Gate physical clear "+physicalGateClear+
                " | hazard active "+hazardActive);
            GUILayout.Label("SAFE ALL EIGHT STATES: "+audit.AllReachable);
            GUILayout.Label("Live origin cell: "+routeOrigin+" | guided: "+CurrentRouteFromPlayer);
            GUILayout.Label("Physical crates: "+observedCrateCell+" & "+
                observedAuxiliaryCell+" | occupied: "+PhysicalBlockedCells);
            GUILayout.Label("Occupancy uncertain: "+occupancyUncertain+
                " | route revision: "+replanRevision);
            if(GUILayout.Button("Try pair SOFTLOCK: move crate B to (5,5)"))
                TryMoveAuxiliaryCrate(new GridCell(5,5));
            if(GUILayout.Button("Move crate B safely to (4,4)"))
                TryMoveAuxiliaryCrate(new GridCell(4,4));
            if(GUILayout.Button("Reset crate B to safe staging (11,8)"))
                TryMoveAuxiliaryCrate(new GridCell(11,8));
            GUILayout.Label("Unsafe crate moves rejected: "+RejectedCrateMoves);
            if(GUILayout.Button("Move crate to SAFE cell (4,4)"))
                TryMoveCrate(new GridCell(4,4));
            if(GUILayout.Button("Reset crate to SAFE staging (3,8)"))
                TryMoveCrate(new GridCell(3,8));
            if(GUILayout.Button("TRY UNSAFE crate chokepoint (7,8)"))
                TryMoveCrate(new GridCell(7,8));
            GUILayout.Label("Witness nodes: "+(current==null?0:current.Cells.Length)+
                " | wall colliders: "+WallCount+" | respawns: "+respawns);
            if(GUILayout.Button("Switch Light / Heavy"))SetHeavy(!heavy);
            if(GUILayout.Button(gateDesiredOpen?"Close gate":"Open gate"))ToggleGate();
            if(GUILayout.Button(hazardActive?"Disable hazard":"Enable hazard"))ToggleHazard();
            if(GUILayout.Button(paused?"Resume":"Pause"))SetPaused(!paused);
            if(GUILayout.Button("Restart"))Restart();
            float x=0,z=0;
            if(GUILayout.RepeatButton("Drive forward (+Z)"))z=1;
            if(GUILayout.RepeatButton("Drive backward (-Z)"))z=-1;
            if(GUILayout.RepeatButton("Drive right (+X)"))x=1;
            if(GUILayout.RepeatButton("Drive left (-X)"))x=-1;
            SetDrive(x,z);
            GUILayout.EndArea();
        }
    }
}
