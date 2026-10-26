#!/usr/bin/env bash
set -euo pipefail
mkdir -p Tools/B01/ECONLEGENDARY001/_upstream
curl --fail --silent --show-error --location --retry 3 "https://raw.githubusercontent.com/C8LyNn/RoboRougelike/12572117a3dce369257feabc57021cde58023369/Assets/B01Packages/ECONREBUILD001/Runtime/RebuildQueueService.cs" -o Tools/B01/ECONLEGENDARY001/_upstream/Econ.cs
curl --fail --silent --show-error --location --retry 3 "https://raw.githubusercontent.com/C8LyNn/RoboRougelike/dffabbc6960304fdb33322b5a23c884feac804c6/Assets/B01Packages/LEGENDARY001/Runtime/LegendaryUnityCore.cs" -o Tools/B01/ECONLEGENDARY001/_upstream/Legend.cs
ACTUAL_ECON=$(git hash-object Tools/B01/ECONLEGENDARY001/_upstream/Econ.cs)
ACTUAL_LEG=$(git hash-object Tools/B01/ECONLEGENDARY001/_upstream/Legend.cs)
test "$ACTUAL_ECON" = "cac95b28647528a7a46cb818c6d78037dc3e80a2" || { echo "BAD_PIN_ECON $ACTUAL_ECON";exit 1; }
test "$ACTUAL_LEG" = "b5ec9f163cd9e967e09a1876ed3f39412ff6c53c" || { echo "BAD_PIN_LEGEND $ACTUAL_LEG";exit 1; }
echo "PINNED_REAL_UPSTREAM_BLOBS_PASS $ACTUAL_ECON $ACTUAL_LEG"

# The R3 host-side ports themselves are immutable install-only reference sources.
curl --fail --silent --show-error --location --retry 3 "https://raw.githubusercontent.com/C8LyNn/RoboRougelike/dffabbc6960304fdb33322b5a23c884feac804c6/Tools/B01/LEGENDARY-UNITY-001/R3LegendaryPort.install.cs" -o Tools/B01/ECONLEGENDARY001/_upstream/R3LegendaryPort.install.cs
curl --fail --silent --show-error --location --retry 3 "https://raw.githubusercontent.com/C8LyNn/RoboRougelike/12572117a3dce369257feabc57021cde58023369/Tools/B01/ECONREBUILD001/R3EconRebuildPort.install.cs" -o Tools/B01/ECONLEGENDARY001/_upstream/R3EconRebuildPort.install.cs
test "$(git hash-object Tools/B01/ECONLEGENDARY001/_upstream/R3LegendaryPort.install.cs)" = "9ac3a3e1c45fa8e52ccc36c09ac973cc017c5269"
test "$(git hash-object Tools/B01/ECONLEGENDARY001/_upstream/R3EconRebuildPort.install.cs)" = "61b0e8bf93095c304e58b125ef4086fa93ad492b"
echo "PINNED_RECEIVER_PORT_SOURCES_VERIFIED"
