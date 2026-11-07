# R4 armor cube — first gameplay intake

Source: artist pack #963 `03af5207143da76faeeb8004543a51f26c7f9c0d`, existing verified `R4B_Cube.fbx` and Unity metadata. This integrates the single-cell cube appearance only. Other 29 shapes, spatial occupancy, restricted ports and selection are not yet in gameplay.

Owned armor and its garage ghost use the supplied mesh. The old physical collider, original HP and ownership remain unchanged; missing or malformed art falls back to the original cube. The gunmetal surface retains the vehicle color.

Unity 6000.3.14f1 EditMode at `bc728f70ffa2ebf75858d8eba763932b7c921090`: 12/12 passed, exit 0, including actual armor attachment, checkpoint tests, replacement model damage and wheel physics. Local evidence: `Artifacts/Local/RC2-CubeArt-Verify-bc728f70-20261107T224904Z/`.
Windows build `1aa878c6` exit 0; actual Player opened, garage entered and cube material/vehicle tint visually observed without pink shaders; returned to menu and exited. No performance benchmark, full combat regression or old-save cold restart executed for this revision.
