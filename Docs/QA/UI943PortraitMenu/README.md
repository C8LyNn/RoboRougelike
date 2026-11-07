# UI #943 — portrait vehicle-selection menu

Source `ad7fc84c295b79ed0155589044b60f869b3750f1`, based on #947. The earlier actual Player at 768×1024 showed the whole landscape UI at 60% scale and tiny text. The Ready screen now uses a portrait layout with vertical vehicle cards and preserves the seed, load, garage, map editor and quit actions. Landscape and other screens stay on their earlier layout.

Unity 6000.3.14f1 targeted EditMode 45/45 PASS exit 0: portrait/landscape geometry and pause, garage catalog and map editor regression. Windows x64 retail and QA builds succeeded, 0 errors, 6 warnings. Actual Player screenshot and human input for this specific changed menu NOT RUN; geometry alone does not prove readability. Portrait garage and other screens remain small. Full PlayMode on this SHA NOT RUN. No merge.
