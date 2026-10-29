# TEST_PLAN

**Producer executed**: in-process V8 invocation of the **same exported functions serialized into `kit_core.mjs`**. 26 test cases including 10 actual kit cost quotes, exact capacity boundary, reject unknown parts/weapons/third family/price drift/duplicated IDs/invalid wallet. JSON receipts in TEST_EVIDENCE.

**Node original files:** `node --test kit_core.test.mjs`, CLI validate/list/quote; Node checkout tests NOT_RUN in producer V8, CI workflow provided for cross-OS execution. For EK08 @wallet240 & inventory197 expect price235, items5, remaining preview5, capacity_ok=false, eligible=false.

**Codex/Unity**: test price with host `model.Price`, check 200 capacity/atomic GUID on actual purchase, inspect garage stock UX, native EditMode and PlayMode and player build ONLY after acceptance. These are NOT_RUN.

## Original host source gate v1.0.1

Po odczycie dokładnego SHA oryginalnego PR #310 i current ref: `python verify_host_source.py --current-ref origin/b01-codex-pr310 --selftest`. Cennik odczytywany z oryginalnego kodu R3/R4, Git blob weryfikowany bezpośrednio. 7 mutacji ma zostać odrzuconych. To niezależny statyczny kontrakt cenowy, **NIE** kompilacja ani native G1 Unity.

## WAVE02 / v1.0.2 safe staged data QA

`python -m unittest discover -s Research/B01-22/P17/EQUIPMENT-KITS-001 -p test_stage_kit_payload.py -v` runs 18 tests on physical package files: positive 10-item staging, EK08 real quote, verified rollback; negatives hash/size tamper, missing file, path traversal, duplicate entry, malicious symlink, unknown kind, >2 gun kinds, preexisting target, modified receipt payload, unexpected extra data, bad revision. Same tests on Windows 2025 and Ubuntu 24.04 in own workflow. Runtime Unity native NOT_RUN; independent B01-60 pending.
