# Read-only candidate and eventual Codex-owned adapter — UXVIEWPORT-001/R1

## First run with no game ownership transfer
```sh
git fetch origin research/b01-11-p17-w10-uxviewport-001
git worktree add --detach /tmp/b01-11-uxviewport origin/research/b01-11-p17-w10-uxviewport-001
dotnet run --project /tmp/b01-11-uxviewport/Research/B01-11/P17/W10-UXVIEWPORT-001/Tests/HudViewportTests.csproj -c Release
```
Do not copy/modify any canonical `Assets/**`, R1 HUD, `#275` camera/aim/garage, CHL/PARTSIZE/MODEL_JOB, HUD gameplay logic or main/develop based on this producer package. Independent G0/QA first; B01-00 decides #79 source offer; Codex owns any adaptation **only after explicit ACK**.

## Source adaptation contract (future, Codex only)
`HudViewport` expects actual **screen pixel coordinates**, top-left origin, safe margins `left,top,right,bottom` and optional font scaling 100–150%. Output `HudLayout` has immutable `HudRect` pixel geometry for `Critical`, `Vitals`, `Threats`, `Objectives`, `Tips`, `Pause`, `FontPixels`, `TwoColumn`. Zero-size optional rectangles mean hidden. **Never** use zero-size `Critical`, `Vitals`, `Threats`, `Pause`; on a too-small viewport the algorithm throws rather than overwrites an essential panel.

The existing Unity `OnGUI` path may use a 1280×720 scaled `GUI.matrix`. If Codex chooses this adaptive screen-pixel geometry, it must avoid double scaling by supplying a matching GUI coordinate frame or transforming rectangles through the inverse matrix. For `Screen.safeArea`, account for Unity bottom-left origin before passing top inset into `HudViewport`. Mapping source data (e.g. player core HP, enemy list, tactical alert from UXFEED #438, pause action) remains **owned by Codex**; this module never updates any game state. Actual Text wrapping/auto height, colors and font assets require native tests; no guaranteed Unity render.

## Native acceptance and rollback
Codex-hosted QA must verify safe area, no clipping/overlap, 100/125/150% text, critical always visible and accessible, 320×568 (small portrait), 768×1024, 1024×768, 1366×768, 1080p, 4K, mouse/keyboard/controller action focus, no interference with aim reticle, pause and weapon HUD, text wrapping, Player build at native Unity 6000.3.14f1. Producer source CI does NOT discharge these gates.

Rollback now: simply ignore/remove isolated `Research/B01-11/P17/W10-UXVIEWPORT-001/` and `.github/workflows/b01-11-uxviewport-w10.yml`, or delete this isolated branch. If an adapter is approved later, only Codex rolls back canonical Unity implementation. No canonical game files or .meta are modified here.
