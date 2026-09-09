# ScalePunch

A mobile survivors-like action roguelite with an idle base meta — waves of
enemies, fist-only auto-combat, an ability draft on every level-up, and gear
plus base-building progression between runs.

Genre reference: *Endless Puncher* (Rollic Games), *Archero*,
*Vampire Survivors*.

## Status

**M0 in progress.** Design and build plan complete. Combat scaffold written
(`Assets/_Project/Scripts/`) but not yet wired into a Unity scene — see
[`docs/05-m0-setup.md`](docs/05-m0-setup.md).

Engine: Unity 6 LTS, URP, 3D.

## Documentation

| Doc | Contents |
|---|---|
| [`docs/01-game-design.md`](docs/01-game-design.md) | Core loop, combat feel, stats and formulas, abilities and evolutions, enemies, gear, base rooms, economy, pacing targets |
| [`docs/02-tech-stack.md`](docs/02-tech-stack.md) | Engine choice and rationale, SDKs, data-driven architecture, project layout, mobile performance rules, save format |
| [`docs/03-roadmap.md`](docs/03-roadmap.md) | Milestones M0–M6 with exit criteria and realistic timings |
| [`docs/04-monetization-and-launch.md`](docs/04-monetization-and-launch.md) | Ad placements, IAP catalogue, analytics events, KPI targets, Play Store checklist, cloning legalities |
| [`docs/05-m0-setup.md`](docs/05-m0-setup.md) | How to wire the M0 scripts into a Unity scene, and what to tune first |

## Start here

1. Create a Unity 6 LTS (3D / URP) project at this repo root so `Assets/` sits
   beside `docs/`.
2. Follow [`docs/05-m0-setup.md`](docs/05-m0-setup.md) to build the `Run` scene.
3. Build to a real phone and tune `CombatTuning` until punching grey capsules is
   fun with no art. Nothing later fixes a core loop that fails this test.

## Architecture notes

- **Combat is registry-based, not physics-based.** `EnemyRegistry` answers all
  "nearest enemy" and "enemies in range" queries. No colliders, no rigidbodies,
  no layer masks — and no 150-rigidbody frame cost on mid-range Android.
- **Nothing calls `Instantiate` during a run.** Everything goes through
  `Pool<T>`.
- **Balance lives in ScriptableObjects**, never in code. `CombatTuning` holds
  every feel number; `EnemyDefinition` holds every enemy number.
