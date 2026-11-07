# R4 column structural-port quarantine

The pinned #963 source has four collider/socket discrepancies already recorded
in `R4_BLOCK_SOCKET_SURFACE_QA.md`: both HexColumn end patches and both
OctColumn end patches contain uncovered sampled points. Previously the adapter
filtered OctColumn ends for device support but still returned all four patches
as structural supports. That could permit a logical connection over geometry
the source collider does not fully cover.

The isolated BUILD001 adapter now filters those four exact source patches for
both structural and device definitions. OctColumn retains its four sampled side
patches; HexColumn has no remaining declared patch and therefore fails closed.
No source mesh or catalog entry is changed. Other patches passing a nine-point
sample are **not** thereby proven fully covered or accepted for gameplay.

Native test cases and lightweight smoke checks have been updated, but **NOT
RUN** under the owner's request to avoid interrupting other activity. Unity
compilation, all rotations, actual collider cooking, placement, graph and
save/load compatibility remain pending. This adapter is not connected to the
game. Source-owner review and a corrected export are still required; do not
silently alter the source geometry or enable these shapes in the garage.
