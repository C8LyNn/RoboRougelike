# W27 Native G1 checklist / B01-60

Package source owner B01-50, dependency source SHA b38216d94fb0f87af0c1cbc47090c7cb4bf7177e (PR #779). Start only after B01-40 G0 and owner's explicit approval. The supplied .unity scene **has not been imported in a real Unity Editor by this producer**.

- [ ] New disposable Unity 6000.3.14f1 URP Windows scratch; no live project, no reuse of Codex worktree.
- [ ] Strict exact original R3 Core+Unity .cs/.meta and R3 Runtime asmdef imported under scratch; original W27 Runtime/Scene/PlayMode source imported with 8 new GUIDs; NO duplicate R1/R2/R3 public script symbols.
- [ ] Editor.log import warnings/errors checked; script attached in actual scene; runOnPlay false.
- [ ] Press Play then explicitly launch native benchmark via component ContextMenu.
- [ ] 10/100/500, STATIC_REST/DYNAMIC_CONTACT, ABBA repeated 2 each, minimum 120 sampled frames; all witness contact counts >0, no Sleep active control, no undisposed old roots.
- [ ] Save original CSV in scratch temp; hash SHA-256 and save full exact path in restricted QA evidence; do not upload private PC paths to GitHub.
- [ ] Run analyze_native_csv.py on actual local original file. "structurally valid" is not G1; all missing Profiler markers must display NOT_MEASURED. Compare performance when **same platform, engine and marker availability**.
- [ ] Run two actual Unity PlayMode NUnit Test Framework tests and retain genuine XML/exit code + Editor.log + screenshot(s) if visual defects.
- [ ] Mark any native import failure, inconclusive contacts, active Sleep, missing/changed SHA or crash as FAIL/BLOCKED; separate native compile vs runtime. No unsubstantiated FPS gain.
- [ ] Return original verified source SHA, G0 receiver, G1 experiment result to #800; only B01-00 may authorize later #79 handoff and only Codex integrates with ACK.

Safe rollback: inventory exact new scratch files; remove only identified B01-50 copied entries after source/scene ref audit. Never recursive-delete the entire Assets root, pre-existing folders, symlinks, saves or user's original game project.
