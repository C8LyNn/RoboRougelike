# B01-22 — EK-R3-COMPAT-001 / PASS17 WAVE04

Purpose: repair B01-40 source compatibility HOLD on immutable author #333 WITHOUT changing author code or Codex-owned Assets.
Original frozen kit author PR #333 SHA: 0d212a706c1e633b773d2e03b7b40c28bb63521b (13 source payloads, all SHA256 validated).
Original Codex PR #310 source SHA: aa7a917f177a9b1f07203868fb7d1d871eb3ee46.
Current Codex PR #383 GAMEPLAY source SHA: 203a0805251ed28293888e43f2d49f0df63767c6.

R3 old blob: 8cc68faf89793c3a803e5807594c9faf0df10091
R3 current blob: 1d0f378dc598e08b3d7c16975f419d84231511bb
R4 old/current blob: 502f6d7f70d4e8078260de632ab181f244a4d730

Exact and ONLY allowed R3 change: public sealed class R3VehicleModel becomes public sealed partial class R3VehicleModel.
The gate requires exact original/current blob identity, exact source text substitution, same R4 blob, complete real host enum/price comparisons, full original frozen manifest SHA256, and 10 original kit prices. Any unrelated R3 change, new code, new part price, enum, invalid kind, >2 gun families or new future Codex SHA remains FAIL_CLOSED. Original producer verifier remains unchanged and correctly strict.

Reproduction commands after git fetch of three exact SHAs from .github/workflows/b01-22-w04-r3-compat.yml:
  python -m unittest discover -s Tools/B01/B01-22/EK-R3-COMPAT-001 -p test_compat_gate.py -v
  python Tools/B01/B01-22/EK-R3-COMPAT-001/compat_gate.py --repo . --receipt /path/to/NEW_COMPAT_RECEIPT.json

The receipt is written only with exclusive create to a new file (never overwrite) and lists 10 actual source-compatible offers, both exact R3 blobs, R4 and frozen original manifest. Rollback: delete only this new receipt; no author #333, Unity, Garage, Run, Wallet or inventory file is touched.

NEXT: B01-40 independently confirms exact-source G0 and checks if HOLD_R3_BLOB_DRIFT can become SOURCE_COMPATIBLE_FOR_QA; B01-60 independently reruns original and mutant tests; B01-00 alone schedules the verified frozen content into CODEX INBOX #79 after G1 and design approval. This gate does NOT assert full game runtime compatibility, Unity native PASS, CODEX_ACK, G2 or integrated content. No main/develop merge.
