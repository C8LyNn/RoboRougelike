/* B01-12 TOOLTIP-ANCHOR-001/R1 -- deterministic, dependency-free contextual tooltip geometry & state. */
(function (root, factory) {
  const api = factory();
  if (typeof module === 'object' && module.exports) module.exports = api;
  if (root) root.B01TooltipAnchor = api;
})(typeof globalThis !== 'undefined' ? globalThis : null, function () {
  'use strict';
  const IDS = Object.freeze(['hp','mass','energy','heat','speed','repair','armor','weapon']);
  const SIDES = Object.freeze(['top','bottom','right','left']);
  const VERSION = 1;
  function record(o, keys, required) {
    if (!o || typeof o !== 'object' || Array.isArray(o) || Object.getPrototypeOf(o) !== Object.prototype) return false;
    if (Object.keys(o).some(k => !keys.includes(k))) return false;
    return required.every(k => Object.hasOwn(o, k));
  }
  function n(x, min, max) { return typeof x === 'number' && Number.isFinite(x) && x >= min && x <= max; }
  function rect(o, allowZero = false) {
    return record(o,['x','y','width','height'],['x','y','width','height']) &&
      n(o.x,0,10000) && n(o.y,0,10000) && n(o.width,allowZero?0:1,10000) && n(o.height,allowZero?0:1,10000);
  }
  function size(o) {
    return record(o,['width','height'],['width','height']) && n(o.width,1,10000) && n(o.height,1,10000);
  }
  function insets(o) {
    return record(o,['top','bottom','left','right'],['top','bottom','left','right']) &&
      Object.values(o).every(v=>n(v,0,5000));
  }
  function overlaps(a,b) { return a.x < b.x+b.width && b.x < a.x+a.width && a.y < b.y+b.height && b.y < a.y+a.height; }
  function fail(code) { return Object.freeze({ok:false,code}); }
  function solve(options) {
    if (!record(options,['viewport','anchor','tooltip','gap','preferred','insets','exclusions'],['viewport','anchor','tooltip'])) return fail('INVALID_INPUT');
    const {viewport, anchor, tooltip} = options;
    const gap = options.gap === undefined ? 12 : options.gap;
    const preferred = options.preferred === undefined ? 'top' : options.preferred;
    const pad = options.insets === undefined ? {top:8,right:8,bottom:8,left:8} : options.insets;
    const exclusions = options.exclusions === undefined ? [] : options.exclusions;
    if (!size(viewport) || !rect(anchor) || !size(tooltip) || !n(gap,0,128) ||
        !SIDES.includes(preferred) || !insets(pad) || !Array.isArray(exclusions) || exclusions.length > 8 ||
        exclusions.some(o => !rect(o))) return fail('INVALID_INPUT');
    const bounds = {x:pad.left,y:pad.top,width:viewport.width-pad.left-pad.right,height:viewport.height-pad.top-pad.bottom};
    if (bounds.width <= 0 || bounds.height <= 0 || tooltip.width > bounds.width || tooltip.height > bounds.height ||
        anchor.x+anchor.width > viewport.width || anchor.y+anchor.height > viewport.height ||
        exclusions.some(o=>o.x+o.width>viewport.width || o.y+o.height>viewport.height)) return fail('OUT_OF_BOUNDS');
    const clamp = (v,lo,hi) => Math.max(lo,Math.min(hi,v));
    const cx = anchor.x+anchor.width/2, cy=anchor.y+anchor.height/2;
    const xlim=bounds.x+bounds.width-tooltip.width, ylim=bounds.y+bounds.height-tooltip.height;
    const order=[preferred,...SIDES.filter(s=>s!==preferred)];
    const candidates=[];
    for(let index=0;index<order.length;index++) {
      const side=order[index];
      const xs = side==='top'||side==='bottom' ? cx-tooltip.width/2 : side==='left' ? anchor.x-gap-tooltip.width : anchor.x+anchor.width+gap;
      const ys = side==='left'||side==='right' ? cy-tooltip.height/2 : side==='top' ? anchor.y-gap-tooltip.height : anchor.y+anchor.height+gap;
      // Only slide along the axis perpendicular to the selected side; never clamp across the anchor.
      const x = side==='top'||side==='bottom' ? clamp(xs,bounds.x,xlim) : xs;
      const y = side==='left'||side==='right' ? clamp(ys,bounds.y,ylim) : ys;
      const candidate={x,y,width:tooltip.width,height:tooltip.height};
      if (x < bounds.x || y < bounds.y || x+candidate.width > bounds.x+bounds.width || y+candidate.height > bounds.y+bounds.height ||
          overlaps(candidate,anchor) || exclusions.some(e=>overlaps(candidate,e))) continue;
      const displacement=(side==='top'||side==='bottom'?Math.abs(x-xs):Math.abs(y-ys));
      candidates.push({rect:candidate,side,score:index*100000+displacement});
    }
    if (!candidates.length) return fail('NO_FIT');
    candidates.sort((a,b)=>a.score-b.score || SIDES.indexOf(a.side)-SIDES.indexOf(b.side));
    const winner=candidates[0];
    return {ok:true,rect:Object.freeze(winner.rect),side:winner.side,score:winner.score};
  }
  const EMPTY = Object.freeze({version:VERSION,id:null,open:false,pinned:false,revision:0});
  function stateValid(s){return record(s,['version','id','open','pinned','revision'],['version','id','open','pinned','revision']) &&
    s.version===VERSION && (s.id===null || IDS.includes(s.id)) && typeof s.open==='boolean' && typeof s.pinned==='boolean' &&
    Number.isSafeInteger(s.revision) && s.revision>=0 && s.revision<=100000 &&
    (!s.open ? s.id===null && !s.pinned : s.id!==null);}
  function reduce(state,event){
    if(!stateValid(state)) return fail('INVALID_STATE');
    if(!record(event,['type','id'],['type']) || typeof event.type!=='string') return fail('INVALID_EVENT');
    let out;
    if(event.type==='SHOW'||event.type==='PIN') {
      if(!record(event,['type','id'],['type','id']) || !IDS.includes(event.id))return fail('INVALID_EVENT');
      if(event.type==='SHOW') {
        if(state.pinned)return {ok:true,state};
        out={...state,id:event.id,open:true,pinned:false};
      }else {
        out=state.pinned&&state.id===event.id ? {...state,id:null,open:false,pinned:false} : {...state,id:event.id,open:true,pinned:true};
      }
    }else if(event.type==='HIDE'||event.type==='ESCAPE'||event.type==='RESET'){
      if(!record(event,['type'],['type']))return fail('INVALID_EVENT');
      if(event.type==='HIDE'&&state.pinned)return {ok:true,state};
      out={...state,id:null,open:false,pinned:false};
    }else return fail('INVALID_EVENT');
    if(out.id===state.id&&out.open===state.open&&out.pinned===state.pinned)return {ok:true,state};
    if(state.revision===100000)return fail('REVISION_LIMIT');
    out.revision=state.revision+1;
    return {ok:true,state:Object.freeze(out)};
  }
  return Object.freeze({VERSION,IDS,SIDES,EMPTY,solve,reduce,stateValid,overlaps});
});
