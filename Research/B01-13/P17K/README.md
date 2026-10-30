B01-13 SAVE-VISUAL-PAIRS-001/R1 - PASS17 / WAVE13

OWNER: B01-13 Human Playtesting.
DISPATCH: GitHub issue #535; governance #276. Selected source ONLY Secure Reattach V2 #510 exact d94400d95d13db7f27e1533df05c9292d5380431. Alternative #511/#494 HOLD.
This is a working offline technical-operator tool. It is NOT a Unity Save/Load implementation, gameplay integration, native Unity test, consented human study or proof that consumed part IDs survive reload.

FUNCTION:
Node.js 20+ offline CLI hashes physical bytes of an operator-supplied EXE, creates a BLANK two-snapshot visual module continuity form and compares manually transcribed BEFORE_SAVE and AFTER_LOAD states. Three scenarios: ATTACHED_ONLY, DETACHED_HELD, REATTACHED_BEFORE_SAVE. Each observed module has nonidentifying visual_token V01..., state ATTACHED/DETACHED, and socket S01... for ATTACHED or null for DETACHED. It detects missing/extra modules, state transitions and unexpected socket changes. It rejects duplicate tokens/sockets, inconsistent scenarios, PII, free text, invalid stage order and blank forms. It does NOT inspect actual internal IDs, load game saves, claim native PhysX, or compare world-space poses of detached bodies (which may drift physically).

DISTINCT FROM #531: previous Wave 12 tool generates a balanced Light/Heavy x clear/overlap trial order with feedback/safety fields; this tool performs paired observation continuity comparison across SAVE / LOAD, not reattach matrix scheduling.

PRIVATE OPERATOR COMMANDS (use absolute paths outside every Git repo):
node Research/B01-13/P17K/save_visual_pairs.mjs template --player /private/Game.exe --scenario DETACHED_HELD --session T0001 --output /private/template.json
Fill both snapshots manually with observed module lists, set unfilled=false only after technical observations. This does not prove that observations were genuinely captured.
node Research/B01-13/P17K/save_visual_pairs.mjs evaluate --input /private/filled.json --output /private/report.json
node --test Research/B01-13/P17K/test_save_visual_pairs.mjs
python3 Research/B01-13/P17K/verify_package.py

SECURITY:
Private output is created exclusively (O_EXCL), synced, refuses Git ancestors, asks for mode 0600 where supported; source input size is capped at 128KiB and binary SHA256 hashes EXE only, NOT complete Unity build or ZIP. Windows junction/ACL guarantees NOT_ATTESTED. No personal names, note fields, free-text, images or network uploads. Test inputs are entirely synthetic fixtures. Never commit real participant observations. Human study requires separate HQ GATE D, consent, authorized native Player and actual source-to-build provenance.

TRUTH:
The result hardcodes human_verified=0, native_unity=NOT_RUN, save_id_contract_verified=false, evidence=OPERATOR_TRANSCRIBED_ONLY. Even NO_OBSERVED_VISUAL_DRIFT does not establish persistence of internal IDs or secure dock safety. Engineering ownership belongs to B01-23/62; independent rollback QA to B01-40/61; Unity G1 to B01-60. Previous #531 remains frozen and untouched.

OWNERSHIP / RECEIVER / ROLLBACK:
Own new paths Research/B01-13/P17K plus dedicated workflow; no canonical game/Assets/Codex/CHL/PARTSIZE/other-owner edits. B01-40 checks exact source manifest/scope, B01-60 independently reruns original tests, B01-00 handles operator adoption and potential later authorized human testing. Do not merge, do not claim Codex ACK. Rollback: abandon unmerged Draft and separately dispose private files per retention policy. Independent source G0 and real Unity G1 are distinct gates.
