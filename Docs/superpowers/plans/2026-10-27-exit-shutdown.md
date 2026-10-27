# Exit and shutdown implementation plan

> **For agentic workers:** Use superpowers:executing-plans to execute these tasks and report actual results.

1. Record the PR #272 baseline and audit application quit hooks and input lifetimes. Reproduce where possible without forcing process termination.
2. Add native EditMode tests for garage pause/resume, combat pause, menu return and activity cleanup; run them against the baseline.
3. Extend the existing controller and HUD with pause in gameplay and garage, menu return and idempotent Unity quit. Preserve the garage's stopped simulation and reject gameplay input while paused. Run focused native tests and existing regression tests.
4. Build the exact frozen source for Windows x64; validate the extracted ZIP, actual player startup and shutdown modes with real process receipts. Record GUI tests as NOT_RUN if unavailable.
5. Run the available final native suites, report evidence and limitations, push a separate branch and create a draft PR based on PR #272. Keep the original integration branch intact.
