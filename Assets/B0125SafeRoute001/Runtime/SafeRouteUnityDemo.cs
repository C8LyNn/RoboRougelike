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
        // Registered foreign world rigidbodies include debris, crates and detached
        // modules from the REAL scene, not only the two authored test crates.
        // The owner must opt in; we never mutate their Rigidbody authority.
        private sealed class ExternalObstacle
        {
            internal Rigidbody Body;
            internal Collider[] Shapes;
            internal Vector3 ObservedPosition;
            internal Quaternion ObservedRotation;
            internal Vector3[] ChildWorldPositions;
            internal Quaternion[] ChildWorldRotations;
            internal Vector3[] ChildWorldScales;
            // Tracks temporary R3Debris collider disable/enable transitions.
            // A dormant collider remains registered but route is NO_PATH.
            internal bool[] ChildCollidable;
        }
        private const int MaxWorldBlockers=32;
        private readonly List<ExternalObstacle> externalObstacles=
            new List<ExternalObstacle>();
        // W30 source improvement: the same bounded PhysX query buffer is
        // reused across replans, including rapid detached-part movement.
        // This MonoBehaviour executes its queries on Unity's main thread.
        private const int PhysicalQueryCapacity=256;
        private readonly Collider[] physicalHits=new Collider[PhysicalQueryCapacity];
        private bool occupancyUncertain;
        private int replanRevision;
        private GridCell[] occupiedCells=System.Array.Empty<GridCell>();
        private int rejectedCrateMoves;
        public Rigidbody PhysicalCrate=>physicalCrate;
        public Rigidbody AuxiliaryCrate=>auxiliaryCrate;
        public GridCell AuxiliaryCrateCell=>observedAuxiliaryCell;
        public bool OccupancyUncertain=>occupancyUncertain;
        public int RegisteredWorldBlockerCount=>externalObstacles.Count;
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
        // R5 scene integration seam: allow independently owned real Rigidbody
        // parts (including post-detachment vehicle components) to become actual
        // navigation blockers. Never write to the object, Save or its owner.
        public bool TryRegisterWorldBlocker(Rigidbody body)
        {
            if(body==null||!body.gameObject.activeInHierarchy||
               body==player||body==physicalCrate||
               body==auxiliaryCrate||externalObstacles.Count>=MaxWorldBlockers)
                return false;
            // Vehicle parts frequently have multiple child colliders. Register
            // their FULL physically attached compound, not only root collider.
            var all=body.GetComponentsInChildren<Collider>(true);
            var shapes=new List<Collider>();
            foreach(var shape in all)
            {
                if(shape==null||shape.isTrigger||shape.attachedRigidbody!=body||
                   !shape.gameObject.activeInHierarchy)
                    continue;
                // Inactive prefab children are not active detached geometry;
                // unlike temporarily disabled colliders on ACTIVE children,
                // they must not permanently strand the route in NO_PATH.
                // R3Debris.Separated runs in Awake while all debris
                // colliders are intentionally disabled for ~0.45 seconds.
                // Keep ownership of these real attached shapes while dormant;
                // sampling stays fail-closed until physics enables them.
                shapes.Add(shape);
            }
            if(shapes.Count==0||shapes.Count>16)return false;
            foreach(var item in externalObstacles)
                if(item.Body==body)return false;
            externalObstacles.Add(new ExternalObstacle{
                Body=body,Shapes=shapes.ToArray(),
                ChildWorldPositions=new Vector3[shapes.Count],
                ChildWorldRotations=new Quaternion[shapes.Count],
                ChildWorldScales=new Vector3[shapes.Count],
                ChildCollidable=new bool[shapes.Count]});
            Replan();
            return true;
        }
        public bool UnregisterWorldBlocker(Rigidbody body)
        {
            if(body==null)return false;
            for(int i=0;i<externalObstacles.Count;i++)
            {
                if(externalObstacles[i].Body!=body)continue;
                externalObstacles.RemoveAt(i);
                Replan();
                return true;
            }
            return false;
        }
        // Unity destroyed objects compare equal to null even though the C#
        // managed wrapper remains. Only those fully destroyed Rigidbody
        // sources can be retired automatically; temporary disabled colliders
        // must remain tracked and fail closed until they become solid again.
        public int PruneDestroyedWorldBlockers()
        {
            int removed=0;
            for(int i=externalObstacles.Count-1;i>=0;i--)
            {
                if(externalObstacles[i].Body!=null)continue;
                externalObstacles.RemoveAt(i);
                removed++;
            }
            if(removed>0)Replan();
            return removed;
        }
        private bool ExternalWorldChanged()
        {
            foreach(var item in externalObstacles)
            {
                if(item.Body==null||item.Shapes==null)return true;
                if((item.Body.position-item.ObservedPosition).sqrMagnitude>.0004f||
                   Quaternion.Angle(item.Body.rotation,item.ObservedRotation)>.2f)
                    return true;
                for(int i=0;i<item.Shapes.Length;i++)
                {
                    var shape=item.Shapes[i];
                    if(shape==null)return true;
                    bool nowCollidable=shape.enabled&&
                        shape.gameObject.activeInHierarchy;
                    if(nowCollidable!=item.ChildCollidable[i])return true;
                    if((shape.transform.position-item.ChildWorldPositions[i])
                       .sqrMagnitude>.0004f||
                       Quaternion.Angle(shape.transform.rotation,
                           item.ChildWorldRotations[i])>.2f||
                       (shape.transform.lossyScale-item.ChildWorldScales[i])
                       .sqrMagnitude>.0004f)return true;
                }
            }
            return false;
        }
        private void RememberExternalWorld()
        {
            foreach(var item in externalObstacles)
            {
                if(item.Body==null||item.Shapes==null)continue;
                item.ObservedPosition=item.Body.position;
                item.ObservedRotation=item.Body.rotation;
                for(int i=0;i<item.Shapes.Length;i++)
                {
                    var shape=item.Shapes[i];
                    if(shape==null)continue;
                    item.ChildWorldPositions[i]=shape.transform.position;
                    item.ChildWorldRotations[i]=shape.transform.rotation;
                    item.ChildWorldScales[i]=shape.transform.lossyScale;
                    item.ChildCollidable[i]=shape.enabled&&
                        shape.gameObject.activeInHierarchy;
                }
            }
        }
        // R5: original finite physical broadphase snapshot for TWO separate
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
            var buffer=physicalHits;
            var cells=new List<GridCell>();
            bool primaryDetected=excluded==crateCollider;
            bool auxiliaryDetected=excluded==auxiliaryCollider;
            var seen=new bool[externalObstacles.Count][];
            for(int k=0;k<externalObstacles.Count;k++)
                seen[k]=new bool[externalObstacles[k].Shapes.Length];
            for(int k=0;k<externalObstacles.Count;k++)
            {
                var external=externalObstacles[k];
                if(external.Body==null||external.Shapes==null)
                {
                    occupancyUncertain=true;
                    return System.Array.Empty<GridCell>();
                }
                foreach(var shape in external.Shapes)
                {
                    if(shape==null||!shape.enabled||
                       !shape.gameObject.activeInHierarchy)
                    {
                        occupancyUncertain=true;
                        return System.Array.Empty<GridCell>();
                    }
                }
            }
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
                    for(int k=0;k<externalObstacles.Count;k++)
                    {
                        var shapes=externalObstacles[k].Shapes;
                        for(int m=0;m<shapes.Length;m++)
                        {
                            if(buffer[i]!=shapes[m])continue;
                            seen[k][m]=true;occupied=true;
                            break;
                        }
                    }
                }
                if(occupied)cells.Add(cell);
            }
            bool allWorldSeen=true;
            for(int k=0;k<seen.Length;k++)
                for(int m=0;m<seen[k].Length;m++)
                    if(!seen[k][m])allWorldSeen=false;
            if(!primaryDetected||!auxiliaryDetected||!allWorldSeen)
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
            foreach(var item in externalObstacles)
                foreach(var shape in item.Shapes)
                    if(shape==null||!shape.enabled||
                       shape.bounds.Intersects(target))
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
            // R3Debris destroys wreckage after 10 seconds. Retire only when
            // Unity actually destroys the Rigidbody; a temporarily disabled
            // collider is still registered and cannot fabricate a safe route.
            if(player==null)return;
            PruneDestroyedWorldBlockers();
            if(paused)return;
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
               (auxiliaryCrate.position-observedAuxiliaryPose).sqrMagnitude>.0004f||
               ExternalWorldChanged())
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
            RememberExternalWorld();
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
                    foreach(var external in externalObstacles)
                        foreach(var shape in external.Shapes)
                            if(shape==collider)return false;
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
            GUILayout.Label("External physical blockers: "+RegisteredWorldBlockerCount);
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
