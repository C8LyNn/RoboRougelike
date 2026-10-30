/* B01-11 P17W22 FRAME-SAFETY-001: Synthetic 3D perspective; not Unity Camera. */
(function(root,factory){
  const api=factory();
  if(typeof module==='object'&&module.exports)module.exports=api;
  else root.FrameSafetyLab=api;
})(typeof globalThis==='object'?globalThis:this,function(){
  'use strict';
  const MAX=100, NEAR=0.05;
  const finite=n=>typeof n==='number'&&Number.isFinite(n)&&Math.abs(n)<=MAX;
  const vec=p=>p&&finite(p.x)&&finite(p.y)&&finite(p.z);
  const sub=(a,b)=>({x:a.x-b.x,y:a.y-b.y,z:a.z-b.z});
  const dot=(a,b)=>a.x*b.x+a.y*b.y+a.z*b.z;
  const cross=(a,b)=>({x:a.y*b.z-a.z*b.y,y:a.z*b.x-a.x*b.z,z:a.x*b.y-a.y*b.x});
  const mul=(v,n)=>({x:v.x*n,y:v.y*n,z:v.z*n});
  const len=v=>Math.hypot(v.x,v.y,v.z);
  const norm=v=>mul(v,1/len(v));
  const point=(c,s)=>({x:c.x+s.x,y:c.y+s.y,z:c.z+s.z});
  const validId=id=>typeof id==='string'&&/^[A-Za-z0-9_-]{1,40}$/.test(id);
  const kinds=new Set(['core','left-weapon','right-weapon','module']);
  function validate(s){
    if(!s||!vec(s.camera)||!vec(s.aim)||!finite(s.verticalFov)||s.verticalFov<25||s.verticalFov>110||
      !finite(s.aspect)||s.aspect<0.5||s.aspect>3||!s.safe||
      !['left','right','top','bottom'].every(k=>finite(s.safe[k])&&s.safe[k]>=0&&s.safe[k]<=1)||
      s.safe.left>=s.safe.right||s.safe.top>=s.safe.bottom||
      !Array.isArray(s.parts)||s.parts.length<1||s.parts.length>30)
      return {ok:false,reason:'INVALID_SCENE'};
    if(len(sub(s.aim,s.camera))<0.1)return {ok:false,reason:'CAMERA_AT_AIM'};
    const ids=new Set();let cores=0;
    for(const part of s.parts){
      if(!part||!validId(part.id)||ids.has(part.id)||!kinds.has(part.kind)||
        typeof part.active!=='boolean'||!vec(part.center)||!vec(part.size)||
        ![part.size.x,part.size.y,part.size.z].every(n=>n>0&&n<=8))
        return {ok:false,reason:'INVALID_PART'};
      ids.add(part.id);
      if(part.kind==='core'&&part.active)cores++;
    }
    if(cores!==1)return {ok:false,reason:'CORE_MISSING_OR_DUPLICATE'};
    return {ok:true,reason:'OK'};
  }
  function basis(s){
    const forward=norm(sub(s.aim,s.camera));
    const upWorld={x:0,y:1,z:0};
    let right=cross(upWorld,forward);
    if(len(right)<0.001)right={x:1,y:0,z:0};
    right=norm(right);
    const up=norm(cross(forward,right));
    return {forward,right,up};
  }
  function corners(part){
    const out=[];
    for(const x of [-0.5,0.5])for(const y of [-0.5,0.5])for(const z of [-0.5,0.5])
      out.push({x:part.center.x+x*part.size.x,y:part.center.y+y*part.size.y,z:part.center.z+z*part.size.z});
    return out;
  }
  function project(s){
    const status=validate(s);
    if(!status.ok)return {ok:false,reason:status.reason,pieces:[],framed:0,total:0,clipped:0,allFramed:false};
    const {forward,right,up}=basis(s);
    const tan=Math.tan(s.verticalFov*Math.PI/360);
    let framed=0,clippedCount=0;
    const pieces=[];
    for(const part of s.parts){
      if(!part.active)continue;
      let minU=Infinity,minV=Infinity,maxU=-Infinity,maxV=-Infinity,clipped=false;
      for(const corner of corners(part)){
        const offset=sub(corner,s.camera),depth=dot(offset,forward);
        if(depth<=NEAR){clipped=true;continue;}
        const u=(dot(offset,right)/(depth*tan*s.aspect)+1)/2;
        const v=(1-dot(offset,up)/(depth*tan))/2;
        minU=Math.min(minU,u);maxU=Math.max(maxU,u);
        minV=Math.min(minV,v);maxV=Math.max(maxV,v);
      }
      const rect=Number.isFinite(minU)?{minU,maxU,minV,maxV}:null;
      const fit=!clipped&&rect!==null&&rect.minU>=s.safe.left&&
        rect.maxU<=s.safe.right&&rect.minV>=s.safe.top&&rect.maxV<=s.safe.bottom;
      if(fit)framed++;
      if(clipped)clippedCount++;
      pieces.push({id:part.id,kind:part.kind,fit,clipped,rect});
    }
    return {ok:true,reason:framed===pieces.length?'FULLY_FRAMED':clippedCount?'NEAR_CLIPPED':'OUTSIDE_SAFE_FRAME',
      pieces,framed,total:pieces.length,clipped:clippedCount,allFramed:framed===pieces.length};
  }
  function dolly(s,scale){
    if(!Number.isFinite(scale)||scale<1||scale>6)return null;
    const delta=sub(s.camera,s.aim);
    return {...s,camera:point(s.aim,mul(delta,scale))};
  }
  function suggest(s){
    const start=project(s);
    if(!start.ok)return {ok:false,reason:start.reason,baseline:start,candidate:null};
    // Deliberately no side effects: native owner must independently solve camera collision and smoothing.
    for(let i=0;i<=50;i++){
      const scale=1+i/10,candidateScene=dolly(s,scale);
      const result=project(candidateScene);
      if(result.ok&&result.allFramed){
        return {ok:true,reason:i===0?'ALREADY_FIT':'SUGGESTED_DOLLY',baseline:start,
          candidate:{scale,camera:candidateScene.camera,result}};
      }
    }
    return {ok:false,reason:'NO_SAFE_DOLLY',baseline:start,candidate:null};
  }
  function message(r){
    if(!r||!r.ok)return {level:'danger',title:'Błędna konfiguracja',detail:'Sprawdź dane kamery i modułów.'};
    if(r.allFramed)return {level:'success',title:'Moduły w bezpiecznym kadrze',detail:String(r.framed)+'/'+r.total+' aktywnych modułów w próbnej ramce; nie jest to test Unity.'};
    if(r.clipped)return {level:'danger',title:'Moduł przecięty przez near plane',detail:String(r.clipped)+' modułów zbyt blisko kamery.'};
    return {level:'warning',title:'Robot nie mieści się w kadrze',detail:String(r.framed)+'/'+r.total+' aktywnych modułów w próbnej ramce.'};
  }
  return Object.freeze({validate,corners,project,dolly,suggest,message});
});
