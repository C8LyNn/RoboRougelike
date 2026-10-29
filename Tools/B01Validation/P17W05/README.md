# B01-40 WAVE 05 — independent v1.0.3 equipment source intake

**PACKAGE_ID:** `B01-40-P17W05-NEW-KIT-V103-RECEIVER-001`. This is a *NEW-source-version acceptance* of [B01-22 Draft #411](https://github.com/C8LyNn/RoboRougelike/pull/411) @ `a3dd45a4c1f222ec5cc387302eafe9104f91ab52`, not a duplicate of the B01-40 #408 *old v1.0.2 drift-only* analysis. Retains existing producer source and all canonical Codex-owned Unity `Assets/**` untouched, no merge.

## Source lineage and exact fixed target

- Old frozen [#333](https://github.com/C8LyNn/RoboRougelike/pull/333) `B01-22-EQUIPMENT-KITS-001 v1.0.2` @ `0d212a706c1e633b773d2e03b7b40c28bb63521b` remains immutable and its original SHA gate still correctly rejects latest Codex.
- One direct child commit author-published new [#411](https://github.com/C8LyNn/RoboRougelike/pull/411) `v1.0.3`. New original catalog Git blob `372b3bc74553ea459e89be38378f4429090e00a2`; manifest Git blob `bd4b759522ca1ebd385abf90680ef9f885ca3bb3`. Exactly seven new changed files are within owner `Research/B01-22/P17/EQUIPMENT-KITS-001/**` plus own workflow. No game/QA file conflict.
- New source catalog pins **current Codex #383 gameplay** `203a0805251ed28293888e43f2d49f0df63767c6`, R3 blob `1d0f378dc598e08b3d7c16975f419d84231511bb`, unchanged R4 `502f6d7f70d4e8078260de632ab181f244a4d730`. B01-40 #408 independently established only `partial ` keyword addition from prior R3.
- **All 10 EK01–EK10 kit part/weapon definitions, all 17 part prices, all 9 weapon prices and every other non-version/provenance catalog field are identical** to #333 after parsing JSON. Unchanged original strict gate, `kit_core`, old tests, CLI, and negative source mutation checks are independently pinned byte-for-byte.

## What code actually executes

`receive_kits_v103.py` independently reads original Git blob bytes, validates *each 13 original manifest SHA256 + size*, parent SHA and changed-file scope, sources against Codex current gameplay+report. It opens an original detached #411 source worktree and really executes original `node --test` (26 tests), original Python stage/rollback (18 tests), source CLI `--verify-only`, **unchanged** strict source gate against **current Codex** with 7 original bad mutations, and deliberately demands that same gate **reject old Codex pin** as an additional anti-bypass check.

Using the unmodified producer staging tool it physically creates `catalog.json`, **10 actual stock offers** in `offers.json` and SHA-bound `receipt.json` in an *external disposable* directory. Independently recomputes every offer price and item count from the unchanged original catalog and checks genuine original staged bytes, then invokes original safe rollback and validates no lingering staged directory. It exports a deterministic **four-member QA-only** ZIP with those real original staged files + B01-40 source-verification receipt, and a separate `qa-source-receipt.json` with SHA256 of each ZIP member.

The **20 additional B01-40 tests** rerun actual source Git blobs, prices, 10 offers, previous-vs-new source lineages, exact physical ZIP hash, negative source/ancestor/price drift safety and no overwrite of canonical game. GitHub Actions executes both original and independent tests on **Windows 2025 and Ubuntu 24.04** and uploads physical receipts. This tool never writes to Unity `Assets/**`, nor installs any shop in actual game.

## Reproduce

From a B01-40 isolated Git checkout with Git, Node 22 and Python 3.11+:

```bash
git fetch --no-tags --depth=2 origin a3dd45a4c1f222ec5cc387302eafe9104f91ab52
git fetch --no-tags --depth=1 origin 0d212a706c1e633b773d2e03b7b40c28bb63521b
git fetch --no-tags --depth=1 origin aa7a917f177a9b1f07203868fb7d1d871eb3ee46
git fetch --no-tags --depth=1 origin 203a0805251ed28293888e43f2d49f0df63767c6
git fetch --no-tags --depth=1 origin f92c873bb1e397a598b133e107ae704e15f86768
python Tools/B01Validation/P17W05/receive_kits_v103.py --repo . --out /tmp/NEW-external-b0140-w05
B0140_W05_OUTPUT=/tmp/NEW-external-b0140-w05 python Tools/B01Validation/P17W05/test_receive_kits_v103.py -v
```

The output path must not exist beforehand and must be outside the Git checkout. On Windows use an external temp directory and set `B0140_W05_OUTPUT` accordingly. **Rollback:** the producer's original stage/rollback removes only its temporary staged offers; the receiver outputs are disposable QA files, remove those manually only after inspecting their SHA. Any optional *game-side* new EK01–EK10 stock adapter/registration is exclusively Codex-owned; Codex must independently test wallet/inventory transactions, save/load, UI, physical/spawn consistency in Unity and revert only its own adapter if it fails.

## What the gates do and do not certify

**B01-40 source G0 is independent and can PASS after real exact-HEAD CI.** Separate B01-60 independent QA on **new #411** is required; previous #408 acceptance of older v1.0.2 is not sufficient. **B01-00 controls CODEX INBOX #79** and request of exact SHA Codex ACK; this receiver does NOT post there or call it integrated. `Unity native Editor/PlayMode/Player NOT_RUN`, `HQ G2 HOLD`, `CODEX_ACK PENDING`, `GAME_INTEGRATED NO`, no main/develop merges. Catalog prices remain reference values, do not authorize spending scrap or mutating owned gear until Codex validates current transactional logic.
