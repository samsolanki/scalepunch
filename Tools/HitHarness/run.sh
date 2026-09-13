#!/usr/bin/env bash
# Compiles the REAL Ballistics.cs and EnemyRegistry.cs against minimal UnityEngine
# stubs and drives them through full engagements, frame by frame, with the
# shipped constants. Nothing here reimplements the game's maths — the two files
# under test are copied verbatim from Assets/.
#
#   sudo apt-get install -y mono-mcs     # one-time
#   Tools/HitHarness/run.sh
#
# Exits non-zero if any round expires with a live target still in range.
set -euo pipefail

HERE="$(cd "$(dirname "$0")" && pwd)"
SRC="$HERE/../../Assets/_Project/Scripts"
BUILD="$(mktemp -d)"
trap 'rm -rf "$BUILD"' EXIT

cp "$HERE"/{UnityStubs.cs,EnemyStub.cs,Harness.cs} "$BUILD/"
cp "$SRC/Combat/Ballistics.cs" "$SRC/Enemies/EnemyRegistry.cs" "$BUILD/"

# Mono's compiler predates C# 9. Purely syntactic; no logic is altered.
sed -i 's/new(256)/new List<Enemy>(256)/' "$BUILD/EnemyRegistry.cs"

cd "$BUILD"
mcs -langversion:latest -target:library -out:shipped.dll \
    UnityStubs.cs EnemyStub.cs Ballistics.cs EnemyRegistry.cs
mcs -langversion:latest -out:harness.exe -r:shipped.dll Harness.cs
mono harness.exe
