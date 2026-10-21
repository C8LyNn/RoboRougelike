# OBJ-001 R1 — verification record (2026-10-22)

- develop HEAD at inspection: `5f06af81e2dd9d877e4300259bb3dd008cb9c9d1`.
- Feature HEAD before R1: `d0946e0cb8872d60ecf9bf9f707f6d939f927aba`.
- Open PR reviewed: #1 Codex, #2, #3 MAP, #4 COM, #6 VFX, #12–#16. Issue #5 ownership reviewed. PR #14 DRAFT → develop.
- Changed paths limited to OBJ-001's two reserved directories. Existing gameplay/scene unchanged.
- Unity EditMode: **NOT RUN** (11 authored).
- Unity PlayMode: **NOT RUN** (3 authored).
- Unity Windows Build: **NOT RUN**.
- Manual Unity demo: **NOT RUN**.
- GitHub CI: inspect new commit Actions after push; no PASS claimed without observed run.
- Static assembly definition JSON: generated with explicit runtime/editor/test references; Unity compilation remains unverified.

Next: execute Unity 6000.3.14f1 EditMode/PlayMode and demo in isolated worktree, collect XML/logs, fix only OBJ-001 defects, then review an optional real-enemy adapter. No automatic merge.
