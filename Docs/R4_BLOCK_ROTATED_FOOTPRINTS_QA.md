# R4 block footprint rotation checks — prepared, not executed

`R4MountDefinitionAdapterTests.EveryAuthoredShapeKeepsOneOwnerAndBlocksEachRotatedOccupiedCell`
enumerates all 30 staged block definitions and all 64 Euler quarter-turn
combinations representing 24 distinct orientations. It checks that rotated
occupied cells remain distinct and each occupied cell is rejected when another
part attempts to use it in the standalone BUILD001 placement rules. HexColumn
is checked separately as unavailable because all its declared sockets are
quarantined; the remaining 29 shapes receive the rotation checks.

The assertions were added while the user requested no Unity runs or builds.
**NOT RUN**: Unity compilation and execution, physical collider cooking,
socket coverage, in-game garage placement, damage graph, save/load and actual
vehicle physics. The test uses the existing disconnected adapter and does not
enable any of the new shapes in the game. Four source socket-surface
discrepancies and other previously reported R4 integration blockers remain.

When heavier work is permitted, run the targeted EditMode fixture on the exact
source SHA, inspect XML and Editor log, and then proceed with actual gameplay
integration only after the canonical R3 mounting/physics/save rules agree.
