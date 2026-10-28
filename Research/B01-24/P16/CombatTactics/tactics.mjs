// B01-24 P16: isolated deterministic combat decision prototype (NOT integrated Unity AI).
// World metres: x/z. No damage or Rigidbody mutation. World-cover suppression awaits HQ.
const v=(x,z)=>({x,z}), sub=(a,b)=>v(a.x-b.x,a.z-b.z);
const dot=(a,b)=>a.x*b.x+a.z*b.z, len=a=>Math.hypot(a.x,a.z);
const norm=a=>len(a)>1e-8?v(a.x/len(a),a.z/len(a)):v(0,0);
const turn=(a,d)=>{const t=d*Math.PI/180;return v(a.x*Math.cos(t)-a.z*Math.sin(t),a.x*Math.sin(t)+a.z*Math.cos(t))};
const segmentDisc=(a,b,p,r)=>{const d=sub(b,a),q=dot(d,d),t=q>1e-9?Math.max(0,Math.min(1,dot(sub(p,a),d)/q)):0;return len(sub(p,v(a.x+t*d.x,a.z+t*d.z)))<r-1e-8};
const segmentBox=(a,b,o,r=0)=>{let near=0,far=1;for(const axis of ['x','z']){const lo=o['min'+axis.toUpperCase()]-r,hi=o['max'+axis.toUpperCase()]+r,d=b[axis]-a[axis];if(Math.abs(d)<1e-9){if(a[axis]<lo||a[axis]>hi)return false;continue}const u=(lo-a[axis])/d,w=(hi-a[axis])/d;near=Math.max(near,Math.min(u,w));far=Math.min(far,Math.max(u,w));if(near>far)return false}return near<=1&&far>=0};
export function decide(input){
 const s=input.self,t=input.target,c={preferredDistance:12,fireRange:60,maxStep:1,clearance:.35,...input.config};
 for(const p of [s.position,t.position])if(!p||!Number.isFinite(p.x)||!Number.isFinite(p.z))throw Error('position');
 if(!Number.isFinite(s.drive)||!Number.isInteger(s.weapons)||s.weapons<0||!Number.isFinite(s.radius)||s.radius<=0)throw Error('equipment');
 for(const n of Object.values(c))if(!Number.isFinite(n)||n<0)throw Error('config');
 const allies=input.allies||[],walls=input.obstacles||[],distance=len(sub(t.position,s.position)),armed=s.weapons>0,mobile=s.drive>0;
 const blockedByAlly=allies.some(a=>segmentDisc(s.position,t.position,a.position,a.radius));
 const worldCover=walls.some(o=>segmentBox(s.position,t.position,o));
 const fireAllowed=armed&&distance>0&&distance<=c.fireRange&&!blockedByAlly;
 const mode=!mobile?(armed?'STATIONARY':'DISABLED'):!armed?'RAM_APPROACH':distance>c.preferredDistance?'ARMED_APPROACH':'ARMED_HOLD';
 const toward=norm(sub(t.position,s.position)),away=v(-toward.x,-toward.z);
 const desired=mode==='RAM_APPROACH'||mode==='ARMED_APPROACH'?toward:mode==='ARMED_HOLD'&&distance<c.preferredDistance*.7?away:v(0,0);
 let movement=v(0,0),choice=-1,blocked='NONE',step=0;
 if(mobile&&len(desired)>0){const travel=c.maxStep*Math.min(1,s.drive);
  const angles=[0,30,-30,60,-60,90,-90,120,-120,150,-150,180];
  for(let i=0;i<angles.length;i++){const dir=turn(desired,angles[i]),end=v(s.position.x+dir.x*travel,s.position.z+dir.z*travel);
   const wall=walls.some(o=>segmentBox(s.position,end,o,s.radius+c.clearance));
   const ally=allies.some(a=>segmentDisc(s.position,end,a.position,s.radius+a.radius+c.clearance));
   if(!wall&&!ally){movement=dir;choice=i;step=travel;break}
   if(i===0)blocked=wall?'OBSTACLE':'ALLY';
  }
 }
 return {mode,fireAllowed,fireReason:!armed?'NO_WEAPON':blockedByAlly?'ALLY_IN_LINE':distance>c.fireRange?'OUT_OF_RANGE':!distance?'NO_TARGET_VECTOR':'READY_POLICY_UNRESOLVED_WORLD_COVER',worldCover,blockedByAlly,movement,step,choice,blocked};
}
export const geometry={segmentDisc,segmentBox};
