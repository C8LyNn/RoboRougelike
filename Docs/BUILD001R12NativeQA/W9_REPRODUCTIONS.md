# Native W9 reproductions

Four failing NUnit cases; each collected two independent fixture observations before asserting the required rejection. These eight sub-observations are not eight additional NUnit cases.

## BUILD_W9_02_UnauthorizedSecondCoreCannotSeedASeparateIsland

```text
BUILD-W9-02 attempt=1 result=Valid support=untrustedLoadedCore secondRootReachable=True expected=reject untrusted second root
BUILD-W9-02 attempt=2 result=Valid support=untrustedLoadedCore secondRootReachable=True expected=reject untrusted second root
```

```text
BUILD-W9-02: every loaded island must be rooted in the one authorized Core; direct-placement rejection alone is insufficient
  Expected: 2
  But was:  0
```

## BUILD_W9_01_DecorativeChildCannotAuthorizeAContactFace

```text
BUILD-W9-01 attempt=1 hit=Decorative child collider point=(0.31, 1.00, 0.00) normal=(1.00, 0.00, 0.00) result=Valid face=(1, 0, 0) expected=reject decorative child
BUILD-W9-01 attempt=2 hit=Decorative child collider point=(0.31, 1.00, 0.00) normal=(1.00, 0.00, 0.00) result=Valid face=(1, 0, 0) expected=reject decorative child
```

```text
BUILD-W9-01: a child collider near a socket plane is not itself an authorized mount collider
  Expected: 2
  But was:  0
```

## BUILD_W9_03_DisconnectedCellsCannotRelaySupportAcrossEmptySpace

```text
BUILD-W9-03 attempt=1 emptyPhysicalGap=True definitionValid=True reason= bridge=Valid downstream=Valid owner=islands expected=no unsupported relay across empty cell
BUILD-W9-03 attempt=2 emptyPhysicalGap=True definitionValid=True reason= bridge=Valid downstream=Valid owner=islands expected=no unsupported relay across empty cell
```

```text
BUILD-W9-03: a single owner ID cannot turn disconnected physical islands into a coherent support bridge
  Expected: 2
  But was:  0
```

## BUILD_W9_04_OversizedColliderMustNotPassPlacementIntoAnOccupiedVolume

```text
BUILD-W9-04 attempt=1 result=Valid support=core physicalOverlap=True depth=0,3100 direction=(-1.00, 0.00, 0.00) declaredCells=1 actualSize=(1.2,.52,.52) expected=reject physical collision
BUILD-W9-04 attempt=2 result=Valid support=core physicalOverlap=True depth=0,3100 direction=(-1.00, 0.00, 0.00) declaredCells=1 actualSize=(1.2,.52,.52) expected=reject physical collision
Saving results to: C:/Users/kubap/AppData/LocalLow/DefaultCompany/RoboRougelike\TestResults.xml
```

```text
BUILD-W9-04: logical non-overlap alone cannot authorize colliding physical envelopes; boundary ownership needs a design ruling
  Expected: 2
  But was:  0
```
