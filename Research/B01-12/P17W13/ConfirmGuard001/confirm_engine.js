'use strict';
/* B01-12-CONFIRM-GUARD-001/R1: deterministic, side-effect-free confirmation reducer. */
(function (root, factory) {
  const api = factory();
  if (typeof module === 'object' && module.exports) module.exports = api;
  if (root) root.ConfirmGuard = api;
})(typeof globalThis === 'object' ? globalThis : undefined, function () {
  const ACTIONS = Object.freeze({
    abandon: 'Porzuć bieżącą próbę',
    discard: 'Odrzuć wybrany moduł',
    overwrite: 'Zastąp lokalny profil'
  });
  const MODES = Object.freeze(['hold', 'two-step']);
  const HOLD_MS = 1200;
  const ACK_DELAY_MS = 200;
  const TTL_MS = 6000;
  function plain(obj) { return obj !== null && typeof obj === 'object' && !Array.isArray(obj) && Object.getPrototypeOf(obj) === Object.prototype; }
  function exactly(obj, keys) { return Object.keys(obj).sort().join('|') === keys.slice().sort().join('|'); }
  function integer(n) { return Number.isSafeInteger(n) && n >= 0 && n <= 1e12; }
  function base() { return {phase:'idle', mode:'hold', action:'discard', serial:0, issuedAt:null, deadline:null, holdAt:null, pointer:null, clock:0, committed:0, receipt:null, message:'Wybierz działanie i uzbrój potwierdzenie.'}; }
  function clone(s) { return { ...s, receipt:s.receipt ? {...s.receipt} : null}; }
  function validate(s) {
    if (!plain(s) || !exactly(s,Object.keys(base()))) return false;
    if (!['idle','armed','holding','committed','expired'].includes(s.phase) || !MODES.includes(s.mode) || !Object.hasOwn(ACTIONS,s.action)) return false;
    if (!integer(s.serial) || !integer(s.clock) || !integer(s.committed) || s.committed > 1) return false;
    if (typeof s.message !== 'string' || s.message.length > 180) return false;
    if (s.phase==='idle') return s.issuedAt===null && s.deadline===null && s.holdAt===null && s.pointer===null && s.receipt===null;
    if (s.phase==='armed' || s.phase==='holding') {
      if (!integer(s.issuedAt) || !integer(s.deadline) || s.deadline!==s.issuedAt+TTL_MS || s.clock<s.issuedAt || s.clock>=s.deadline || s.receipt!==null) return false;
      if (s.phase==='armed') return s.holdAt===null && s.pointer===null;
      return s.mode==='hold' && integer(s.holdAt) && s.holdAt>=s.issuedAt && s.holdAt<=s.clock && typeof s.pointer==='string' && /^[a-z0-9_-]{1,48}$/i.test(s.pointer);
    }
    if (s.phase==='expired') return integer(s.issuedAt) && integer(s.deadline) && s.deadline===s.issuedAt+TTL_MS && s.clock>=s.deadline && s.holdAt===null && s.pointer===null && s.receipt===null;
    return s.phase==='committed' && s.committed===1 && integer(s.issuedAt) && integer(s.deadline) && s.deadline===s.issuedAt+TTL_MS && s.clock>=s.issuedAt && s.clock<s.deadline && s.holdAt===null && s.pointer===null && plain(s.receipt) && exactly(s.receipt,['serial','action','mode','at']) && s.receipt.serial===s.serial && s.receipt.action===s.action && s.receipt.mode===s.mode && integer(s.receipt.at) && s.receipt.at===s.clock;
  }
  function reject(s, reason) { return {ok:false, state:clone(s), reason}; }
  function success(s,reason) {return {ok:true, state:s,reason};}
  function resetPreserving(s,message) {return {...s, phase:'idle',issuedAt:null,deadline:null,holdAt:null,pointer:null,receipt:null, message};}
  function transition(original,event) {
    if (!validate(original)) return {ok:false,state:null,reason:'INVALID_STATE'};
    if (!plain(event) || !exactly(event,['type','at','value']) || typeof event.type !== 'string' || !integer(event.at)) return reject(original,'INVALID_EVENT');
    if (event.at<original.clock) return reject(original,'NON_MONOTONIC_CLOCK');
    if (event.at-original.clock>86400000 && event.type!=='RESET' && event.type!=='CANCEL') return reject(original,'CLOCK_JUMP');
    if (typeof event.value !== 'string' || event.value.length>80) return reject(original,'INVALID_VALUE');
    const s=clone(original); s.clock=event.at;
    if (event.type==='RESET') return success(resetPreserving({...s,committed:0},'Przywrócono stan demonstratora.'),'RESET');
    if (event.type==='CANCEL') return success(resetPreserving(s,'Anulowano. Nie wykonano żadnej operacji.'),'CANCELLED');
    if (['armed','holding'].includes(s.phase) && event.at>=s.deadline) {
      s.phase='expired';s.holdAt=null;s.pointer=null;s.message='Żądanie wygasło. Uzbrój nowe potwierdzenie.';
      return success(s,'EXPIRED');
    }
    if (event.type==='SET_MODE' || event.type==='SET_ACTION') {
      if (s.phase!=='idle' && s.phase!=='committed' && s.phase!=='expired') return reject(original,'BUSY');
      if (event.type==='SET_MODE') {
        if (!MODES.includes(event.value)) return reject(original,'UNKNOWN_MODE');
        const n=resetPreserving(s,'Wybrano metodę potwierdzenia.');n.mode=event.value;return success(n,'MODE_SET');
      }
      if (!Object.hasOwn(ACTIONS,event.value)) return reject(original,'UNKNOWN_ACTION');
      const n=resetPreserving(s,'Wybrano działanie.');n.action=event.value;return success(n,'ACTION_SET');
    }
    if (event.type==='ARM') {
      if (!['idle','expired','committed'].includes(s.phase)) return reject(original,'ALREADY_ARMED');
      if (s.serial>=999999999) return reject(original,'SERIAL_LIMIT');
      s.phase='armed';s.serial++;s.committed=0;s.issuedAt=event.at;s.deadline=event.at+TTL_MS;s.holdAt=null;s.pointer=null;s.receipt=null;
      s.message=s.mode==='hold'?'Przytrzymaj przez 1,2 s, następnie zwolnij.':'Potwierdź ponownie po 0,2 s.';
      return success(s,'ARMED');
    }
    if (event.type==='TICK') {
      if (event.value!=='') return reject(original,'INVALID_VALUE');
      if (s.phase!=='armed' && s.phase!=='holding') return reject(original,'TICK_NOT_ALLOWED');
      return success(s,'TICK');
    }
    if (event.type==='HOLD_START') {
      if (s.mode!=='hold' || s.phase!=='armed') return reject(original,'HOLD_NOT_ALLOWED');
      if (!/^[a-z0-9_-]{1,48}$/i.test(event.value)) return reject(original,'INVALID_POINTER');
      s.phase='holding';s.holdAt=event.at;s.pointer=event.value;s.message='Trwa przytrzymanie — nie zwalniaj przed 1,2 s.';
      return success(s,'HOLDING');
    }
    if (event.type==='HOLD_END') {
      if (s.mode!=='hold' || s.phase!=='holding') return reject(original,'NOT_HOLDING');
      if (event.value!==s.pointer) return reject(original,'POINTER_MISMATCH');
      if (event.at-s.holdAt<HOLD_MS) {
        s.phase='armed';s.holdAt=null;s.pointer=null;s.message='Zbyt krótko. Operacja nie została zatwierdzona.';
        return success(s,'TOO_SHORT');
      }
      return commit(s);
    }
    if (event.type==='CONFIRM') {
      if (event.value!=='') return reject(original,'INVALID_VALUE');
      if (s.mode!=='two-step' || s.phase!=='armed') return reject(original,'CONFIRM_NOT_ALLOWED');
      if (event.at-s.issuedAt<ACK_DELAY_MS) return reject(original,'TOO_EARLY');
      return commit(s);
    }
    return reject(original,'UNKNOWN_EVENT');
  }
  function commit(s) {
    s.phase='committed';s.holdAt=null;s.pointer=null;s.committed=1;
    s.receipt={serial:s.serial,action:s.action,mode:s.mode,at:s.clock};
    s.message='Potwierdzono w demonstratorze — bez zmiany stanu gry.';
    return success(s,'COMMITTED');
  }
  function progress(s) {if(!validate(s)) throw new Error('INVALID_STATE');return s.phase==='holding'? Math.min(100,Math.round((s.clock-s.holdAt)*100/HOLD_MS)):0;}
  return Object.freeze({ACTIONS,MODES,HOLD_MS,ACK_DELAY_MS,TTL_MS,initial:base,validate,transition,progress});
});
