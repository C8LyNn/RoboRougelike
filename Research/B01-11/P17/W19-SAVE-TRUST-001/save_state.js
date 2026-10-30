/* B01-11 SAVE-TRUST-001: read-only, synthetic UX contract, not a Save implementation. */
(function (root, factory) {
  const api = factory();
  if (typeof module === 'object' && module.exports) module.exports = api;
  else root.SaveTrust = api;
})(typeof globalThis !== 'undefined' ? globalThis : this, function () {
  'use strict';
  const identity = /^[A-Za-z0-9_-]{1,48}$/;
  const digest = /^[a-f0-9]{64}$/;
  const kinds = new Set([
    'BEGIN', 'SAVE_VERIFIED', 'SAVE_UNCERTAIN', 'SAVE_FAILED',
    'RECONCILE_BEGIN', 'RECONCILE_VERIFIED', 'RECONCILE_NOT_SAVED',
    'RECONCILE_INCONCLUSIVE', 'HOST_READY', 'DISCONNECT'
  ]);
  const failureReasons = new Set(['BUSY', 'STALE', 'CONFLICT', 'ROLLBACK', 'IO', 'INVALID_ID', 'CORRUPT']);
  const validRun = value => Number.isSafeInteger(value) && value > 0;
  const validId = value => typeof value === 'string' && identity.test(value);
  const validProof = proof => proof && typeof proof === 'object' &&
    proof.kind === 'durable-readback' && Number.isSafeInteger(proof.generation) &&
    proof.generation > 0 && typeof proof.digest === 'string' && digest.test(proof.digest);

  function initialState(runId, slot) {
    if (!validRun(runId) || !validId(slot)) throw new TypeError('Invalid run/slot identity');
    return Object.freeze({ runId, slot, phase: 'idle', lastSeq: 0, operationId: null,
      generation: null, digest: null, problem: null });
  }
  function no(state, reason) { return Object.freeze({ state, accepted: false, reason }); }
  function yes(state) { return Object.freeze({ state: Object.freeze(state), accepted: true, reason: 'accepted' }); }

  // The authoritative host MUST supply events. Reducer does not read disk, control Unity,
  // authenticate an emitter, or determine whether a disk commit actually occurred.
  function transition(state, event) {
    if (!state || !validRun(state.runId) || !validId(state.slot) || !Number.isSafeInteger(state.lastSeq))
      throw new TypeError('Invalid reducer state');
    if (!event || typeof event !== 'object' || !kinds.has(event.kind) ||
        !validRun(event.runId) || !validId(event.slot) ||
        !Number.isSafeInteger(event.seq) || event.seq <= 0)
      return no(state, 'invalid-event');
    if (event.runId !== state.runId || event.slot !== state.slot) return no(state, 'foreign-run-or-slot');
    if (event.seq <= state.lastSeq) return no(state, 'replayed-or-out-of-order');
    const next = { ...state, lastSeq: event.seq };
    const op = validId(event.operationId) && event.operationId === state.operationId;
    switch (event.kind) {
      case 'BEGIN':
        if (!validId(event.operationId)) return no(state, 'invalid-operation');
        if (!(state.phase === 'idle' || state.phase === 'saved')) return no(state, 'operation-not-allowed');
        next.phase = 'saving'; next.operationId = event.operationId; next.problem = null;
        return yes(next);
      case 'SAVE_VERIFIED':
        if (state.phase !== 'saving' || !op) return no(state, 'wrong-operation-or-phase');
        if (!validProof(event.proof) ||
            (state.generation !== null && event.proof.generation <= state.generation))
          return no(state, 'missing-or-nonmonotonic-readback');
        next.phase = 'saved'; next.generation = event.proof.generation;
        next.digest = event.proof.digest; next.operationId = null; next.problem = null;
        return yes(next);
      case 'SAVE_UNCERTAIN':
        if (state.phase !== 'saving' || !op) return no(state, 'wrong-operation-or-phase');
        next.phase = 'uncertain'; next.problem = 'AMBIGUOUS_COMMIT'; return yes(next);
      case 'SAVE_FAILED':
        if (state.phase !== 'saving' || !op) return no(state, 'wrong-operation-or-phase');
        if (!failureReasons.has(event.reason)) return no(state, 'invalid-failure-reason');
        next.phase = 'blocked'; next.problem = event.reason; next.operationId = null;
        return yes(next);
      case 'RECONCILE_BEGIN':
        if (state.phase !== 'uncertain' || !op) return no(state, 'wrong-operation-or-phase');
        next.phase = 'checking'; return yes(next);
      case 'RECONCILE_VERIFIED':
        if (state.phase !== 'checking' || !op) return no(state, 'wrong-operation-or-phase');
        if (!validProof(event.proof) ||
            (state.generation !== null && event.proof.generation <= state.generation))
          return no(state, 'missing-or-nonmonotonic-readback');
        next.phase = 'saved'; next.generation = event.proof.generation;
        next.digest = event.proof.digest; next.operationId = null; next.problem = null;
        return yes(next);
      case 'RECONCILE_NOT_SAVED':
        if (state.phase !== 'checking' || !op || event.confirmed !== true)
          return no(state, 'unconfirmed-reconciliation');
        next.phase = 'blocked'; next.problem = 'NO_DURABLE_COMMIT'; next.operationId = null;
        return yes(next);
      case 'RECONCILE_INCONCLUSIVE':
        if (state.phase !== 'checking' || !op) return no(state, 'wrong-operation-or-phase');
        next.phase = 'uncertain'; next.problem = 'AMBIGUOUS_COMMIT'; return yes(next);
      case 'HOST_READY':
        if (state.phase !== 'blocked' || event.confirmed !== true) return no(state, 'host-not-ready');
        next.phase = 'idle'; next.operationId = null; next.problem = null;
        return yes(next);
      case 'DISCONNECT':
        if (state.phase === 'uncertain') return no(state, 'already-uncertain');
        next.phase = (state.phase === 'saving' || state.phase === 'checking') ? 'uncertain' : 'blocked';
        next.problem = (next.phase === 'uncertain') ? 'AMBIGUOUS_COMMIT' : 'DISCONNECTED';
        return yes(next);
      default:
        return no(state, 'unknown-event');
    }
  }

  function copy(state) {
    if (!state || !validRun(state.runId)) throw new TypeError('Invalid state');
    switch (state.phase) {
      case 'idle': return { level: 'neutral', title: 'Gotowość do zapisu',
        detail: 'Brak potwierdzenia nowego zapisu dla bieżącej operacji.' };
      case 'saving': return { level: 'active', title: 'Trwa zapisywanie',
        detail: 'Nie wyłączaj gry. Potwierdzenie trwałości jeszcze nie dotarło.' };
      case 'saved': return { level: 'success', title: 'Zapis potwierdzony',
        detail: 'Host zgłosił odczyt kontrolny trwałego checkpointu (generacja ' + state.generation + ').' };
      case 'uncertain': return { level: 'danger', title: 'Nie można potwierdzić zapisu',
        detail: 'Wynik operacji jest nieznany. Nie zakładaj, że nowe postępy zostały zapisane.' };
      case 'checking': return { level: 'active', title: 'Sprawdzanie zapisu',
        detail: 'Trwa weryfikacja checkpointu po niejednoznacznym wyniku.' };
      case 'blocked':
        return { level: 'danger', title: 'Zapis niedostępny',
          detail: state.problem === 'NO_DURABLE_COMMIT'
            ? 'Weryfikacja nie potwierdziła zapisania nowych postępów.'
            : state.problem === 'BUSY'
              ? 'Inna sesja korzysta z tego miejsca zapisu. Nie nadpisujemy jej danych.'
              : 'Host zablokował zapis. Nie ma potwierdzenia zapisania nowych postępów.' };
      default: return { level: 'danger', title: 'Stan zapisu nieznany',
        detail: 'Nie pokazuj potwierdzenia zapisu; wymagana weryfikacja hosta.' };
    }
  }
  return Object.freeze({ initialState, transition, copy });
});
