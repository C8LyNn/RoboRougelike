// B01-25-SLALOM-001/R1: independently executable map + physical-source-space oracle.
// Test geometry is derived from MapWorld.cs primitive dimensions; never a Unity/PhysX claim.
export function validateSlalom(document, manifest) {
  const checks=[], fails=[];
  const check=(id,ok)=>{checks.push({id,passed:!!ok});if(!ok)fails.push(id);};
  const key=(x,z)=>x+","+z;
  const coord=(a)=>Array.isArray(a)&&a.length===2&&a.every(Number.isInteger);
  const items=Array.isArray(document?.items)?document.items:[];
  const W=manifest?.dimensions?.columns||0,H=manifest?.dimensions?.rows||0;
  check("V2_FORMAT_LOCAL_FLOOR",document?.version===2&&document?.globalFloor===false);
  check("SUPPORTED_DIMENSIONS",W===14&&H===18&&manifest.dimensions.cellMeters===4);
  const kinds={};
  for(const it of items)if(it)kinds[it.kind]=(kinds[it.kind]||0)+1;
  check("EXACT_KIND_COUNTS",items.length===360&&kinds[0]===252&&kinds[1]===92&&kinds[6]===4&&kinds[7]===6&&kinds[8]===4&&kinds[4]===1&&kinds[5]===1);
  const validItem=(i)=>i&&Number.isInteger(i.kind)&&i.kind>=0&&i.kind<=9&&Number.isInteger(i.x)&&Number.isInteger(i.z)&&Math.abs(i.x)<=50&&Math.abs(i.z)<=50&&Number.isInteger(i.rotation)&&i.rotation>=0&&i.rotation<=3&&["scaleX","scaleY","scaleZ"].every(k=>Number.isInteger(i[k])&&i[k]>=1&&i[k]<=4);
  check("MAPDRAFT_SOURCE_RANGES",items.every(validItem));
  const ground=items.filter(i=>i&&i.kind===0),objects=items.filter(i=>i&&i.kind!==0);
  const floorCells=new Set(ground.map(i=>key(i.x,i.z)));
  const objectCells=new Set(objects.map(i=>key(i.x,i.z)));
  const obstacleCells=new Set(objects.filter(i=>i.kind!==4&&i.kind!==5).map(i=>key(i.x,i.z)));
  check("COMPLETE_GROUND_UNIQUE",ground.length===W*H&&floorCells.size===W*H);
  check("NON_GROUND_NO_DUPLICATE_FOOTPRINT",objects.length===objectCells.size&&objects.every(i=>i.scaleX===1&&i.scaleZ===1));
  check("START_FINISH_SOURCE_INTACT",items.some(i=>i.kind===4&&i.x===7&&i.z===2)&&items.some(i=>i.kind===5&&i.x===7&&i.z===15)&&!obstacleCells.has("7,2")&&!obstacleCells.has("7,15"));
  const four=[[0,0],[W-1,0],[0,H-1],[W-1,H-1]];
  check("FOUR_SOLID_CORNER_PLATFORMS",four.every(([x,z])=>objects.some(i=>i.kind===6&&i.x===x&&i.z===z&&i.rotation===0&&i.scaleX===1&&i.scaleY===4&&i.scaleZ===1)));
  const perimeter=items.filter(i=>i.kind===1&&(i.x===0||i.x===W-1||i.z===0||i.z===H-1));
  check("ALL_PERIMETER_WALLS",perimeter.length===2*(W-2)+2*(H-2)&&perimeter.every(i=>((i.x===0||i.x===W-1)?i.rotation===1:i.rotation===0)));
  const gates=Array.isArray(manifest.gates)?manifest.gates:[];
  check("FOUR_ALTERNATING_GATES",gates.length===4&&gates.map(g=>g.side).join(",")==="EAST,WEST,EAST,WEST"&&gates.map(g=>g.z).join(",")==="4,7,10,13");
  for(const gate of gates){
    const expected=Array.from({length:gate.wallMaxX-gate.wallMinX+1},(_,i)=>gate.wallMinX+i);
    check("GATE_"+gate.z+"_PHYSICAL_WALL_SPAN",expected.every(x=>items.some(o=>o.kind===1&&o.x===x&&o.z===gate.z&&o.rotation===0)));
    check("GATE_"+gate.z+"_THREE_CELL_OPENING",gate.passMaxX-gate.passMinX===2&&[gate.passMinX,gate.passMinX+1,gate.passMaxX].every(x=>floorCells.has(key(x,gate.z))&&!objectCells.has(key(x,gate.z))));
    check("GATE_"+gate.z+"_ROW_ONLY_EXPECTED_WALLS",items.filter(i=>i.kind===1&&i.z===gate.z&&i.x>0&&i.x<W-1).length===9);
  }
  const box=(i)=>{
    const x=i.x*4,z=i.z*4;
    if(i.kind===1){const hx=i.rotation%2?.25:2,hz=i.rotation%2?2:.25;return {kind:1,x0:x-hx,x1:x+hx,z0:z-hz,z1:z+hz};}
    if(i.kind===6)return {kind:6,x0:x-2,x1:x+2,z0:z-2,z1:z+2};
    if(i.kind===7)return {kind:7,x0:x-.75,x1:x+.75,z0:z-.75,z1:z+.75};
    if(i.kind===8)return {kind:8,x0:x-2,x1:x+2,z0:z-.25,z1:z+.25};
    return null;
  };
  const boxes=objects.map(box).filter(Boolean);
  function crosses(A,B,r,strict=false){
    let lo=0,hi=1;
    for(const [axis,min,max]of [["x","x0","x1"],["z","z0","z1"]]){
      const delta=B[axis]-A[axis];
      if(Math.abs(delta)<1e-8){if(A[axis]<r[min]||A[axis]>r[max])return false;}
      else{const u=(r[min]-A[axis])/delta,v=(r[max]-A[axis])/delta;lo=Math.max(lo,Math.min(u,v));hi=Math.min(hi,Math.max(u,v));if(lo>hi)return false;}
    }
    return !strict||(lo>1e-7&&lo<1-1e-7);
  }
  const xx=(W-1)*4,zz=(H-1)*4;
  const probes=[
    ["WEST",[1,zz/2],[-1,zz/2],1],["EAST",[xx-1,zz/2],[xx+1,zz/2],1],
    ["SOUTH",[xx/2,1],[xx/2,-1],1],["NORTH",[xx/2,zz-1],[xx/2,zz+1],1],
    ["SW",[1,1],[1,-1],6],["SE",[xx-1,1],[xx-1,-1],6],
    ["NW",[1,zz-1],[1,zz+1],6],["NE",[xx-1,zz-1],[xx-1,zz+1],6]
  ];
  for(const [id,a,b,kind]of probes)check("PHYSICAL_SOURCE_RAY_"+id,boxes.some(r=>r.kind===kind&&crosses({x:a[0],z:a[1]},{x:b[0],z:b[1]},r)));
  const r=manifest?.routes?.reference||{},p=Array.isArray(r.waypoints)?r.waypoints:[];
  check("REFERENCE_ROUTE_49_STEPS",p.length===50&&r.expectedSteps===49);
  check("REFERENCE_START_FINISH",p.length>0&&key(...p[0])==="7,2"&&key(...p[p.length-1])==="7,15");
  check("REFERENCE_ROUTE_ADJACENCY",p.every((q,i)=>coord(q)&&(i===0||Math.abs(q[0]-p[i-1][0])+Math.abs(q[1]-p[i-1][1])===1)));
  check("REFERENCE_ROUTE_SOURCE_GRID_CLEAR",p.every(q=>floorCells.has(key(...q))&&!obstacleCells.has(key(...q))));
  for(const gate of gates)check("REFERENCE_CROSSES_GATE_"+gate.z,p.some(q=>key(...q)===key(...gate.referenceCross)));
  // Sample every 0.25m along the exact authored grid polyline. Conservative
  // pillar/low barrier AABB; no claims about actual vehicle rig or PhysX.
  let minClear=Infinity,routeHitCount=0;
  for(let i=1;i<p.length;i++){
    for(let sample=0;sample<=16;sample++){
      const t=sample/16,x=(p[i-1][0]+t*(p[i][0]-p[i-1][0]))*4,z=(p[i-1][1]+t*(p[i][1]-p[i-1][1]))*4;
      for(const b of boxes){const dx=Math.max(b.x0-x,0,x-b.x1),dz=Math.max(b.z0-z,0,z-b.z1),dist=Math.hypot(dx,dz);
        minClear=Math.min(minClear,dist);if(dist<1e-7)routeHitCount++;
      }
    }
  }
  check("REFERENCE_025M_SWEPT_CENTERLINE_NO_OBSTACLE",p.length>=2&&routeHitCount===0);
  check("REFERENCE_STATIC_CLEARANCE_AT_LEAST_2M",minClear>=2-1e-8);
  const q=[[7,2]],dist=new Map([["7,2",0]]);
  for(let j=0;j<q.length;j++){
    const [x,z]=q[j],distance=dist.get(key(x,z));
    for(const [dx,dz]of [[1,0],[-1,0],[0,1],[0,-1]]){
      const X=x+dx,Z=z+dz,K=key(X,Z);
      if(X<1||Z<1||X>=W-1||Z>=H-1||obstacleCells.has(K)||dist.has(K))continue;
      dist.set(K,distance+1);q.push([X,Z]);
    }
  }
  const shortest=dist.get("7,15");
  check("FINISH_REACHABLE_GRID",Number.isInteger(shortest));
  check("SLALOM_DETOUR_LONGER_THAN_STRAIGHT_LINE",Number.isInteger(shortest)&&shortest>13&&shortest<=49);
  return {ok:fails.length===0,checks,failures:fails,metrics:{itemCount:items.length,checkCount:checks.length,gateCount:gates.length,referenceSteps:p.length-1,shortestGridSteps:shortest??null,minStaticSourceAabbClearanceMeters:Number.isFinite(minClear)?Math.round(minClear*1000)/1000:null,sourcePerimeterProbeCount:probes.length}};
}
