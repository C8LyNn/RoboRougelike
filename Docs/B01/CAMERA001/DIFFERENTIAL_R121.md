# B01 CAMERA-001 R1.2.1 DIAGNOSTIC — source-only differential, NO gameplay rewrite

## Trigger and classification
Original producer R1.2 frozen [PR #247](https://github.com/C8LyNn/RoboRougelike/pull/247) exact `48581e49a878eca376674551017532bf17fd35b2`.
External licensed Unity Windows [Codex QA #249](https://github.com/C8LyNn/RoboRougelike/pull/249) head `08037f0b831381dfda111edc6b9810a1adfd2ffa`:
compile PASS, original Edit5/5, Play13/13, CAM-R11-01/CAM-R11-02 originally failing cases now **PASS_OBSERVED**. Native sampled CAMERA pose and collision 7,664/7,664 safe, post-weapon LateUpdate rays 3,360 safe.
**Open FAIL CAM-R12-INT-01**: real Light W+LMB gun tracking peaks 15.306° at720p and 13.104° at1080p, while target ray first hit transitions from enemy component to floor; actual turret rotates at170°/s. Not proven a CAMERA regression. Other 6/8 full Windows cases PASS.

## Concrete differential from native QA example — not a native re-run
Native receiver observed approx old target `(.00391,1.914,30.8045)`, new floor point `(.00269,0,34.8804)` and current muzzle `(.00401,1.5552,27.8219)`. Unit-normalized directions from **the same muzzle origin** change by approximately **19.285°**. If the gun has one 1/60s frame and slew ≤170°/s, one local step allows only **2.833°**; closing a 19.285° stationary step would require at least **0.113s** at the stated rate, with no chassis motion/other constraints. This numerical discriminator supports a turret-slew *hypothesis*, **not a demonstrated cause**: exact native per-frame target/gun rotation, muzzle movement and matching BASE/R1.1 run are not yet supplied.

## New actual producer work
New runtime source `Assets/B01Packages/CAMERA001/Runtime/Camera001AimTransitionDiagnostics.cs`:
- Passive `Measure(Sample)` with previous/current world target points, current muzzle origin, previous/current world muzzle forward, dt, known slew-rate cap, actual camera final-pose error/self-ray/world overlap.
- Reports measured target direction jump, available one-frame slew, world muzzle rotation, current alignment, ideal catch-up lower bound and non-causal diagnostic classification:
  `CAMERA_FINAL_POSE_UNSAFE`, `TARGET_STEP_EXCEEDS_TURRET_BUDGET_PLAUSIBLE`, `UNRESOLVED_TRACKING_GAP_REQUIRES_PAIRED_NATIVE_QA`, `ALIGNED`.
- World muzzle rotation includes body motion: a budget flag is **not evidence** that the local pivot violates its limit. Lower-bound catch-up assumes fixed target; can't prove one particular gun implementation's behavior.
- Does not query scene, write `transform`, choose targets, rotate a gun, alter fire/input, change collision/render/aim or add a second camera. Existing R1.2 physics collision fixes, tests, driver and demo are byte-identical. Camera source is not activated by default.
- Adds six Unity EditMode NUnit regression cases (named, separated) and actual exact-source C# executable via .NET8 with **explicit fake Unity vector math**, not a licensed Unity build. Previous original 5 Edit+13 Play remain unchanged.

## Native QA strict A/B/C plan (Codex/B01-60 future scope, no auto-run)
Run **three matched disposable checkouts**, not active Codex game:
A) BASE/current approved R3 weapon/controller + P12 camera, no CAMERA package.
B) frozen CAMERA R1.1 source `d48108f56d3fe0790ad715683e77b2de9219e4b1`.
C) frozen CAMERA R1.2 source `48581e49a878eca376674551017532bf17fd35b2` plus optional passive diagnostic R1.2.1; no game edits from producer.

Same actual R3 vehicle Light, enemy Light at(0,1,30), 720p/1080p, flat collision floor, exactly the same 5s W+LMB input stream, shots recorded and renderer order, random/frame seeds, hit filtering and target selection. Record at PRE target query, weapon LateUpdate, POST fire and EndOfFrame: actual camera final pose and ray, first-hit *collider instance ID/type/owner* (not just names), target world point, muzzle world origin/forward, rigid-body orientation/angular velocity, effective locally applied max turret angular speed, elapsed dt; gun rotation before and after and actual projectile velocity. **Always distinguish frame target jump, gun error and CAMERA pose error**. Keep result raw CSV/JSONL/NUnit XML/Player exit and 720p/1080p frame screenshots.
- If BASE/R1.1 exhibit comparable sudden target-step and residual error with safe camera pose, route to **Codex host weapon/target acquisition owner**, not CAMERA.
- If only C shows target-step while older A/B are clean using matched input/geometry, investigate camera ray/target acquisition semantics on this producer SHA first; then minimal source-only correction with new native retest.
- If target direction remains stable and gun error stays >.05° beyond physically necessary slew duration, inspect weapon update order/servo; **do not secretly retune 170°/s or fake fixed muzzle orientation**.
- If R1.2 CAMERA own hull/world penetration/final aim error violates 0/.05 thresholds, reopen camera producer defect with exact frame reproduction.
- Existing 15m/s continuous camera constraint and one LateUpdate owner remain. R1.2 LMB observed firing; prior QA explicitly says this OBJ base had **no RMB group B binding**. No claim dual-family game acceptance.

## Gates
Code source is separate Draft on immutable #247; original #247 and Codex native #249 are not modified. Full stack #134+#214+#247+this diagnostic patch with original paired .meta. GitHub CI source tests and deterministic SHA256 ZIP are gate G0 producer only.
NATIVE_UNITY_EDITMODE new6=NOT_RUN until actual runner; existing R1.2 5+13 native PASS applies only prior SHA. B01-60 independent G1=PENDING, B01-40 target/import G0=PENDING, HQ G2=HOLD, no merge/Codex active worktree edits or BUILD ownership overlap.
