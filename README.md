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

**M0 + core of M1 written.** Design and build plan complete. Combat, XP,
levelling, the ability system and the draft all exist in
`Assets/_Project/Scripts/` but are not yet wired into a Unity scene — see
[`docs/05-m0-setup.md`](docs/05-m0-setup.md) then
[`docs/06-m1-setup.md`](docs/06-m1-setup.md). The scripts have not been
compiled against a Unity install yet.

Still open in M1: wave timelines, the Spitter, the boss, and run-end screens.

Engine: Unity 6 LTS, URP, 3D.

## Documentation

| Doc | Contents |
|---|---|
| [`docs/00-current-state.md`](docs/00-current-state.md) | **Start here.** What is built vs designed vs not planned, system by system |
| [`docs/01-game-design.md`](docs/01-game-design.md) | Core loop, combat feel, stats and formulas, abilities and evolutions, enemies, gear, base rooms, economy, pacing targets |
| [`docs/02-tech-stack.md`](docs/02-tech-stack.md) | Engine choice and rationale, SDKs, data-driven architecture, project layout, mobile performance rules, save format |
| [`docs/03-roadmap.md`](docs/03-roadmap.md) | Milestones M0–M6 with exit criteria and realistic timings |
| [`docs/04-monetization-and-launch.md`](docs/04-monetization-and-launch.md) | Ad placements, IAP catalogue, analytics events, KPI targets, Play Store checklist, cloning legalities |
| [`docs/05-m0-setup.md`](docs/05-m0-setup.md) | How to wire the M0 combat scripts into a Unity scene, and what to tune first |
| [`docs/06-m1-setup.md`](docs/06-m1-setup.md) | XP, the level-up draft, the ability roster to author, and the HUD |
| [`docs/07-save-and-currency.md`](docs/07-save-and-currency.md) | The profile format, the migration rule, currencies, and the editor save tools |
| [`docs/08-draft-screen.md`](docs/08-draft-screen.md) | What triggers a draft, everything a card shows, and the rarity table |
| [`docs/09-art-direction.md`](docs/09-art-direction.md) | The art style, why, what to buy, and the readability check to run first |

## Start here

1. Open the project in Unity 6 (URP).
2. Run **ScalePunch ▸ Build Run Scene** from the menu bar. It generates every
   material, ScriptableObject, prefab and GameObject and wires all references,
   then saves `Assets/_Project/Scenes/Run.unity`. TextMeshPro's essential
   resources are imported automatically on first run.
3. Press Play.
4. Build to a real phone and tune `CombatTuning` until holding the ring against
   grey capsules is fun with no art. Nothing later fixes a core loop that fails
   this test — see [`docs/05-m0-setup.md`](docs/05-m0-setup.md) for the tuning
   order.

The builder is re-runnable and preserves asset GUIDs, so rebuild whenever
something comes unwired.

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
- **`TimeController` solely owns `Time.timeScale`.** Hitstop and menu pauses
  both want it; pauses are reference-counted and always win.
- **Abilities are data plus a small effect class.** A new active is a
  ScriptableObject asset and an `AbilityEffect` subclass — never a change to
  `AbilitySystem`.
- **Nothing calls `Instantiate` during a run.** Everything goes through
  `Pool<T>`.
- **Balance lives in ScriptableObjects**, never in code. `CombatTuning` holds
  every feel number; `EnemyDefinition` holds every enemy number.
