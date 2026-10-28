# TEST_PLAN

**Producer executed**: in-process V8 invocation of the **same exported functions serialized into `kit_core.mjs`**. 26 test cases including 10 actual kit cost quotes, exact capacity boundary, reject unknown parts/weapons/third family/price drift/duplicated IDs/invalid wallet. JSON receipts in TEST_EVIDENCE.

**Node original files:** `node --test kit_core.test.mjs`, CLI validate/list/quote; Node checkout tests NOT_RUN in producer V8, CI workflow provided for cross-OS execution. For EK08 @wallet240 & inventory197 expect price235, items5, remaining preview5, capacity_ok=false, eligible=false.

**Codex/Unity**: test price with host `model.Price`, check 200 capacity/atomic GUID on actual purchase, inspect garage stock UX, native EditMode and PlayMode and player build ONLY after acceptance. These are NOT_RUN.

## Original host source gate v1.0.1

Po odczycie dokładnego SHA oryginalnego PR #310 i current ref: `python verify_host_source.py --current-ref origin/b01-codex-pr310 --selftest`. Cennik odczytywany z oryginalnego kodu R3/R4, Git blob weryfikowany bezpośrednio. 7 mutacji ma zostać odrzuconych. To niezależny statyczny kontrakt cenowy, **NIE** kompilacja ani native G1 Unity.
