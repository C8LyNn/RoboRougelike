# MAPGENWORLD001 — W12 reproducible evidence
**Agent B01-21, standalone v1.0.0, G2 HOLD.** Requires full MAPGEN R3.4.1 exact `5be31039411f89f17654b1c332f8692883fa03cc` (#237); B01-10 policy source PR #240 SHA `93f290acca26839792a5bec8ea42415ac83932f3`.

## Actually executed (evidence is from PREVIOUS code commit; newest workflow should be checked too)
- Adapter implementation+synthetic tests commit `05bfa98e720d1b44a5fae1f017866ccf89dd33bf`: [push Actions run 40222055975](https://github.com/C8LyNn/RoboRougelike/actions/runs/40222055975) **SUCCESS**, [PR Actions 40222058957](https://github.com/C8LyNn/RoboRougelike/actions/runs/40222058957) **SUCCESS**.
- Actual `MapWorldPolicy.cs` + immutable producer MapGenData/Planner/Validator compiled and executed under explicit synthetic Unity vector math: **31/31 targeted cases PASS** (12 seed7/31/83 recipes×measured Light2.980m/Heavy5.180m; seven Heavy historical; five measured Light10m rejects; seven input/seed negative or repeat controls).
- **Actual `MapGenWorldAdapter.cs` C# compiled**, driven by explicitly fake MonoBehaviour/GameObject/**fake MapGenBuilder** (not actual Unity scene, not native producer renderer): **8/8 PASS**, including successful generation, atomic input rejection, event order, idempotent clear, broken listener isolation, defensive snapshot array copy, caught builder failure and corrupted post-build result forced teardown.
- Python package tamper+determinism **8/8 PASS**, includes real source ZIP verify, byte-for-byte repeated build, missing `.meta`, duplicate GUID, tampered file SHA and ZIP path traversal rejection. Real inner ZIP **67 files in embedded SHA256 manifest, 14 unique Unity GUID**, **139,299 bytes SHA256 `c15a605e15dfa028381efb18704ec775d164f4a4892ccde65fd93560813c2635`** for the above PREVIOUS commit; build and verify scripts print consistent hash.
- [Real earlier artifact #12610065853](https://github.com/C8LyNn/RoboRougelike/actions/runs/40222055975/artifacts/12610065853), **107,735 bytes** outer GitHub wrapper ZIP, GitHub-reported outer SHA256 `28838a6e03b0e5c5d58c8db238e6cde5e1ad99068b75dc12c3055ccf9df084f5`. Outer archive is **not** inner deterministic ZIP. Newer source commits require NEW inner SHA/size, not this previous value.

## Explicitly unexecuted gate
- Unity 6000.3.14f1 native compile and NUnit EditMode/PlayMode: **NOT_RUN**; Unity test methods are authored only.
- Windows URP Player render/screenshots, actual MapGenBuilder lifecycle, genuine PhysX/R6 measured-rig driving: **NOT_RUN**.
- B01-60 independent G1: **PENDING**; B01-40 independent G0_TARGET/current Codex SHA collision & parent meta preservation: **PENDING**; B01-00 G2: **HOLD**. Not READY_FOR_CODEX, not merged or installed into active game.

## Subsequent controlled self-update
This evidence note is part of the deterministic package, so inserting the receipt changes inner ZIP bytes and SHA. Obtain **current final ZIP hash and HEAD** exclusively from newest successful GitHub Actions job or its `SHA256SUMS.txt` artifact; do not reuse the historical inner hash listed above.
