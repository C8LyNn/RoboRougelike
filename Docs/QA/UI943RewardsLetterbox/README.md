# UI #943 — rewards letterbox

Source eeab3db4008ff0f10e21c3ff945631253bd82676, PR #955 on #954; Unity 6000.3.14f1 targeted EditMode 35/35 PASS, exit 0. Before images use real Player at 86ec, real damaged shotgun and API-driven ledger; 1024x768 shows ground/vehicle through the bottom margin. Other requested resolutions are recorded. Non-768 results have a diagnostic CAPTURED_WRONG_RESOLUTION flag because fixture hardcodes 768x1024, but JSON/PNG dimensions equal their requested sizes. Automated exits C000041D/C0000005, cause unconfirmed. After-fix screenshot pending; manual clicks and full PlayMode NOT RUN. Sanitized logs/XML and original hashes provided.

Actual Player eeab at 1024x768: after-rewards and after-inventory images confirm both bottom margins are opaque, while cards remain visible. Fixture used API damage and reflection; actual mouse input NOT RUN. Automated exit returned C000041D after capturing, cause unconfirmed.
