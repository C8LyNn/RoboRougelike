# UI #943 — warnings and defense heading

Source `6a10686e4cf4b408729748c39c59fc88768025ed`, based on #946. Physical diagnostic Player screenshot at prior `4c258437` confirmed the OBRONA heading was vertically clipped; its actual capture and explicit API fixture are [here](../UI943Hud/Player-defense-grace-1280x720.png). The heading now has a taller label and its rows fit below; damage notices moved below the suspension panel and above the test-range footer.

Unity 6000.3.14f1 targeted EditMode 43/43 PASS exit 0, including five-resolution geometric checks, 1/4 defense modules, three notices, weapon pages, garage and weapon-type limits. Windows x64 retail and QA builds succeeded, 0 errors, 6 warnings. An after-fix screenshot of this exact condition and human gameplay are NOT RUN; the geometric checks do not establish actual font rendering. Full PlayMode on this SHA NOT RUN. No merge.
