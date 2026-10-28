# B01-13 / PASS17 — AIM-DELTA001 R1 (private screenshot comparison)

**A working tool, not a gameplay patch.** This package adds an **offline, image-backed before/after comparison** for *two already authenticated-by-bytes AIM-OBS001/R1 receipts*. It visualizes **eight actual image files** across `CLOSE_STATIC`, `FAR_STATIC`, `OCCLUDED` and `ZOOM_MOVING`, original R/T/I annotations, and operator-recorded outcome; calculates **after − before** annotated pixel difference. Scenario selector, marker visibility toggle, mobile layout and browser controls are functional. No outcome is inferred, no physics executed. This is distinct from #349's original image annotation + single-receipt verification.

## Scope and immutable dependency

- `PACKAGE_ID=AIM-DELTA001`, `VERSION=R1`, producer `B01-13 HUMAN PLAYTESTING`.
- **Owned/new paths:** `Research/B01-13/P17B/{aim_delta.py,test_aim_delta.py,QA_delta.json,README.md,manifest.json}` only.
- **Stacked base:** existing B01-13 **Draft #349** `research/b01-13-pass17-aim-obs001` pinned to `4ef06698e2ed6ada5e05b1a3e0a96b1c6aff5906`. New Draft PR is stacked **against that research branch**, not main; no modification to source of #349 or to `Assets/**`.
- `Research/B01-13/P17/verify_aim_receipt.py` original, unchanged Git blob `70f93467fef1d5782ec4e5cbd6a8a98af6fd0b00`, physical SHA256 `8e6cd46bc5f1229b54e565a1b74ea9387d27073d463f61268809f212a9e2ddf4` — imported from **that exact original byte sequence**. If the receiver changes, this package fails closed.
- No Unity `.meta`/prefabs or gameplay scripts. No human participant or approved study. Native Unity and real Windows Player screenshot collection **NOT_RUN** by B01-13.

## Actual operator usage — private machine only

Prerequisite: copy or checkout **both** this package and parent #349, install Python 3.11+ and Pillow. Do not store any real evidence in public GitHub.

```bash
python3 Research/B01-13/P17B/aim_delta.py \
  --receiver Research/B01-13/P17/verify_aim_receipt.py \
  --before /private/before/receipt.json --before-images /private/before \
  --before-game-sha <EXACT_40_HEX_BEFORE> --before-zip-sha256 <EXACT_64_HEX_BEFORE> \
  --after /private/after/receipt.json --after-images /private/after \
  --after-game-sha <EXACT_40_HEX_AFTER> --after-zip-sha256 <EXACT_64_HEX_AFTER> \
  --output /private/AIM_DELTA_001.html --confirm-private-embedded-images
```

Both original receipts must pass the original #349 real-image-byte/hash/metadata gate. Data modes must match (`SYNTHETIC` or `OPERATOR_SOLO` only); `HUMAN_APPROVED` is intentionally **blocked** to avoid embedding sensitive participant evidence into an easily distributed file. Require distinct session IDs, identical case/group/input assignments and same screenshot dimensions for each pair. Cross-resolution pixel comparisons fail closed. All differences are simply observed annotation deltas, **not a clinical-quality metric, winner selection, or proof of physics/hits**. Comparing pictures taken from different camera/vehicle/target states can be misleading even if pixel dimensions match.

Output HTML is newly written (overwrite forbidden) with local `0600` Unix permission, contains full original screenshot bytes as `data:` URLs, no external assets, restrictive CSP (`connect-src 'none'`), no filenames, personal notes, consent refs or arbitrary untrusted text from the receipts. The generated file **must remain on the private operator machine**, and previously exported copies require manual deletion under local data policy. Windows ACLs must be configured independently; Unix mode is not a Windows confidentiality claim. Removing the private output and input copies is the rollback; closing the unmerged Draft has no gameplay impact.

### Actual executable QA

```bash
python3 -m pip install pillow playwright
python3 Research/B01-13/P17B/test_aim_delta.py
```

Run inside a repository checkout containing the exact parent #349 and this stacked package; test requires `/usr/bin/chromium` on Linux. It physically creates **eight synthetic PNG images with varying bytes**, independently validates all via the unchanged **original #349** Pillow receiver, generates private HTML using `aim_delta.py` subprocess, and opens **original generated HTML in real Chromium**. Tests compare image dimensions, delta math, eight embedded original images, 3 responsive widths (320/720/1280), marker geometries, switchable scenarios, annotation visibility, CSP/no network and negative CLI outcomes (overwrite, no privacy confirmation, bad ZIP SHA, fake receiver SHA, human-approved mode, input group conflicts, dimension forgery, same-session misuse). All temporary screenshots/HTML generated under private test tempdir; published `QA_delta.json` is **synthetic automation evidence** only.

**Executed locally:** Python `py_compile PASS`; original Chromium + Pillow + CLI **41/41 PASS**, with a genuine 320px horizontal-overflow failure repaired and retested. **Native Unity NOT_RUN**; **Windows GUI Player NOT_RUN**; **0 humans**; game code not integrated. Independent B01-60 regression + B01-40 handoff and Codex ACK pending. No real screenshot is published in the PR.

## Codex receiver / import / rollback

- B01-40: verify stacked PR changes **only** `Research/B01-13/P17B/**`, same exact original base #349 SHA, physical source hashes and no Unity `.meta` required.
- B01-60: rerun exact source tests on new SHA and independently inspect private-output safety, owner-generated synthetic evidence versus actual game evidence.
- Codex (only after B01-00 intake): **no C# import**; an authorized Windows Player operator can collect actual screenshot evidence from a source-pinned game/build, use #349 annotate to produce two independent private image-byte receipts, then compare them privately using this script. **No automated claim of camera jitter or aim correction.**
- Rollback: do not merge Draft, remove the research-only code/report from the private operator checkout, and securely manage any externally distributed private generated HTML under approved policy.
