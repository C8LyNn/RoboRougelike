/* B01-12-A11Y-PREFS-001 R1 — independent, deterministic accessibility preference model.
   No localStorage, networking, Unity runtime mutation, or HTML dependencies. */
(function(root, factory) {
  const api = factory();
  if (typeof module === 'object' && module.exports) module.exports = api;
  if (root) root.B01A11y = api;
})(typeof globalThis !== 'undefined' ? globalThis : this, function() {
  'use strict';
  const SCHEMA = 'b01-12.a11y-profile';
  const VERSION = 1;
  const MAX_HISTORY = 24;
  const MAX_IMPORT_BYTES = 4096;
  const DEFAULT = Object.freeze({ contrast: 'standard', textScale: 100, motion: 'normal', cues: 'icon-text', captions: true });
  const ALLOWED = Object.freeze({
    contrast: ['standard','high'], textScale: [100,125,150],
    motion: ['normal','reduced'], cues: ['icon-text','icon-text-pattern'], captions: [true,false]
  });
  const KEYS = Object.freeze(Object.keys(DEFAULT));
  const PALETTES = Object.freeze({
    standard: Object.freeze({ bg: '#101827', surface:'#1d2b41', fg:'#f2f7ff', muted:'#c5d2e5', accent:'#9bddff', warning:'#ffd88a' }),
    high: Object.freeze({ bg:'#000000', surface:'#111111', fg:'#ffffff', muted:'#ffffff', accent:'#00ffff', warning:'#ffff00' })
  });
  function plain(o) { return o !== null && typeof o === 'object' && !Array.isArray(o) && Object.getPrototypeOf(o) === Object.prototype; }
  function keysMatch(obj, exact) { return plain(obj) && Object.keys(obj).length === exact.length && exact.every(k => Object.hasOwn(obj,k)); }
  function clonePrefs(p) { return {contrast:p.contrast,textScale:p.textScale,motion:p.motion,cues:p.cues,captions:p.captions}; }
  function validPrefs(p) { return keysMatch(p,KEYS) && KEYS.every(k => ALLOWED[k].some(v => v === p[k])); }
  function equal(a,b) { return KEYS.every(k => a[k] === b[k]); }
  function validState(s) {
    return plain(s) && keysMatch(s,['prefs','past','future','revision','exported','status']) && validPrefs(s.prefs)
      && Array.isArray(s.past) && s.past.length <= MAX_HISTORY && s.past.every(validPrefs)
      && Array.isArray(s.future) && s.future.length <= MAX_HISTORY && s.future.every(validPrefs)
      && Number.isSafeInteger(s.revision) && s.revision >= 0 && s.revision < Number.MAX_SAFE_INTEGER
      && (s.exported === null || (typeof s.exported === 'string' && s.exported.length <= MAX_IMPORT_BYTES))
      && typeof s.status === 'string' && s.status.length <= 120;
  }
  function fresh() { return {prefs:clonePrefs(DEFAULT),past:[],future:[],revision:0,exported:null,status:'Ustawienia domyślne.'}; }
  function success(s,prefs,description) {
    if (equal(s.prefs,prefs)) return {state:s,ok:true,changed:false,reason:'Brak zmian.'};
    let next={prefs:clonePrefs(prefs),past:[...s.past.slice(1-MAX_HISTORY),clonePrefs(s.prefs)].slice(-MAX_HISTORY),future:[],revision:s.revision+1,exported:null,status:description};
    return {state:next,ok:true,changed:true,reason:description};
  }
  function fail(s,reason) { return {state:s,ok:false,changed:false,reason}; }
  function parseProfile(input) {
    if (typeof input !== 'string' || input.length > MAX_IMPORT_BYTES) return {ok:false,error:'Nieprawidłowa długość profilu.'};
    let doc;
    try { doc=JSON.parse(input); } catch (_) { return {ok:false,error:'Niepoprawny JSON.'}; }
    if (!keysMatch(doc,['schema','version','prefs']) || doc.schema !== SCHEMA || doc.version !== VERSION || !validPrefs(doc.prefs)) {
      return {ok:false,error:'Nieobsługiwany schemat lub wartości.'};
    }
    return {ok:true,prefs:clonePrefs(doc.prefs)};
  }
  function profile(prefs) { if(!validPrefs(prefs)) throw Error('Invalid preferences'); return JSON.stringify({schema:SCHEMA,version:VERSION,prefs:clonePrefs(prefs)},null,2); }
  function transition(s, event) {
    if (!validState(s)) throw new TypeError('Untrusted or invalid state: fail closed');
    if (!plain(event) || typeof event.type !== 'string') return fail(s,'Nieprawidłowe zdarzenie.');
    switch(event.type) {
      case 'SET': {
        if (!keysMatch(event,['type','key','value']) || !Object.hasOwn(ALLOWED,event.key) || !ALLOWED[event.key].some(v=>v===event.value)) return fail(s,'Wartość ustawienia jest niedozwolona.');
        return success(s,{...s.prefs,[event.key]:event.value},'Zmieniono: '+event.key+'.');
      }
      case 'RESET': if (!keysMatch(event,['type'])) return fail(s,'Nieprawidłowe zdarzenie.'); return success(s,DEFAULT,'Przywrócono ustawienia domyślne.');
      case 'UNDO': {
        if (!keysMatch(event,['type'])) return fail(s,'Nieprawidłowe zdarzenie.');
        if (!s.past.length) return fail(s,'Brak zmian do cofnięcia.');
        const prev=s.past[s.past.length-1];
        return {ok:true,changed:true,reason:'Cofnięto zmianę.',state:{prefs:clonePrefs(prev),past:s.past.slice(0,-1).map(clonePrefs),future:[...s.future,clonePrefs(s.prefs)].slice(-MAX_HISTORY),revision:s.revision+1,exported:null,status:'Cofnięto zmianę.'}};
      }
      case 'REDO': {
        if (!keysMatch(event,['type'])) return fail(s,'Nieprawidłowe zdarzenie.');
        if (!s.future.length) return fail(s,'Brak zmian do ponowienia.');
        const next=s.future[s.future.length-1];
        return {ok:true,changed:true,reason:'Ponowiono zmianę.',state:{prefs:clonePrefs(next),past:[...s.past,clonePrefs(s.prefs)].slice(-MAX_HISTORY),future:s.future.slice(0,-1).map(clonePrefs),revision:s.revision+1,exported:null,status:'Ponowiono zmianę.'}};
      }
      case 'EXPORT': {
        if (!keysMatch(event,['type'])) return fail(s,'Nieprawidłowe zdarzenie.');
        const text=profile(s.prefs);
        return {ok:true,changed:false,reason:'Profil wyeksportowany.',state:{...s,exported:text,status:'Profil wyeksportowany.'},output:text};
      }
      case 'IMPORT': {
        if (!keysMatch(event,['type','text'])) return fail(s,'Nieprawidłowe zdarzenie.');
        const parsed=parseProfile(event.text);
        if (!parsed.ok) return fail(s,parsed.error);
        return success(s,parsed.prefs,'Zaimportowano poprawny profil.');
      }
      default: return fail(s,'Nieznany typ zdarzenia.');
    }
  }
  function srgb(hex) { const h=hex.replace('#','');return [0,2,4].map(i=>parseInt(h.slice(i,i+2),16)/255).map(c=>c<=0.04045?c/12.92:Math.pow((c+0.055)/1.055,2.4)); }
  function luminance(hex) {const [r,g,b]=srgb(hex);return 0.2126*r+0.7152*g+0.0722*b;}
  function contrast(a,b) { const x=luminance(a),y=luminance(b);return (Math.max(x,y)+0.05)/(Math.min(x,y)+0.05); }
  function preview(p) {
    if (!validPrefs(p)) throw new TypeError('Invalid preferences');
    const pal=PALETTES[p.contrast];
    return { palette:pal,fontScale:p.textScale/100,animationMs:p.motion==='reduced'?0:220,showPattern:p.cues==='icon-text-pattern',showCaptions:p.captions,
      minimumTextContrast:Math.min(contrast(pal.fg,pal.surface),contrast(pal.muted,pal.surface),contrast(pal.fg,pal.bg)) };
  }
  return Object.freeze({SCHEMA,VERSION,DEFAULT,ALLOWED,PALETTES,MAX_HISTORY,fresh,transition,profile,parseProfile,preview,contrast,validState});
});
