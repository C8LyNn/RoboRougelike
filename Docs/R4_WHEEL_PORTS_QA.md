# Wheel port correction and integrated preview

Windows build source `ceeba233`, stacked on Draft PR #1047. Newly purchased,
recovered, stored and remounted side-wheel variants use the two opposing local
side faces; the small fork uses its upper face. Rotation transforms those
ports with the part. Already mounted legacy wheels retain their saved contacts
until removal, avoiding an implicit change to an existing vehicle. Legacy
inventory contacts are normalized on a copy when loaded, preserving the input
save. Factory-wheel restoration remains valid after normalization.

- Unity 6000.3.14f1 EditMode runner, tests entering Play mode: **22 passed,
  0 failed, 0 skipped**, native exit 0 on `e13c5bf577f7ba15459d3f2e0768cc58773d1fe1`.
  Covers side-only new mounts, old inventory loading, factory wheel removal
  and remounting, movement-system swap, salvage and wheel variants. Evidence:
  `Artifacts/Local/RC2-SideWheelPorts-20261108T005954Z/`.
- Correct-port R6 drive and visual socket orientation: **9 passed, 0 failed,
  0 skipped**, exit 0 on `7cdc23681793ccc5b7f274d6b67362c28cc8ebd9`.
  Large, monster and small vehicles each drove on actual Rigidbody/ground with
  a single input per fixed step. Evidence:
  `Artifacts/Local/RC2-SideWheelDrive-20261108T010138Z/`.
- Windows x64: Unity build exit 0. The ZIP was extracted and the extracted
  executable opened the garage, switched to/from underbody view with U, and
  exited via the game menu. This check does not represent a full campaign.
- Full campaign, terrain balance and standalone PlayMode suite: **NOT RUN**.

Archive: `C:\Users\kubap\Desktop\Prototypy\RoboRougelike_Kola_Klocki_ceeba233_Windows_x64.zip`
(44,776,327 bytes), SHA-256
`a50147c98a5d4f754e554e5addd37df554405a057afcf01cb98a43e120871044`.
