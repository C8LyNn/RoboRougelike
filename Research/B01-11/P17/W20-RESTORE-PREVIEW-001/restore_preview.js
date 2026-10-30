/* B01-11 RESTORE-PREVIEW-001: advisory UI only. Never executes Save/Load. */
(function (root, factory) {
  const api = factory();
  if (typeof module === 'object' && module.exports) module.exports = api;
  else root.RestorePreview = api;
})(typeof globalThis !== 'undefined' ? globalThis : this, function () {
  'use strict';
  const ID = /^[A-Za-z0-9_-]{1,48}$/;
  const SHA = /^[a-f0-9]{64}$/;
  const integer = x => Number.isSafeInteger(x) && x >= 0;
  const positive = x => Number.isSafeInteger(x) && x > 0;
  const fail = reason => Object.freeze({ allowed: false, reason });
  const ok = Object.freeze({ allowed: true, reason: 'READY' });
  const stamp = s => JSON.stringify([
    s.runId,s.slotId,s.generation,s.sha256,s.partCount,s.detachedCount,s.consumedCount
  ]);
  const authorityStamp = h => JSON.stringify([
    h.epoch,h.verifiedReadback,h.quiescent,h.singleSlotOwner,h.canonicalReady,h.unresolvedCommit
  ]);

  function validSnapshot(s) {
    return !!(s && typeof s === 'object' && positive(s.runId) &&
      typeof s.slotId === 'string' && ID.test(s.slotId) &&
      positive(s.generation) && typeof s.sha256 === 'string' && SHA.test(s.sha256) &&
      integer(s.partCount) && integer(s.detachedCount) &&
      s.detachedCount <= s.partCount && integer(s.consumedCount));
  }
  function validHost(h) {
    return !!(h && typeof h === 'object' && positive(h.epoch) &&
      typeof h.verifiedReadback === 'boolean' && typeof h.quiescent === 'boolean' &&
      typeof h.singleSlotOwner === 'boolean' && typeof h.canonicalReady === 'boolean' &&
      typeof h.unresolvedCommit === 'boolean');
  }
  function gate(current, target, host) {
    if (!validSnapshot(current) || !validSnapshot(target) || !validHost(host)) return fail('INVALID_METADATA');
    if (current.runId !== target.runId) return fail('FOREIGN_RUN');
    if (current.slotId !== target.slotId) return fail('FOREIGN_SLOT');
    if (!host.verifiedReadback) return fail('UNVERIFIED_TARGET');
    if (host.unresolvedCommit) return fail('AMBIGUOUS_COMMIT');
    if (!host.singleSlotOwner) return fail('HOST_BUSY');
    if (!host.quiescent) return fail('WORLD_ACTIVE');
    if (!host.canonicalReady) return fail('CANONICAL_API_PENDING');
    if (target.generation < current.generation) return fail('OLDER_GENERATION');
    if (target.generation === current.generation) {
      if (target.sha256 !== current.sha256) return fail('FORKED_GENERATION');
      return fail('ALREADY_CURRENT');
    }
    if (target.consumedCount < current.consumedCount) return fail('POSSIBLE_ID_RESURRECTION');
    return ok;
  }
  function session(current, target, host) {
    const verdict = gate(current,target,host);
    return Object.freeze({ phase: verdict.allowed ? 'review' : 'blocked', verdict: verdict.reason,
      current: validSnapshot(current) ? Object.freeze({...current}) : null,
      target: validSnapshot(target) ? Object.freeze({...target}) : null,
      host: validHost(host) ? Object.freeze({...host}) : null,
      currentStamp: validSnapshot(current) ? stamp(current) : null,
      targetStamp: validSnapshot(target) ? stamp(target) : null,
      hostStamp: validHost(host) ? authorityStamp(host) : null });
  }
  function arm(s) {
    if (!s || s.phase !== 'review' || s.verdict !== 'READY') return Object.freeze({accepted:false,reason:'NOT_REVIEWABLE',state:s});
    return Object.freeze({accepted:true,reason:'ARMED',state:Object.freeze({...s,phase:'armed'})});
  }
  // Revalidation checks ALL original metadata plus authority status. No side effects.
  // A real host must revalidate its own disk+world ledger inside the transaction.
  function request(s, latestCurrent, latestTarget, latestHost) {
    if (!s || s.phase !== 'armed') return Object.freeze({accepted:false,reason:'NOT_ARMED',state:s});
    const verdict = gate(latestCurrent,latestTarget,latestHost);
    if (!verdict.allowed) return Object.freeze({accepted:false,reason:verdict.reason,state:s});
    if (stamp(latestCurrent) !== s.currentStamp || stamp(latestTarget) !== s.targetStamp ||
        authorityStamp(latestHost) !== s.hostStamp)
      return Object.freeze({accepted:false,reason:'CHANGED_DURING_CONFIRMATION',state:s});
    return Object.freeze({accepted:true,reason:'REQUEST_ONLY',state:Object.freeze({...s,phase:'requested'})});
  }
  function display(s) {
    if (!s || typeof s !== 'object') return {level:'danger',title:'Brak danych',message:'Odmowa operacji.'};
    if (s.phase === 'requested') return {level:'active',title:'Prośba o wczytanie przekazana',
      message:'To nie jest potwierdzenie wczytania. Autorytatywny host musi osobno potwierdzić wynik.'};
    if (s.phase === 'armed') return {level:'warning',title:'Potwierdź wybór checkpointu',
      message:'Przed przekazaniem prośby ponownie sprawdzimy tożsamość, generację i stan hosta.'};
    if (s.phase === 'review') return {level:'neutral',title:'Dostępny punkt wczytania',
      message:'Podgląd metadanych. Potwierdzenie nie omija sprawdzenia hosta. Wczytanie może zmienić stan robota.'};
    const messages = {
      INVALID_METADATA:'Nieprawidłowe lub niekompletne dane checkpointu.',
      FOREIGN_RUN:'Checkpoint należy do innego przebiegu.',
      FOREIGN_SLOT:'Checkpoint należy do innego miejsca zapisu.',
      UNVERIFIED_TARGET:'Host nie potwierdził odczytu checkpointu.',
      AMBIGUOUS_COMMIT:'Nie ustalono wyniku poprzedniej transakcji zapisu.',
      HOST_BUSY:'Inny host posiada miejsce zapisu.',
      WORLD_ACTIVE:'Świat nie został bezpiecznie wstrzymany.',
      CANONICAL_API_PENDING:'Kanoniczne API odtwarzania świata nie jest gotowe.',
      OLDER_GENERATION:'Starszy checkpoint: wczytanie wymaga osobnej polityki odzyskiwania.',
      FORKED_GENERATION:'Konflikt: identyczna generacja, różne dane.',
      ALREADY_CURRENT:'Ten checkpoint jest już bieżący.',
      POSSIBLE_ID_RESURRECTION:'Mniej zużytych identyfikatorów części; możliwe przywrócenie usuniętych elementów.'
    };
    return {level:'danger',title:'Wczytanie zablokowane',message:messages[s.verdict] || 'Brak uprawnionego potwierdzenia.'};
  }
  function delta(s) {
    if (!s || !s.current || !s.target || s.phase === 'blocked') return null;
    return Object.freeze({partChange:s.target.partCount - s.current.partCount,
      detachedChange:s.target.detachedCount - s.current.detachedCount,
      consumedChange:s.target.consumedCount - s.current.consumedCount,
      generationsAhead:s.target.generation - s.current.generation});
  }
  return Object.freeze({validSnapshot,validHost,gate,session,arm,request,display,delta});
});
