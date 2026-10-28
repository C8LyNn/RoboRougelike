# PASS16 — Physical ramming for unarmed enemies

Base `561c223f7f01c1e609548bf1c8c66b5132ec5485`. On a real Unity R6 range vehicle with its weapon destroyed and active wheel traction, the pre-change enemy retreated from 7.85m to 18.04m and did not contact the player, because it maintained gun standoff and `CanMove` treated the player as an obstacle (RED, exit 2).

When no weapon is operational, the existing enemy drive now seeks the player and allows its physical collider as a navigation endpoint; it still avoids world geometry and other enemies, uses existing R6 wheel forces and does not add an artificial rigidbody solver or collision-damage economy. Normal armed movement is unchanged. Physical ramming means real contact/push; no new collision-damage rule is asserted.

Unity 6000.3.14f1 Windows: final targeted suite **12/12 PASS**, exit 0. Actual unarmed R6 enemy closed 7.84m→3.06m and registered real collider contact, four wheels grounded. A destroyed-wheel case produced no artificial drive; an opaque solid wall remained impassable. Existing enemy grounding/encounter tests pass. XML and exit codes in Evidence; full logs local `Artifacts/Local/P16Ram`. These are scripted Unity tests, not human gameplay; dynamic friendly avoidance, Player validation and independent B01-24/B01-60 acceptance NOT_RUN. No G1/G2 or merge.
