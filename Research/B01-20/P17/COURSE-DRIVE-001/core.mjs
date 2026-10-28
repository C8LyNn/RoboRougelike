// COURSE-DRIVE-001. Deterministic, axis-aligned arcade vehicle preview over
// ORIGINAL MapGenPlanner ObstacleCourse MapBlueprint geometry. NOT Rigidbody/PhysX.
export const PROFILES = Object.freeze({
  Light: Object.freeze({width:2.1,length:3.2,maxSpeed:6.5}),
  Heavy: Object.freeze({width:4.5,length:5.8,maxSpeed:4.8})
});
const margin=.25;
export const clamp=(x,a,b)=>Math.max(a,Math.min(b,x));
export function distance(a,b){return Math.hypot(a.x-b.x,a.z-b.z);}
export function validateMap(m){
  if(!m||m.packageId!=="MAPGEN-COURSE-P17"||m.recipe!=="ObstacleCourse"||
    m.sourceSha!=="5be31039411f89f17654b1c332f8692883fa03cc"||
    !/^P17-COURSE-[A-Z-]+$/.test(m.variantId)||
    !Number.isInteger(m.seed)||!Array.isArray(m.checkpoints)||
    m.checkpoints.length!==m.config?.segments||
    m.checkpoints.length<3||m.checkpoints.length>5||
    !/^[a-f0-9]{64}$/.test(m.geometryHash||"")||
    m.nativeUnity!=="NOT_RUN"||!Array.isArray(m.elements)||
    m.elements.length<6) throw Error("INVALID_MAPGEN_BLUEPRINT");
  const points=[m.start,...m.checkpoints,m.finish];
  for(const p of points)if(!p||!Number.isFinite(p.x)||!Number.isFinite(p.z))
    throw Error("INVALID_ROUTE_COORDINATES");
  for(const e of m.elements) {
    if(!["Floor","Boundary","Obstacle","Ramp","Cover"].includes(e.kind) ||
      !e.position || !e.size ||
      !["x","y","z"].every(k=>Number.isFinite(e.position[k])&&Number.isFinite(e.size[k])&&e.size[k]>0))
      throw Error("INVALID_MAP_ELEMENT");
  }
  if(!m.elements.some(e=>e.kind==="Floor"))throw Error("NO_FLOOR");
  return m;
}
export function free(m,profile,x,z){
  const halfW=profile.width*.5+margin;
  const halfL=profile.length*.5+margin;
  // Exactly the original source's support 3x3 footprint preflight projection:
  // original MAPGEN only has primitive flat floor. No PhysX claim.
  for(let ix=-1;ix<=1;ix++)for(let iz=-1;iz<=1;iz++){
    const px=x+ix*halfW,pz=z+iz*halfL;
    if(!m.elements.some(e=>e.kind==="Floor" &&
      Math.abs(px-e.position.x)<=e.size.x*.5 &&
      Math.abs(pz-e.position.z)<=e.size.z*.5))return false;
  }
  for(const e of m.elements){
    if(e.kind==="Floor")continue;
    let halfZ=e.size.z*.5;
    if(e.kind==="Ramp"){
      const angle=(e.rotationEuler?.x||0)*Math.PI/180;
      halfZ=Math.abs(Math.cos(angle))*e.size.z*.5+
            Math.abs(Math.sin(angle))*e.size.y*.5;
    }
    if(Math.abs(e.position.x-x)<e.size.x*.5+halfW &&
      Math.abs(e.position.z-z)<halfZ+halfL)return false;
  }
  return true;
}
export function createState(m,profileName){
  validateMap(m);
  const profile=PROFILES[profileName];
  if(!profile)throw Error("UNKNOWN_PROFILE");
  if(!free(m,profile,m.start.x,m.start.z))throw Error("START_BLOCKED");
  return {x:m.start.x,z:m.start.z,vx:0,vz:0,elapsed:0,cp:0,collisions:0,
    done:false,paused:false,profileName,geometryHash:m.geometryHash};
}
export function tick(m,state,ctrl,dt){
  if(!Number.isFinite(dt)||dt<0||dt>.11)throw Error("BAD_FIXED_DT");
  if(state.geometryHash!==m.geometryHash)throw Error("MAP_STATE_MISMATCH");
  if(state.paused||state.done||dt===0)return state;
  const vehicle=PROFILES[state.profileName];
  if(!vehicle)throw Error("UNKNOWN_PROFILE");
  const fx=clamp(Number.isFinite(ctrl?.x)?ctrl.x:0,-1,1);
  const fz=clamp(Number.isFinite(ctrl?.z)?ctrl.z:0,-1,1);
  const scale=Math.max(1,Math.hypot(fx,fz));
  const speed=vehicle.maxSpeed;
  const alpha=Math.min(1,dt*8);
  let vx=state.vx+(fx/scale*speed-state.vx)*alpha;
  let vz=state.vz+(fz/scale*speed-state.vz)*alpha;
  const px=state.x+vx*dt,pz=state.z+vz*dt;
  let x=state.x,z=state.z,blocked=false;
  const steps=Math.max(1,Math.ceil(Math.hypot(px-x,pz-z)/.20));
  for(let j=1;j<=steps;j++){
    const nx=state.x+(px-state.x)*j/steps;
    const nz=state.z+(pz-state.z)*j/steps;
    if(!free(m,vehicle,nx,nz)){blocked=true;vx=0;vz=0;break;}
    x=nx;z=nz;
  }
  let cp=state.cp;
  const route=[...m.checkpoints,m.finish];
  // Final original course checkpoint can equal Finish: consume both
  // in order, but never allow skipping an earlier checkpoint.
  while(cp<route.length&&distance({x,z},route[cp])<2.6)cp++;
  return {...state,x,z,vx,vz,elapsed:state.elapsed+dt,cp,
    collisions:state.collisions+(blocked?1:0),done:cp===route.length};
}
export function assist(m,state){
  const p=[...m.checkpoints,m.finish][state.cp];
  if(!p)return {x:0,z:0};
  const dx=p.x-state.x,dz=p.z-state.z;
  // Keep original axis-aligned vehicle footprint and follow source spine.
  const l=Math.hypot(dx,dz);
  return l<.001?{x:0,z:0}:{x:dx/l,z:dz/l};
}
export function runAssist(m,name,maxSteps=6000){
  let state=createState(m,name);
  for(let i=0;i<maxSteps&&!state.done;i++)state=tick(m,state,assist(m,state),.05);
  return state;
}
