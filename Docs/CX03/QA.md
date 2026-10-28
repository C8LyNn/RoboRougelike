# CX-03 — Invalid weapon transaction integrity

Base: `231005c6d0351945a8ee6684f3856af118f0e94f` (PR #310). This change rejects unknown gun kinds in a complete incoming salvage batch and ledger, checks the type and durability of an owned part before repair/dismantling, computes dismantle value before removal, and rejects an invalid emergency repair plan before mutation. An invalid price/repair/dismantle quote is zero; valid game economy calculations are unchanged.

Unity 6000.3.14f1, Windows: original added regression tests RED: 3 failed, 1 passed, Unity exit 2. After source fix the targeted Unity EditMode regression suite passed 42/42, zero skipped, Unity exit 0 (includes invalid salvage and legacy inventory, healthy repair/dismantle, insufficient funds, garage, rewards and fire groups). Raw NUnit XML and exit codes are in Evidence; original Editor logs are `Artifacts/Local/CX03/{red,regression}.log` in this worktree.

B01-22 independent strict original-source contracts, B01-62 review and B01-60 independent Unity G1 remain NOT_RUN by Codex; this is a handoff, not independent acceptance. Windows Player interactive GUI and PlayMode NOT_RUN for this isolated patch. No main/develop merge.
