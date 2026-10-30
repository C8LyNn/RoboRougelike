/* B01-12-HUDSAFE-001/R1 · deterministic standalone UI safe-zone layout, NOT Unity runtime. */
(function (root, factory) {
  const api = factory();
  if (typeof module === 'object' && module.exports) module.exports = api;
  if (root) root.HUDSafe = api;
})(typeof globalThis !== 'undefined' ? globalThis : this, function () {
  'use strict';
  const ANCHORS = ['tl', 'tr', 'bl', 'br', 'tc', 'bc'];
  const MAX_WIDGETS = 12;
  function fail(reason) { return {ok:false, reason, placements:[], requested:0, occupied:0}; }
  function integer(n, lo, hi) { return Number.isSafeInteger(n) && n>=lo && n<=hi; }
  function rectValid(r, w, h) {
    return !!r && typeof r === 'object' && !Array.isArray(r) && integer(r.x,0,w) && integer(r.y,0,h)
      && integer(r.w,1,w) && integer(r.h,1,h) && r.x+r.w<=w && r.y+r.h<=h;
  }
  function overlap(a,b) { return a.x < b.x+b.w && a.x+a.w > b.x && a.y < b.y+b.h && a.y+a.h > b.y; }
  function within(r, area) { return r.x >= area.x && r.y >= area.y && r.x+r.w <= area.x+area.w && r.y+r.h <= area.y+area.h; }
  function candidate(anchor, area, w, h, gap) {
    const xL=area.x, xR=area.x+area.w-w, xC=area.x+Math.floor((area.w-w)/2);
    const yT=area.y, yB=area.y+area.h-h;
    const x=anchor.endsWith('l')?xL:anchor.endsWith('r')?xR:xC;
    const y=anchor.startsWith('t')?yT:yB;
    return {x,y,w,h};
  }
  function solve(config) {
    if(!config || typeof config!=='object' || Array.isArray(config)) return fail('INVALID_CONFIG');
    const {width,height,insets,exclusions,widgets} = config;
    if(!integer(width,240,4096)||!integer(height,240,2160))return fail('INVALID_VIEWPORT');
    if(!insets || !['top','right','bottom','left'].every(k=>integer(insets[k],0,200)))return fail('INVALID_INSETS');
    const gap=8;
    const area={x:insets.left+gap,y:insets.top+gap,w:width-insets.left-insets.right-2*gap,h:height-insets.top-insets.bottom-2*gap};
    if(area.w<32||area.h<32)return fail('NO_SAFE_AREA');
    if(!Array.isArray(exclusions)||exclusions.length>8||!exclusions.every(r=>rectValid(r,width,height)))return fail('INVALID_EXCLUSIONS');
    if(!Array.isArray(widgets)||!widgets.length||widgets.length>MAX_WIDGETS)return fail('INVALID_WIDGETS');
    const ids=new Set();
    for(const widget of widgets){
      if(!widget||typeof widget!=='object'||Array.isArray(widget)||!/^[-_a-zA-Z0-9]{1,28}$/.test(widget.id||'') ||ids.has(widget.id)
         || !integer(widget.w,40,500)||!integer(widget.h,24,300)||!integer(widget.priority,0,9)
         || !ANCHORS.includes(widget.anchor))return fail('INVALID_WIDGET');
      ids.add(widget.id);
    }
    const sorted=widgets.slice().sort((a,b)=>b.priority-a.priority||a.id.localeCompare(b.id,'en'));
    const placed=[];
    function allowed(r){return within(r,area)&&!exclusions.some(e=>overlap(r,e))&&!placed.some(p=>overlap(r,p));}
    for(const item of sorted){
      if(item.w>area.w||item.h>area.h)return fail('UNPLACEABLE:'+item.id);
      const choice=[];
      for(const anchor of [item.anchor,...ANCHORS.filter(a=>a!==item.anchor)]){
        const rect=candidate(anchor,area,item.w,item.h,gap);if(allowed(rect)){choice.push(rect);break;}
      }
      if(!choice.length){
        const preferred=candidate(item.anchor,area,item.w,item.h,gap);
        const scans=[];
        // Stable 8px search, with exact right/bottom edges as extra candidates.
        const xx=[];for(let x=area.x;x<=area.x+area.w-item.w;x+=8)xx.push(x);
        xx.push(area.x+area.w-item.w);
        const yy=[];for(let y=area.y;y<=area.y+area.h-item.h;y+=8)yy.push(y);
        yy.push(area.y+area.h-item.h);
        for(const y of new Set(yy))for(const x of new Set(xx)){
          const r={x,y,w:item.w,h:item.h};if(allowed(r))scans.push(r);
        }
        scans.sort((a,b)=>(Math.abs(a.x-preferred.x)+Math.abs(a.y-preferred.y))-(Math.abs(b.x-preferred.x)+Math.abs(b.y-preferred.y))||a.y-b.y||a.x-b.x);
        if(scans.length)choice.push(scans[0]);
      }
      if(!choice.length)return fail('UNPLACEABLE:'+item.id);
      placed.push({...choice[0],id:item.id,priority:item.priority,anchor:item.anchor});
    }
    return {ok:true,reason:'OK',placements:placed,requested:widgets.length,occupied:placed.reduce((n,p)=>n+p.w*p.h,0),safeArea:area};
  }
  function snapshot(config){return JSON.stringify(solve(config));}
  return Object.freeze({solve,snapshot,overlap,within,anchors:ANCHORS.slice()});
});
