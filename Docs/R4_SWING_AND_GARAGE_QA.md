# Garage categories, caster and swing-arm wheel

Based on Draft PR #1055. The garage filters its quick parts list into four
categories. The swing-arm model from #1018 `R4M_WheelM_SwingArm_Offroad.fbx`
has SHA-256 `ea7931360e9af8ebedb26c86fa7eda16f0b8b1e882b4ca1dd784741200518d8c`.
It is a single owned wheel with a top socket, 0.33 m radius, 0.26 m width,
12 kg mass and normal wheel traction. The arm follows nearby real terrain
within a limited angle, with the physical tire following its visual axle.
It uses R6's existing contact/force system and does not add spring forces or
another Rigidbody. Existing MOV-009 suspension may be used; the free arm is
not driven as a visual MOV-011 steering knuckle. Model and parameters remain
initial tuning requiring human driving feedback on varied terrain.

- Native Unity 6000.3.14f1 targeted EditMode runner, test enters Play mode:
  raised ground causes arm movement and physical axle alignment **1/1 PASS**
  at `2bc582b6d1e03deed5902e30167f94ce2275ceb3`, evidence
  `Artifacts/Local/RC2-SwingArm-20261108T093526Z`.
- Targeted swing-wheel garage purchase/mount/checkpoint, actual R6/Rigidbody
  driving with existing MOV-009 controller and wheel catalog **3/3 PASS** at
  `e5e0e74fa41b9dabf178a356d8e2595a5e837afd`, evidence
  `Artifacts/Local/RC2-SwingGameplay-20261108T093719Z`. The R6 fixture traveled
  8.46 m in 1.6 s on flat ground. This is not an uneven-terrain playtest.
- Targeted existing real-garage repair/removal/mount-next-fight and victory
  checkpoint restoration **2/2 PASS** at the same SHA, evidence
  `Artifacts/Local/RC2-CombinedFlow-20261108T093809Z`. These are API-driven
  Unity tests, not a complete human-played campaign.
- Pre-final Windows Player `8128723e`: entered garage, selected and purchased
  both caster and swing wheel, visited practice range and returned to garage.
  The category buttons wrapped at this resolution, fixed subsequently.
- Final Windows x64 Player `3463782f`: Unity build exit 0, ZIP extracted and
  game launched. Category labels fit in the tested window; category switching,
  garage-to-route-to-battle, one shot and normal pause-menu exit observed.
  New-wheel manual installation/driving, complete campaign, other window
  sizes and standalone PlayMode suite: **NOT RUN**. Build log
  `Artifacts/Local/GarageCasterSwingFinalBuild.log`.
- ZIP `C:/Users/kubap/Desktop/Prototypy/RoboRougelike_Garaz_Kola_3463782f_Windows_x64.zip`
  is 46,068,908 bytes; SHA-256
  `729adfe6d2439f778848eec875ef70a97ed54f2d7bc922e0031cc5728a0f5dec`.

In garage select Koła, scroll to the desired wheel, buy it and press U to see
its upper mounting face. The player must leave space for the moving part.
Caster tuning, swing-wheel behavior over ramps, chassis self-clearance across
the complete articulation range and UI comfort at other aspect ratios need
further testing before claiming release readiness.
