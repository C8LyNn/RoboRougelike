# MAP-SYSTEM R6 — execution evidence (local/offline)

## Scope

R6 extends R5 with turn-aware graph searches, sampled junction curves, correction of the `lower_road` reference junction, obstacle probes along a proposed turning trajectory, and passive Rigidbody drive evidence recording.

## Results

| Verification | Result |
|---|---|
| R3 Python logic tests | 8/8 PASS |
| R5 Python logic tests | 12/12 PASS |
| R6 Python junction math tests | 5/5 PASS |
| R6 Python route/path tests | 9/9 PASS |
| **Python unit tests total** | **34/34 PASS** |
| R3 topology compatibility / R5 geometry / closed mesh topology | PASS offline |
| Unity asset structure / .meta GUID / asmdef JSON | PASS static |
| Reference turn audit | 5 sampled candidates; 0 geometry needs redesign; 1 deliberately invalid connection |
| C# source files included | 31 |
| New Unity `[Test]`/`[UnityTest]` declarations | 15 — NOT RUN |
| **Unity C# compilation / EditMode / PlayMode / Windows build** | **NOT RUN** |
| **Real driving of Rigidbody/production vehicle** | **NOT RUN** |

All commands, actual stdout/stderr and exit codes are in `R6_QA_OFFLINE_LOG.txt`.

## Concrete issue resolved in reference metadata

- Before fix: `junction_lower -> lower_exit` sampled min radius approximately **3.84 m**, smaller than example vehicle minimum **4.5 m**.
- Fix: `lower_road.padWidth` **12 -> 18 m**; sample map version **5 -> 6**.
- After fix: sampled min radius approximately **5.86 m**. This passes the reference vehicle's **geometric radius filter**; it is **not proof of safe physics driving**.
- Existing 7 nodes, 8 edges, 3 map states and 4 sandbox challenges remain.

## Risk / next required evidence

1. R6 junction visuals are only `Gizmos`. Physically continuous collider mesh at intersections is NOT built; R5 node pads remain and their seams can cause wheel impacts.
2. G1b Physics checks are only implemented; Unity Physics tests have NOT run.
3. Path finding with turn constraints is graph-based, and does not model reversing, drift, traction, asymmetric mass or contact behavior.
4. Passive recorder records motion and suspected sticking, but never autonomously proves the start-to-finish traversal.
5. Independent real Unity evidence is needed before integrating with AI or production Gauntlet.

**QA disposition: READY FOR ISOLATED UNITY COMPILATION AND DRIVING QA; NOT READY FOR PRODUCTION MERGE.**
