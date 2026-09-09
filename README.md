# ScalePunch

A mobile zombie-defence roguelite with an idle base meta. The player is a
**fixed emplacement** with an engagement radius: zombies converge from every
direction, anything crossing the ring is acquired and shot automatically, and
an ability draft on every level-up plus gear and base-building between runs
carry the progression.

No movement, no joystick, no fire button — the decisions are what you draft and
what you upgrade, not where you stand.

Genre reference: *Endless Puncher* (Rollic Games), *Archero*,
*Vampire Survivors*, tower-defence idle shooters.

## Status

**M0 in progress.** Design and build plan complete. Combat scaffold written
(`Assets/_Project/Scripts/`) but not yet wired into a Unity scene — see
[`docs/05-m0-setup.md`](docs/05-m0-setup.md). The scripts have not been
compiled against a Unity install yet.

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
3. Build to a real phone and tune `CombatTuning` until holding the ring against
   grey capsules is fun with no art. Nothing later fixes a core loop that fails
   this test.

## Architecture notes

- **Combat is registry-based, not physics-based.** `EnemyRegistry` answers
  every "nearest zombie", "zombies in range" and projectile-sweep query. No
  colliders, no rigidbodies, no layer masks — and no 150-rigidbody frame cost
  on mid-range Android.
- **Projectiles sweep, they do not point-test.** A 30 m/s round covers half a
  metre per frame and would otherwise tunnel through anything standing between
  its old and new position.
- **Weapons are multipliers over the player's stat sheet**, never absolute
  numbers, so every weapon scales off one upgrade tree.
- **Nothing calls `Instantiate` during a run.** Everything goes through
  `Pool<T>`.
- **Balance lives in ScriptableObjects**, never in code. `CombatTuning` holds
  every feel number; `EnemyDefinition` holds every enemy number.
