/* B01-12-TUTORIALFLOW-001/R1 — isolated deterministic tutorial flow. No game writes. */
(function (root, factory) {
  'use strict';
  const exported = factory();
  if (typeof module === 'object' && module.exports) module.exports = exported;
  else root.B01TutorialFlow = exported;
})(typeof globalThis === 'object' ? globalThis : this, function () {
  'use strict';
  const STEPS = Object.freeze([
    Object.freeze({ id: 'garage_core', phase: 'garage', action: 'select_core', optional: false, title: 'Zaznacz rdzeń', hint: 'Wskaż rdzeń konstrukcji w garażu.' }),
    Object.freeze({ id: 'garage_wheel', phase: 'garage', action: 'mount_wheel', optional: false, title: 'Zamontuj koło', hint: 'Dodaj jeden moduł napędowy.' }),
    Object.freeze({ id: 'garage_weapon', phase: 'garage', action: 'inspect_weapon', optional: true, title: 'Sprawdź uzbrojenie', hint: 'Otwórz kartę broni lub pomiń poradę.' }),
    Object.freeze({ id: 'combat_fire', phase: 'combat', action: 'fire_a', optional: false, title: 'Wypróbuj strzał', hint: 'Użyj broni przypisanej do grupy A.' }),
    Object.freeze({ id: 'combat_dodge', phase: 'combat', action: 'dodge', optional: true, title: 'Wykonaj unik', hint: 'Przećwicz manewr lub pomiń poradę.' }),
    Object.freeze({ id: 'result_reward', phase: 'result', action: 'claim_reward', optional: false, title: 'Odbierz nagrodę', hint: 'Zakończ ćwiczenie przyciskiem odbioru.' })
  ]);
  const ALLOWED = Object.freeze(['ACTION', 'SKIP', 'BACK', 'RESET', 'RESTART']);
  function initial() {
    return { version: 1, index: 0, status: 'active', history: [], receipts: [], revision: 0 };
  }
  function assertState(state) {
    if (!state || state.version !== 1 || !Number.isInteger(state.index) || state.index < 0 || state.index > STEPS.length ||
      (state.status !== 'active' && state.status !== 'complete') || !Array.isArray(state.history) || !Array.isArray(state.receipts) ||
      !Number.isSafeInteger(state.revision) || state.revision < 0 || state.history.length !== state.index ||
      (state.status === 'complete') !== (state.index === STEPS.length)) throw new TypeError('INVALID_FLOW_STATE');
    for (let i = 0; i < state.history.length; i++) {
      const item = state.history[i];
      if (!item || item.step !== STEPS[i].id || (item.result !== 'done' && item.result !== 'skipped') ||
        (item.result === 'skipped' && !STEPS[i].optional)) throw new TypeError('INVALID_FLOW_HISTORY');
    }
    if (state.receipts.length > STEPS.length || state.receipts.some(x => typeof x !== 'string' || !/^[A-Za-z0-9:_-]{1,64}$/.test(x)) ||
      new Set(state.receipts).size !== state.receipts.length) throw new TypeError('INVALID_FLOW_RECEIPTS');
  }
  function view(state) {
    assertState(state);
    const current = state.index < STEPS.length ? STEPS[state.index] : null;
    return {
      index: state.index, total: STEPS.length, complete: state.status === 'complete',
      current: current ? { ...current } : null, progress: Math.round(state.index / STEPS.length * 100),
      history: state.history.map(x => ({ ...x })),
      steps: STEPS.map((x, i) => ({ ...x, state: i < state.index ? state.history[i].result : (i === state.index ? 'current' : 'pending') }))
    };
  }
  function output(state, code, changed, text) {
    return { state, changed, effect: { code, text } };
  }
  function advance(state, result, receipt) {
    const step = STEPS[state.index];
    const nextIndex = state.index + 1;
    const completed = nextIndex === STEPS.length;
    const next = {
      ...state, index: nextIndex, status: completed ? 'complete' : 'active',
      revision: state.revision + 1,
      history: [...state.history, { step: step.id, result }],
      receipts: receipt ? [...state.receipts, receipt] : state.receipts.slice()
    };
    return output(next, completed ? 'FLOW_COMPLETE' : (result === 'skipped' ? 'STEP_SKIPPED' : 'STEP_DONE'), true,
      completed ? 'Samouczek ukończony.' : result === 'skipped' ? `Pominięto: ${step.title}.` : `Wykonano: ${step.title}.`);
  }
  function transition(state, evt) {
    assertState(state);
    if (!evt || typeof evt !== 'object' || !ALLOWED.includes(evt.type)) return output(state, 'INVALID_EVENT', false, 'Nieznane zdarzenie.');
    if (evt.type === 'RESET') {
      const next = initial();
      next.revision = state.revision + 1;
      return output(next, 'RESET', true, 'Samouczek rozpoczęty od nowa.');
    }
    if (evt.type === 'RESTART') {
      if (state.status !== 'complete') return output(state, 'NOT_COMPLETE', false, 'Restart dostępny po ukończeniu.');
      const next = initial();
      next.revision = state.revision + 1;
      return output(next, 'RESTART', true, 'Nowa sesja samouczka.');
    }
    if (evt.type === 'BACK') {
      if (!state.index) return output(state, 'AT_START', false, 'To pierwszy krok.');
      const newIndex = state.index - 1;
      return output({ ...state, index: newIndex, status: 'active', revision: state.revision + 1,
        history: state.history.slice(0, newIndex).map(item => ({ ...item })), receipts: [] }, 'STEP_BACK', true,
        'Cofnięto wyłącznie postęp poradnika; akcje gry nie są odwracane.');
    }
    if (state.status === 'complete') return output(state, 'ALREADY_COMPLETE', false, 'Samouczek został ukończony.');
    const step = STEPS[state.index];
    if (evt.type === 'SKIP') {
      return step.optional ? advance(state, 'skipped', null) : output(state, 'REQUIRED_STEP', false, 'Tego kroku nie można pominąć.');
    }
    if (typeof evt.id !== 'string' || !/^[A-Za-z0-9:_-]{1,64}$/.test(evt.id))
      return output(state, 'INVALID_RECEIPT', false, 'Brak prawidłowego identyfikatora akcji.');
    if (state.receipts.includes(evt.id)) return output(state, 'DUPLICATE', false, 'Ta sama akcja została już odebrana.');
    if (evt.phase !== step.phase || evt.action !== step.action)
      return output(state, 'WRONG_ACTION', false, 'Ta akcja nie spełnia aktualnego kroku.');
    return advance(state, 'done', evt.id);
  }
  return Object.freeze({ STEPS, initial, view, transition });
});
