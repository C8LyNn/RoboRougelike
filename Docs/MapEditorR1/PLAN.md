# Map editor R1 integration plan

- Build on `feature/weapon-types-stage1` (`d44fa09c227dffca1c1f8832383bbd80430bba90`), with an isolated branch and no scene asset changes.
- Create a bounded, versioned map document with 6 kinds, undo/redo, transactional JSON loading and local named files. One ground tile and one upper-layer item may occupy a grid cell; start and finish are unique.
- Render real colliders and a ramp mesh. The editor uses the existing main camera only while not driving; editing begins from the main menu and can return to the existing garage for vehicle changes.
- Test builds a copy of the canonical R3 vehicle with existing R6 wheels/physics. Returning restores the workshop model and map document; Esc retains the normal pause behavior with a return-to-editor action.
- Verify document operations, invalid file rejection and geometry in headless Unity; build Windows. Never start an interactive Player while the user prohibits control; mark manual gameplay NOT RUN.
