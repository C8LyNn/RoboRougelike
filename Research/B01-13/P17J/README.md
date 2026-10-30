# B01-13 REATTACH-TRIAL-MATRIX-001/R1 (PASS17 WAVE12)

Owner: B01-13 HUMAN PLAYTESTING. Dispatch issue #518; governance #276.
Only selected Secure Reattach V2 producer: #510 @ d94400d95d13db7f27e1533df05c9292d5380431. Alternatives #511 and #494 HOLD. Nothing in their source trees is changed.

## Functional scope
Node.js 20+ original offline technical-operator tool: physically SHA256-hash the supplied EXE, generate a 4x4 Williams counterbalanced matrix over Light/Heavy x clear/overlap docking cases, and validate complete observation records. Every condition occupies each order position once per 4-session block and all 12 directed adjacent carryover pairs occur once. This is order counterbalancing, NOT randomized sampling or a tested Unity function. Four to 64 rehearsal sessions (multiple of four), with four cards each.

On reports, if an overlapping module physically changes rig state it is PHYSICAL_SAFETY_RED; missing/contradictory visual confirmation is FEEDBACK_RED. PRODUCT_BLOCKED, SETUP_BLOCKED and NOT_RUN have separate counts. This does not prove a reattach defect, collision response, intended negative scenario, or even that reattach is available in Player: observations are manually operator-entered.

## Execute (private directory outside any Git worktree)

Node 20+ required; use an actual local EXE for planned authorized technical preflight. Example:
    node Research/B01-13/P17J/trial_matrix.mjs prepare --player /private/Game.exe --output /private/plan.json --sessions 8 --seed 3

Creates 32 cards, each with id, position, condition and rig. To score, provide a private JSON array of exactly one observation per card. Example single array element: {"id":"S01-T1","outcome":"NOT_RUN","feedback_visible":null,"state_changed":null}. Supported outcomes: OBSERVED, PRODUCT_BLOCKED, SETUP_BLOCKED, NOT_RUN. Attempted trials require two booleans; unrun requires two nulls. No names, notes or other unrecognized fields allowed.
    node Research/B01-13/P17J/trial_matrix.mjs score --plan /private/plan.json --observations /private/obs.json --output /private/summary.json
    node --test Research/B01-13/P17J/test_trial_matrix.mjs
    python3 Research/B01-13/P17J/verify_package.py

Outputs use O_EXCL and fsync and refuse paths under a .git ancestor. POSIX permissions requested 0600. Windows NTFS ACL/junction protections NOT ATTESTED. Never publish actual human data, screenshots, EXE or private observation logs to GitHub.

## Truth and gates
Plan pins exactly #510 source candidate SHA, but actual EXE SHA256 is only that ONE file, not the complete Unity ZIP or verification of source-to-build provenance. The current candidate #510 is NOT integrated. The output hard-codes player_to_source UNVERIFIED, unity_g1 NOT_RUN, human_trials NOT_RUN, human_verified 0 and integrated false. This tool cannot certify human readiness, Unity native PhysX, G1, G2 or a Codex ACK. Missing-ID and transaction rollback native tests remain B01-40/60/61/62 ownership. Human testing requires separate HQ GATE D authorization, consent, validated actual native Player and separate privacy/retention handling. The program has no human mode. Source tests use synthetic EXE bytes and synthetic observations, not participants or gameplay.

## Versioned scope and rollback
Only Research/B01-13/P17J and .github/workflows/b01-13-p17w12-trial-matrix.yml; no Assets, canonical game or frozen #495/#516 changes. manifest.json pins exact original file Git blob SHA1 and physical bytes; verifier rechecks blobs and prints real SHA256 for receiver/CI.
Receivers: B01-40 independent scope/manifest G0; B01-60 independent source tests; B01-00 adoption decision. No merge, no offer to Codex, no ACK.
Rollback: leave Draft unmerged and delete isolated branch if rejected; separately delete private operator reports according to their retention/withdrawal procedure.
