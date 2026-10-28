(function (root) {
  'use strict';
  const CATEGORIES = Object.freeze(['RETICLE_OCCLUSION', 'TEXT_OVERLAP', 'LOW_CONTRAST', 'HIDDEN_WARNING', 'OTHER']);
  const SEVERITIES = Object.freeze(['P0', 'P1', 'P2']);
  const MAX_ITEMS = 100;
  function own(obj, key) { return Object.prototype.hasOwnProperty.call(obj, key); }
  function finite(v) { return typeof v === 'number' && Number.isFinite(v); }
  function validateAnnotation(a) {
    if (!a || typeof a !== 'object' || Array.isArray(a)) throw Error('annotation object required');
    if (!Number.isSafeInteger(a.id) || a.id < 1 || a.id > 1000000) throw Error('bad id');
    if (!CATEGORIES.includes(a.category) || !SEVERITIES.includes(a.severity)) throw Error('bad category/severity');
    if (typeof a.note !== 'string' || a.note.length > 200 || /[\u0000-\u0008\u000b\u000c\u000e-\u001f]/.test(a.note)) throw Error('bad note');
    if (![a.x,a.y,a.w,a.h].every(finite) || a.x < 0 || a.y < 0 || a.w <= 0 || a.h <= 0 || a.x+a.w > 1.0000001 || a.y+a.h > 1.0000001) throw Error('bad rectangle');
    return {id:a.id,category:a.category,severity:a.severity,note:a.note,x:a.x,y:a.y,w:a.w,h:a.h};
  }
  function validate(s) {
    if (!s || typeof s !== 'object' || Array.isArray(s) || s.schema !== 'frame-ux-001/v1') throw Error('bad schema');
    if (!Number.isSafeInteger(s.width) || !Number.isSafeInteger(s.height) || s.width<1 || s.height<1 || s.width>8192 || s.height>8192) throw Error('bad dimensions');
    if (typeof s.imageName !== 'string' || s.imageName.length > 150 || /[\\/\u0000-\u001f]/.test(s.imageName)) throw Error('bad name');
    if (!Array.isArray(s.annotations) || s.annotations.length > MAX_ITEMS) throw Error('bad annotation count');
    const annotations = s.annotations.map(validateAnnotation);
    const ids = annotations.map(a=>a.id);
    if (new Set(ids).size !== ids.length) throw Error('duplicate id');
    if (!Number.isSafeInteger(s.nextId) || s.nextId<1 || s.nextId>1000001 || (ids.length && s.nextId<=Math.max(...ids))) throw Error('bad nextId');
    return {schema:'frame-ux-001/v1',width:s.width,height:s.height,imageName:s.imageName,nextId:s.nextId,annotations};
  }
  function create(width,height,imageName='synthetic-fixture.png') { return validate({schema:'frame-ux-001/v1',width,height,imageName,nextId:1,annotations:[]}); }
  function add(s,input) {
    const state=validate(s);if(state.annotations.length>=MAX_ITEMS) throw Error('annotation limit');
    const rect=validateAnnotation({id:state.nextId,...input});
    return validate({...state,nextId:state.nextId+1,annotations:[...state.annotations,rect]});
  }
  function remove(s,id) {
    const state=validate(s);
    if (!state.annotations.some(a=>a.id===id)) throw Error('unknown id');
    return validate({...state,annotations:state.annotations.filter(a=>a.id!==id)});
  }
  function exportJSON(s) { return JSON.stringify(validate(s),null,2)+'\n'; }
  function importJSON(str,dimensions) {
    if (typeof str!=='string' || str.length>100000 || !dimensions) throw Error('invalid import');
    const s=validate(JSON.parse(str));
    if (s.width!==dimensions.width || s.height!==dimensions.height) throw Error('screenshot dimensions do not match');
    return s;
  }
  function summary(s) {
    const a=validate(s).annotations;
    return {total:a.length,P0:a.filter(x=>x.severity==='P0').length,P1:a.filter(x=>x.severity==='P1').length,P2:a.filter(x=>x.severity==='P2').length,reticle:a.filter(x=>x.category==='RETICLE_OCCLUSION').length};
  }
  const api=Object.freeze({CATEGORIES,SEVERITIES,MAX_ITEMS,create,add,remove,validate,validateAnnotation,exportJSON,importJSON,summary});
  root.FrameUXCore=api;
  if (typeof module==='object' && module.exports) module.exports=api;
})(typeof window!=='undefined'?window:globalThis);
