# Hit-detection harness

Compiles the **real** `Ballistics.cs` and `EnemyRegistry.cs` — copied verbatim
from `Assets/` at run time — against a minimal `UnityEngine` stub, and drives
them through 360 full engagements (4 tiers × 90 approach angles) frame by frame
with the shipped constants.

```
sudo apt-get install -y mono-mcs   # one-time
Tools/HitHarness/run.sh
```

Exits non-zero if any round expires with a live target still in range.

## What it covers

Target interception, turret slew, the firing arc, projectile homing, and the
swept segment test against every tier's body radius.

## What it does NOT cover, and why that matters

It exercises the **maths**, not the **runtime state**. Untested here:

- Object pooling — round reuse, `_live` / `_alreadyHit` / `_connected` resets
- Damage reservation and `IsDoomed` target switching
- Multiple simultaneous zombies
- `Time.timeScale` — hitstop and the draft pause
- Prefab wiring: whether `Enemy.health` is assigned, whether the registry is
  populated, whether `Projectile.Update` runs at all

A pass here means aiming and detection are correct in isolation. If rounds still
fail in the editor, the fault is in that second list, and the near-miss logging
in `WeaponTelemetry` is what narrows it down.

## Pass criterion

**Every round must connect**, at every range including point blank — not merely
"no round was logged as a miss".

The first version failed only on `missed > 0`, and it reported PASS against a
92% hit rate. A round that spawns past its target and spends its whole life
turning around is never logged as a miss; it is still in the air when the
engagement ends. A test that cannot fail is worse than no test, because it is
believed.

## Regression value

Verified in both directions against the barrel-tip spawn bug:

```
pre-fix   92.3%   FAIL - 361 rounds never landed
post-fix  100.0%  PASS
```

Point-blank cases are what catch it. Engagements that start at 16 m and walk in
never sample the range where a round can spawn *past* its target, so the
original harness passed happily while the bug was live.

Run against the pre-fix flat-radius detection, the harness **still passes** —
because the lock-on drives every round to the target's centre, where a 0.55 m
test is plenty even for a Brute. Worth knowing: the per-tier body radius is a
genuine correctness fix and it matters for any weapon that does *not* home (a
shotgun), but it was **not** the cause of rounds failing to connect in play.
