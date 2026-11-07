# UI #943 — weapon status pagination

Game SHA `76ef50333011b23b1803410a3a62bd0cd06eb1f4`, based on #945. Existing HUD only drew two operational guns despite builds allowing 12. It now displays two per page and advertises Tab when more exist; the selected page is clamped if guns are lost. This does not change fire groups, weapon mechanics or camera.

Unity 6000.3.14f1 EditMode: status paging, HUD geometry, garage geometry and weapon-type limits 36/36 PASS, exit 0; page accessibility tests cover 0, 1, 2, 4 and 12 guns. Pre-change helper test RED is not a visual or interaction test. Windows x64 retail/QA builds succeeded, 0 errors, 6 warnings. Actual GUI Tab operation, multi-gun screenshot, human review and full PlayMode on this SHA NOT RUN. No merge.
