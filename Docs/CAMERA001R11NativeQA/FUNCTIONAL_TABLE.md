# Functional observations, actual Windows Player

| Rig/resolution | Parts | Hull W×H×L m | Actual own-ray hits | World clearance flags | Pivot-blocked samples | Damage parts | Move m | Physical shells observed | Muzzle error deg |
|---|---|---|---|---|---|---|---|---|---|
| Light-1280 | 20 | 2.980×1.564×2.720 | 5 | 8 | 14 | 20→19 | 17.559 | 11 | 0.0198 |
| Heavy-1280 | 139 | 5.180×2.114×4.920 | 4 | 6 | 10 | 139→138 | NOT_RUN | NOT_RUN | NOT_RUN |
| Tall-1280 | 34 | 2.980×9.160×2.720 | 0 | 8 | 14 | 34→33 | NOT_RUN | NOT_RUN | NOT_RUN |
| Wide-1280 | 149 | 10.420×2.114×4.920 | 4 | 4 | 8 | 149→148 | NOT_RUN | NOT_RUN | NOT_RUN |
| Light-1920 | 20 | 2.980×1.564×2.720 | 5 | 8 | 14 | 20→19 | 17.559 | 11 | 0.0198 |
| Heavy-1920 | 139 | 5.180×2.114×4.920 | 4 | 6 | 10 | 139→138 | NOT_RUN | NOT_RUN | NOT_RUN |
| Tall-1920 | 34 | 2.980×9.160×2.720 | 0 | 8 | 14 | 34→33 | NOT_RUN | NOT_RUN | NOT_RUN |
| Wide-1920 | 149 | 10.420×2.114×4.920 | 4 | 4 | 8 | 149→148 | NOT_RUN | NOT_RUN | NOT_RUN |

Own intersections occur only at framing→orbit pitch transition, not the subsequent constant-pitch yaw sweep. World clearance flags mean camera centre within0.1m of an axis-aligned wall AABB; not a full camera near-plane penetration test. Damage uses original HP/detachment. Physical shell identification uses recorded P17PhysicalShell/R4ProjectileOrigin components; muzzle error is not a measured ballistic trajectory error. Static Tall/Wide models are QA fixtures, not garage-validity certification.
