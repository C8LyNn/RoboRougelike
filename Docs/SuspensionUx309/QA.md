# MOV009 suspension UX — Issue #309

Base PR #308: `bd435d5157680ef4f5f50598218d48991dd3768a`. Unity 6000.3.14f1, Windows x64. Game, Windows build and native test source: `7299dcc5fd63ac042a0ffcd97b69d02fb8744649`. Later documentation commits do not change game assets. No changes to MOV009 physics, force constants, balancing, Unity version or swarm-owned packages; no main/develop merge.

The garage now labels the tab **Zawieszenie**, explains what supports the body, how to mount the physical part, the OFF/Assist/Full distinction and the requested height range. Placement errors remain visible. The HUD separates the selected mode from actual adapter state, operational module count and measured force. It reports missing/damaged modules, Hover suppression, pause and zero force without treating a requested mode as actual force. FullSupport fallback to Assist under competing wheel springs is disclosed. The existing pause panel disables unavailable combat-time configuration and explains that settings are changed in garage or test range. The existing practice track at x≈12 is indicated for comparing modes; no new track or physics was added.

Assist currently scales the existing support force to 25% and Full uses the existing full support calculation, both still subject to the original force budget. Height is a requested offset, limited to ±0.35 m and approached over time; it does not teleport the body. Existing Hover support suppression and competing WheelCollider fallback remain unchanged.

## Results

| Check | Result and scope |
| --- | --- |
| Unity compile and Windows x64 build | PASS, exit 0 on source above |
| Targeted EditMode | PASS 51/51, exit 0; includes UnityTests entering actual Play Mode with the real vehicle, garage and range |
| Targeted PlayMode | PASS 27/27, exit 0; 14 MOV009 physics tests and 13 camera world collision tests |
| Suspension UI state | PASS real Unity: missing, mounted/ready, OFF, Assist, Full, offset, Hover, damaged/removed support, actual positive force, pause and combat read-only state |
| Windows Player suspension test | PASS, batch/API-driven, exit 0. Three real supports and Hover engine: missing, ready, OFF, Assist, Full, +0.2 m, actual 1161.38 N sample, paused, Hover suppression and destroyed supports |
| Previous features | PASS targeted Unity garage freedom, two fire groups, reticles, pause and map editor; unpacked Windows Player garage/no gun/no wheels and map/range return tests both exit 0 |
| 720p / 1080p visual comparison, mouse / keyboard UI and player comfort | NOT RUN. User requested no PC/mouse control; batch/nographics is not evidence of rendered text, focus or manual gameplay |

The baseline RED test correctly failed because no shared presentation of actual suspension status existed. XML and process receipts are under `Evidence/`. Original Unity logs are in `Artifacts/Local/FM270-SUSP309-RC-20261027T210233Z/`; unpacked Player logs are in `Artifacts/Local/SUSP309-PLAYER/`, `SUSP309-PLAYER-garage/`, and `SUSP309-PLAYER-map/`. These Player tests use opt-in batch automation, not user-driven visual gameplay.

## Build and three-step manual check

[Windows x64 ZIP](https://drive.google.com/file/d/1k6AcZ-jFVAisIEiee17nu0X71J2judMS/view?usp=drivesdk): 38,650,598 bytes; SHA-256 `ec3193bb3ac24398c496ea65ad12aa653321db160a4dc74e8f618e3cc7ad6d1d`. ZIP CRC and SHA-256 of all 212 extracted files matched the original build. Extract the complete archive and launch `RoboRougelike.exe`.

1. Open **Garaż i poligon testowy** and select **Zawieszenie**. Check the missing-module guidance; mount suspension modules on valid low points and inspect the mode, height and support count.
2. Choose OFF, Assist and Full and change height; start the test range. Compare behavior on the existing uneven track near x≈12. Pause to adjust; check the displayed state and force.
3. With an installed Hover engine, select Hover and check the suppression message. Damage/remove supports or return to garage and verify that the state reports no operational module. Check readability and controls at 1280×720 and 1920×1080.
