# B01-11 | PASS17 W20 | RESTORE-PREVIEW-001/R1

## New, distinct UX functionality

This standalone browser and Node package previews the consequences of a checkpoint LOAD and requires a second explicit confirmation. Prior W19 SAVE-TRUST-001 PR #658 addressed status of SAVE attempts. No game Save/Load host is implemented or modified here.

- Original source: Research/B01-11/P17/W20-RESTORE-PREVIEW-001/restore_preview.js
- Original 75-case tests: Research/B01-11/P17/W20-RESTORE-PREVIEW-001/test_restore_preview.cjs
- Functional Polish browser demonstration: index.html (four distinct scenarios).
- Exact manifest: manifest.json (five source files; bytes + SHA256, self excluded).
- Unique two-OS source workflow under .github/workflows.

The renderer rejects foreign run and slot, invalid/forked/older generation, unverified readback, uncertain durable commit, competing host, non-quiescent world, missing canonical API, and possible consumed-ID resurrection. It compares module counts and detached/consumed ID counts only as UX hints, not a physical or ID integrity guarantee.

Two-stage interaction: review -> armed -> REQUEST_ONLY. At the moment of the proposed request the reducer revalidates original run/slot/generation/digest/count fields and host epoch/status; changes abort the request. REQUEST_ONLY never means that any Unity world was actually restored. Host-supplied booleans are **synthetic and forgeable in this demo**. Real B01-23/62 API MUST revalidate authoritative disk checkpoint, private Rig array and AtomicDockLedger within a single authorized transaction before any physical publication.

## Testing
Run: node --check Research/B01-11/P17/W20-RESTORE-PREVIEW-001/restore_preview.js
Run: node --test Research/B01-11/P17/W20-RESTORE-PREVIEW-001/test_restore_preview.cjs

Cases challenge identity, same-generation fork, malformed metadata, 150 generated newer checkpoints, 250 rejected fork digests, authority/stale changes in the time between arm and confirm, genuine original HTML and manifest. Tests are ORIGINAL JS SOURCE (not Unity, not human research). Windows/Ubuntu source tests in dedicated GitHub Actions at exact PR SHA.

## Receivers and gates

- B01-23/62: select exactly one approved R4 (#647 versus #652), then separately implement trusted read-only state API for #666; no source import in this package.
- B01-20 and B01-12: investigate whether the language and 2-step restore choice can be adapted to authorized in-game HUD. Do not assume this browser state is authoritative.
- B01-61/41: independent real checkpoint/consumed ledger/lease/transaction proof at exact chosen R4, and race review.
- B01-13: eventual human comprehension study; a 100% understanding target for critical warnings is a proposed criterion, NOT measured.
- B01-40: independent source/G0 intake; B01-60: activated Unity G1 or NOT_RUN.
- B01-00/Codex: handoff later, only with separate authorization, no implied ACK.

Six lifecycle fields: IMPLEMENTED=YES; TESTED=SOURCE_ONLY if CI verified; QA_ACCEPTED=NO; READY_FOR_INTEGRATOR=NO; INTEGRATED=NO; RELEASED=NO. SOURCE_G0=PENDING; NATIVE_G1=NOT_RUN; LIVE_GAME_SAVE=HOLD; CODEX_ACK=NONE; G2=HOLD; INTEGRATOR_FEEDBACK=NOT_MEASURED.

Ownership: only unique B01-11 Research folder and B01-11 workflow; no Assets, protected branch, frozen #510/#556/#596/#642, R4 #647/#652 or ALT #604/#644 edits, no merge.
