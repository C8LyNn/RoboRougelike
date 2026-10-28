/* B01-10 PASS15 — LD02 prototype-only rule engine.
 * Research, NOT Unity R1RunController or a Codex gameplay integration.
 * No real enemy spawning, no player physics, no salvage or Scrap awards.
 * Browser: window.LD02Encounter; Node/validator: globalThis.LD02Encounter.
 * Config is exactly LD02_EncounterRuleset_R1.json; no external libraries.
 */
(function(root) {
  "use strict";
  const validNames = new Set(["Basic", "Automatic", "Heavy", "Shotgun", "Railgun",
    "BeamCutter", "RocketLauncher", "RotarySaw", "EmpEmitter"]);

  function cloneLoadout(value) {
    const x = value || {};
    return {
      types: Array.isArray(x.types) ? x.types.slice() : ["Basic","Automatic"],
      barrels: x.barrels === undefined ? 6 : x.barrels,
      functionalDrive: x.functionalDrive === undefined ? true : !!x.functionalDrive
    };
  }
  function validateLoadout(input) {
    const l = cloneLoadout(input);
    if (!l.functionalDrive) return "no-selected-drive";
    if (l.types.length === 0) return "no-functional-weapon";
    if (l.types.length > 2) return "max-2-families";
    if (l.types.some(x => !validNames.has(x))) return "invalid-weapon-family";
    if (new Set(l.types).size !== l.types.length) return "duplicate-family";
    if (!Number.isInteger(l.barrels) || l.barrels < l.types.length) return "invalid-barrel-count";
    return null;
  }
  function initial(loadout) {
    return {
      phase: "setup", loadout: cloneLoadout(loadout), route: null,
      hp: 100, kills: 0, remaining: 2, gridSteps: 0, scrap: 0,
      lastError: null, message: "Wybierz konfigurację i rozpocznij próbę.",
      events: 0
    };
  }
  function advance(state, event, config) {
    const s = { ...state, loadout: cloneLoadout(state.loadout), lastError: null, events: state.events + 1 };
    const e = event || {};
    if (e.type === "retry") return initial(s.loadout);
    if (e.type === "loadout") {
      if (s.phase !== "setup") { s.lastError = "loadout-locked"; return s; }
      s.loadout = cloneLoadout(e.value);
      s.message = "Zmieniono konfigurację prototypu.";
      return s;
    }
    if (e.type === "start") {
      if (s.phase !== "setup") { s.lastError = "already-started"; return s; }
      const error = validateLoadout(s.loadout);
      if (error) { s.lastError = error; s.message = "Start odmówiony: " + error; return s; }
      s.phase = "approach";
      s.message = "Wybierz trasę: krótszą otwartą lub dłuższy objazd.";
      return s;
    }
    if (e.type === "abort") {
      if (["approach", "combat", "extract"].includes(s.phase)) {
        s.phase = "lost"; s.message = "Próba przerwana, wybierz PONÓW.";
      } else s.lastError = "not-running";
      return s;
    }
    if (e.type === "route") {
      if (s.phase !== "approach") { s.lastError = "route-not-available"; return s; }
      const route = config.scenario.routes.find(x => x.id === e.id);
      if (!route) { s.lastError = "unknown-route"; return s; }
      s.route = route.id;
      s.gridSteps = route.nominalMeters / config.scenario.gridMeters;
      s.hp = Math.max(0, s.hp - route.prototypeExposureDamage);
      s.phase = s.hp === 0 ? "lost" : "combat";
      s.message = "Podejście " + route.label + " — hipotetyczne obrażenia " +
        route.prototypeExposureDamage + " HP; eliminuj cele E1/E2.";
      return s;
    }
    if (e.type === "frame" || e.type === "hit") {
      if (s.phase !== "combat" && s.phase !== "extract") {
        s.lastError = "no-active-combat"; return s;
      }
      const amount = e.type === "hit" ? e.damage : e.damage;
      const count = e.type === "frame" ? (e.defeated === undefined ? 0 : e.defeated) : 0;
      if (!Number.isInteger(amount) || amount < 0 ||
          !Number.isInteger(count) || count < 0) {
        s.lastError = "invalid-frame"; return s;
      }
      // Mirrors published R1 defeat-before-victory precedence as a PROTOTYPE contract.
      s.hp = Math.max(0, s.hp - amount);
      if (s.hp === 0) {
        s.phase = "lost";
        s.message = "PORAŻKA: uszkodzenie ma pierwszeństwo w tej samej klatce.";
        return s;
      }
      const killed = Math.min(s.remaining, count);
      s.kills += killed;
      s.remaining -= killed;
      if (s.remaining === 0) s.phase = "extract";
      s.message = killed ? "Cele wyeliminowane: " + s.kills + "/2" :
        "Otrzymano " + amount + " obrażeń.";
      return s;
    }
    if (e.type === "fire") {
      if (s.phase !== "combat") { s.lastError = "fire-not-available"; return s; }
      if (e.group !== "A" && e.group !== "B") {
        s.lastError = "invalid-fire-group"; return s;
      }
      if (e.group === "B" && s.loadout.types.length < 2) {
        s.lastError = "missing-group-b"; return s;
      }
      s.remaining -= 1;
      s.kills += 1;
      s.phase = s.remaining === 0 ? "extract" : "combat";
      s.message = "Zgrupowanie " + e.group + ": cel " + s.kills + "/2. " +
        (s.phase === "extract" ? "Można dotknąć mety." : "Drugi cel pozostaje.");
      return s;
    }
    if (e.type === "finish") {
      if (s.phase === "extract" && s.hp > 0 && s.kills === 2) {
        s.phase = "won";
        s.message = "WYGRANA BADAWCZA: 2/2 cele i meta. Złom: 0.";
      } else {
        s.lastError = "objectives-not-clear";
        s.message = "META NIE ZALICZA STARĆ: najpierw 2/2 cele.";
      }
      return s;
    }
    s.lastError = "unknown-event";
    return s;
  }
  root.LD02Encounter = Object.freeze({ initial, advance, validateLoadout });
})(globalThis);
