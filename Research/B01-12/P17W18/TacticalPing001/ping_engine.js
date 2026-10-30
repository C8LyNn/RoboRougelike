/* B01-12 TACTICAL-PING-001/R1: deterministic, side-effect-free tactical marker reducer. */
(function (root, factory) {
  const api = factory();
  if (typeof module !== 'undefined' && module.exports) module.exports = api;
  else root.B01Ping = api;
})(typeof globalThis === 'object' ? globalThis : this, function () {
  'use strict';
  const TYPES = ['attack', 'defend', 'repair', 'move'];
  const TTL = { attack: 6000, defend: 8000, repair: 10000, move: 5000 };
  const CAP = 6, MAX_TIME = 1000000000;
  const keys = (o, expected) => !!o && typeof o === 'object' && !Array.isArray(o) &&
    Object.getPrototypeOf(o) === Object.prototype &&
    Object.keys(o).sort().join('|') === expected.slice().sort().join('|');
  const validTime = t => Number.isSafeInteger(t) && t >= 0 && t <= MAX_TIME;
  const validPoint = p => !!p && typeof p === 'object' && !Array.isArray(p) &&
    Object.keys(p).sort().join('|') === 'x|y' && Number.isSafeInteger(p.x) && Number.isSafeInteger(p.y) &&
    p.x >= 0 && p.x <= 999 && p.y >= 0 && p.y <= 999;
  const validID = x => typeof x === 'string' && /^[A-Z0-9]{1,18}$/.test(x);
  function initial() { return { version: 1, clock: 0, serial: 0, selected: null, markers: [] }; }
  function valid(s) {
    if (!keys(s, ['version','clock','serial','selected','markers']) || s.version !== 1 || !validTime(s.clock) ||
      !Number.isSafeInteger(s.serial) || s.serial < 0 || s.serial > 1000000 ||
      !(s.selected === null || validID(s.selected)) || !Array.isArray(s.markers) || s.markers.length > CAP) return false;
    const ids = new Set();
    for (const m of s.markers) {
      if (!keys(m,['id','type','point','created','expires','priority']) || !validID(m.id) || ids.has(m.id) ||
        !TYPES.includes(m.type) || !validPoint(m.point) || !validTime(m.created) || !validTime(m.expires) ||
        m.created > s.clock || m.expires <= s.clock || m.expires !== m.created + TTL[m.type] ||
        !Number.isSafeInteger(m.priority) || m.priority < 1 || m.priority > 3) return false;
      ids.add(m.id);
    }
    return s.selected === null || ids.has(s.selected);
  }
  const reject = (state, reason) => ({ ok: false, reason, state });
  const accept = state => ({ ok: true, state });
  function step(state, event) {
    if (!valid(state)) return reject(state, 'INVALID_STATE');
    if (!event || typeof event !== 'object' || Array.isArray(event) || Object.getPrototypeOf(event) !== Object.prototype ||
        typeof event.op !== 'string') return reject(state, 'INVALID_EVENT');
    if (event.op === 'tick') {
      if (!keys(event,['op','time']) || !validTime(event.time) || event.time < state.clock) return reject(state,'CLOCK_REWIND');
      const markers = state.markers.filter(m => m.expires > event.time);
      const selected = markers.some(m => m.id === state.selected) ? state.selected : null;
      return accept({ ...state, clock: event.time, markers, selected });
    }
    if (event.op === 'add') {
      if (!keys(event,['op','id','type','point','priority']) || !validID(event.id) || !TYPES.includes(event.type) ||
        !validPoint(event.point) || !Number.isSafeInteger(event.priority) || event.priority < 1 || event.priority > 3) return reject(state,'INVALID_MARKER');
      if (state.markers.some(m => m.id === event.id)) return reject(state,'DUPLICATE_ID');
      if (state.markers.length >= CAP) return reject(state,'CAPACITY');
      if (state.serial >= 1000000 || state.clock + TTL[event.type] > MAX_TIME) return reject(state,'OVERFLOW');
      const marker = { id:event.id, type:event.type, point:{...event.point}, created:state.clock,
        expires:state.clock + TTL[event.type], priority:event.priority };
      return accept({ ...state, serial:state.serial+1, selected:marker.id, markers:[...state.markers,marker] });
    }
    if (event.op === 'select') {
      if (!keys(event,['op','id']) || !(event.id === null || validID(event.id))) return reject(state,'INVALID_SELECTION');
      if (event.id !== null && !state.markers.some(m => m.id === event.id)) return reject(state,'UNKNOWN_ID');
      return accept({...state,selected:event.id});
    }
    if (event.op === 'remove') {
      if (!keys(event,['op','id']) || !validID(event.id)) return reject(state,'INVALID_ID');
      if (!state.markers.some(m => m.id === event.id)) return reject(state,'UNKNOWN_ID');
      return accept({ ...state, markers:state.markers.filter(m=>m.id!==event.id), selected:state.selected===event.id?null:state.selected });
    }
    if (event.op === 'reset') {
      if (!keys(event,['op'])) return reject(state,'INVALID_EVENT');
      return accept(initial());
    }
    return reject(state,'UNKNOWN_OP');
  }
  function exportState(s) { if (!valid(s)) throw new Error('INVALID_STATE'); return JSON.stringify(s); }
  function importState(json) {
    if (typeof json !== 'string' || json.length > 5000) return {ok:false,reason:'INVALID_JSON'};
    try { const obj=JSON.parse(json); return valid(obj)?accept(obj):{ok:false,reason:'INVALID_SNAPSHOT'}; }
    catch { return {ok:false,reason:'INVALID_JSON'}; }
  }
  const rank = s => valid(s) ? [...s.markers].sort((a,b)=>b.priority-a.priority||a.expires-b.expires||a.id.localeCompare(b.id)) : [];
  return Object.freeze({ TYPES:Object.freeze([...TYPES]), TTL:Object.freeze({...TTL}), CAP, initial, valid, step, rank, exportState, importState });
});
