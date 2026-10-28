# CX-06 — Critical garage readiness warnings

Base `f0047085123389487c597cd49426321182916673`. The garage now shows both missing operational weapon and missing selected drive together, independently of optional suggestions. The diagnostic counts live components physically connected to the live core, including the selected drive's existing side requirements. Editing and existing activity-launch rules are unchanged. Other deploy errors remain visible in the existing status area.

Windows Unity 6000.3.14f1: new required-warning tests baseline 0/3 RED, exit 2; final targeted EditMode regression 33/33 PASS, zero skipped, exit 0, including both absent, damaged, disconnected gun behind damaged bridge, healthy vehicle, garage editing and weapon groups. Raw XML/exit in Evidence, full logs local `Artifacts/Local/CX06/`. Native GUI layout/readability and Windows Player NOT_RUN. Independent B01 review / G1 remain NOT_RUN; no main/develop merge.
