/* B01-11 CAMERA-VISIBILITY-LAB-001: deterministic synthetic top-down visibility, NOT Unity physics. */
(function(root,factory){
  const api=factory();
  if(typeof module==="object"&&module.exports)module.exports=api;
  else root.CameraVisibilityLab=api;
})(typeof globalThis==="object"?globalThis:this,function(){
  "use strict";
  const EPS=1e-9;
  const finite=n=>typeof n==="number"&&Number.isFinite(n)&&Math.abs(n)<=100;
  const idOk=s=>typeof s==="string"&&/^[A-Za-z0-9_-]{1,40}$/.test(s);
  const validPoint=p=>p&&finite(p.x)&&finite(p.z)&&finite(p.y)&&p.y>=0&&p.y<=20;
  function validate(s){
    if(!s||!validPoint(s.camera)||!validPoint(s.target)||!finite(s.radius)||
       s.radius<=0||s.radius>2||!Array.isArray(s.obstacles)||s.obstacles.length>32)
      return {ok:false,reason:"INVALID_SCENE"};
    if(Math.hypot(s.camera.x-s.target.x,s.camera.z-s.target.z)<0.1)
      return {ok:false,reason:"CAMERA_AT_TARGET"};
    const ids=new Set();
    for(const a of s.obstacles){
      if(!a||!idOk(a.id)||ids.has(a.id)||!finite(a.minX)||!finite(a.maxX)||
         !finite(a.minZ)||!finite(a.maxZ)||!finite(a.height)||
         a.maxX-a.minX<=EPS||a.maxZ-a.minZ<=EPS||
         a.height<=0||a.height>20)
        return {ok:false,reason:"INVALID_OBSTACLE"};
      ids.add(a.id);
    }
    return {ok:true,reason:"OK"};
  }
  function hitRay(start,end,box){
    let enter=0,exit=1;
    for(const [a,min,max] of [["x",box.minX,box.maxX],["z",box.minZ,box.maxZ]]){
      const origin=start[a],step=end[a]-origin;
      if(Math.abs(step)<EPS){
        if(origin<min-EPS||origin>max+EPS)return null;
      }else{
        let t1=(min-origin)/step,t2=(max-origin)/step;
        if(t1>t2)[t1,t2]=[t2,t1];
        enter=Math.max(enter,t1);exit=Math.min(exit,t2);
        if(enter>exit+EPS)return null;
      }
    }
    const ye=start.y+(end.y-start.y)*Math.max(0,enter);
    const yx=start.y+(end.y-start.y)*Math.min(1,exit);
    if(Math.min(ye,yx)>box.height+EPS||Math.max(ye,yx)<-EPS)return null;
    return Math.max(0,enter);
  }
  function samplePoints(target,radius){
    return [
      {x:target.x,z:target.z,y:target.y,label:"center"},
      {x:target.x+radius,z:target.z,y:target.y,label:"right"},
      {x:target.x-radius,z:target.z,y:target.y,label:"left"},
      {x:target.x,z:target.z+radius,y:target.y,label:"front"},
      {x:target.x,z:target.z-radius,y:target.y,label:"back"}
    ];
  }
  function assess(scene){
    const validity=validate(scene);
    if(!validity.ok)return {ok:false,reason:validity.reason,visible:0,total:0,fraction:0,rays:[],blockers:[]};
    const rays=[],blocks=new Set();
    for(const point of samplePoints(scene.target,scene.radius)){
      let closest=null;
      for(const obstacle of scene.obstacles){
        const t=hitRay(scene.camera,point,obstacle);
        if(t!==null&&(!closest||t<closest.t-EPS||
          (Math.abs(t-closest.t)<=EPS&&obstacle.id<closest.id)))
          closest={id:obstacle.id,t};
      }
      if(closest)blocks.add(closest.id);
      rays.push({label:point.label,end:point,blocked:!!closest,by:closest?closest.id:null});
    }
    const visible=rays.filter(x=>!x.blocked).length;
    return {ok:true,reason:visible===5?"CLEAR":visible===0?"FULL_OCCLUSION":"PARTIAL_OCCLUSION",
      visible,total:5,fraction:visible/5,rays,blockers:[...blocks].sort()};
  }
  function suggest(scene){
    const baseline=assess(scene);
    if(!baseline.ok)return {ok:false,reason:baseline.reason,baseline,candidate:null};
    const c=scene.camera;
    const offsets=[
      [0,0,0,"original"],[-2,0,0,"shift-left"],[2,0,0,"shift-right"],
      [-4,0,0,"shift-far-left"],[4,0,0,"shift-far-right"],
      [0,-2,0,"pull-back"],[0,2,0,"push-forward"],
      [0,0,2,"raise-2"],[0,0,4,"raise-4"],
      [-2,0,2,"left-and-raise"],[2,0,2,"right-and-raise"],
      [-4,0,2,"far-left-and-raise"],[4,0,2,"far-right-and-raise"]
    ];
    let best=null;
    for(let i=0;i<offsets.length;i++){
      const [dx,dz,dy,label]=offsets[i],cam={x:c.x+dx,z:c.z+dz,y:c.y+dy};
      const res=assess({...scene,camera:cam});
      if(!res.ok)continue;
      const delta=Math.hypot(dx,dz,dy);
      if(!best||res.visible>best.result.visible||
        (res.visible===best.result.visible&&delta<best.distance-EPS))
        best={camera:cam,label,result:res,distance:delta,index:i};
    }
    if(!best)return {ok:false,reason:"NO_VALID_CANDIDATES",baseline,candidate:null};
    return {ok:true,reason:best.result.visible>baseline.visible?"IMPROVED":
      best.result.visible===5?"ALREADY_CLEAR":"NO_BETTER_CANDIDATE",
      baseline,candidate:best};
  }
  function uiCopy(result){
    if(!result||!result.ok)return {title:"Brak poprawnej sceny",detail:"Wprowadź prawidłową geometrię testową.",level:"danger"};
    const v=result.visible;
    if(v===5)return {title:"Sylwetka czytelna w próbce",detail:"5/5 promieni modelu 2D jest odsłoniętych. To nie jest pomiar Unity/PhysX.",level:"success"};
    if(v===0)return {title:"Robot całkowicie zasłonięty w próbce",detail:"0/5 promieni. Rozważ przesunięcie lub podniesienie kamery.",level:"danger"};
    return {title:"Robot częściowo zasłonięty",detail:String(v)+"/5 promieni odsłoniętych. Konieczna ocena w rzeczywistej scenie.",level:"warning"};
  }
  return Object.freeze({validate,hitRay,samplePoints,assess,suggest,uiCopy});
});
