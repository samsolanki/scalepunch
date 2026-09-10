# Current State & Backlog

Rewritten after the Unity project landed. **48 runtime scripts, 4 editor
scripts, 19 data assets, 6 prefabs, one playable scene.**

| Mark | Meaning |
|---|---|
| **BUILT** | Exists and is wired into `Run.unity` |
| **DESIGNED** | Specified in the docs. No code |
| **NOT PLANNED** | Not in the design, deliberately or otherwise |

---

## 1. What the game is

A **stationary zombie-defence roguelite**. The player is a fixed emplacement
with an engagement radius. Zombies converge from every direction; anything
crossing the ring is acquired and shot automatically. No movement input, no
aiming input, no fire button. Every level-up pauses the run for a choice of
three cards — the only interaction a run has.

## 2. Status

| System | Status |
|---|---|
| Unity 6 project, URP mobile | **BUILT** |
| `RunSceneBuilder` — generates scene, prefabs, materials, data | **BUILT** |
| Auto-targeting + shooting, 4 priorities | **BUILT** |
| Projectiles: pooled, swept hits, pierce, spread, homing | **BUILT** |
| Zombies: chase, melee, knockback, separation | **BUILT** |
| Armour, crit, lifesteal | **BUILT** |
| Feel: hitstop, shake, flash, flinch, muzzle flash, damage numbers | **BUILT** |
| Floating health bars (pooled, one canvas) | **BUILT** |
| XP, gems, magnet, levelling | **BUILT** |
| Ability draft, 8 abilities, 3 active effects | **BUILT** |
| Run HUD: XP, health, timer, kills, priority toggle | **BUILT** |
| **Waves / stages** | **DESIGNED** — spawner is still a plain timer ramp |
| **Boss** | **DESIGNED** |
| **Run end (win/lose)** | **DESIGNED** — death only disables the HUD |
| **Audio** | **DESIGNED** — *no audio system exists at all* |
| Currencies, gear, base rooms, save | **DESIGNED** — zero code |
| Ads / IAP / analytics | **DESIGNED** — M4 |
| Energy system | **NOT PLANNED** — rejected, see §5 |
| Skill tree | **NOT PLANNED** — see §5 |

---

## 3. The backlog, in the order it should be done

### P0 — Repo correctness (~30 min)

The project currently contradicts itself.

- [ ] **Stale data assets.** `Zombie_Brute.asset` holds 90 HP / armour 2;
      `RunSceneBuilder.Data.cs` writes 10 HP / armour 0. The scene loads the
      asset, so the repo does not behave the way the last commit says. Re-run
      `ScalePunch/Build Run Scene` and commit.
- [ ] **Two sources of truth for balance.** The `.asset` files and the builder
      both claim to own the numbers, which is what produced the item above.
      Pick one: gitignore generated assets and let the builder own balance, or
      make the builder a one-time bootstrap and let the assets own it. Both is
      the state that keeps regenerating this bug.
- [ ] **Zombie tiers are flattened.** All three are effectively 10 HP. A Brute
      with 10 HP, no armour and knockback immunity is a slow large Shambler.
      Fine while debugging "does the gun work"; meaningless for tuning.
- [ ] Delete `Assets/NewMonoBehaviourScript.cs` (Unity template leftover).
- [ ] Duplicate `<summary>` tag on `SqrDistanceToSegment`.

### P1 — The run lifecycle (the real gap)

**A run currently has no beginning, no end and no consequence.** Everything
downstream is meaningless until this exists.

- [ ] `WaveDefinition` / `StageDefinition` timelines to replace the timer ramp
- [ ] One boss with two telegraphed phases
- [ ] Victory and defeat screens with a reward summary
- [ ] Restart / return-to-meta flow
- [ ] Failed runs still pay out, at a reduced rate — never send a player away
      empty-handed

### P2 — Persistence and rewards

Closing the app currently erases everything.

- [ ] `CurrencyService`: coins, gems, scrap
- [ ] Run payout on win and loss
- [ ] `SaveService`: versioned JSON **with a migrations class from day one**.
      The first post-launch save-shape change without one wipes every account
- [ ] Cloud save before launch — device loss is a top-3 one-star review cause

### P3 — Audio (nothing exists)

Disproportionate return for the effort. In a shooter, sound is most of what
sells the gun.

- [ ] Pooled `AudioService` (never `PlayOneShot` on 40 concurrent impacts)
- [ ] Fire layers with pitch variation, impact, zombie death, level-up sting,
      low-health warning, UI clicks
- [ ] Music with a wave-intensity layer

### P4 — De-risk the core design question (cheap, do it early)

- [ ] One manually triggered ability on a cooldown — a grenade button or a
      barricade

With a stationary player and automatic fire there is **no moment-to-moment
input**. If testers say runs feel like *watching*, this is the fix, and it is
far cheaper to try now than after 30 stages exist.

### P5 — Content breadth

- [ ] **Weapon roster.** Shotgun, minigun, railgun are *pure data* — the
      multiplier system already supports them with zero code changes. Highest
      variety-per-hour in the project
- [ ] Spitter (needs ranged-attack code), Bomber, Swarm, Elite
- [ ] Abilities from 8 to 25-30
- [ ] Evolutions — hooks exist on `AbilityDefinition`, nothing unlocks them
- [ ] 30 stages across 3 chapters

### P6 — Meta layer

- [ ] Gear: 6 slots, 5 rarities, affixes, levelling, duplicate fusion
- [ ] Base rooms + offline Vault income, 8-hour cap
- [ ] Daily missions, login rewards

### P7 — Business layer (M4)

- [ ] Ad mediation, IAP, analytics, remote config, Crashlytics, consent flow

---

## 4. Worth doing that is not on the roadmap

- **Extend `RunSceneBuilder` to author waves and stages.** It already generates
  every other asset; wave timelines are the next thing that would otherwise be
  hand-built and un-diffable.
- **A headless balance harness.** Simulate N runs, log time-to-kill per tier and
  DPS per build, and print a table. In a game whose entire quality is a balance
  curve, being able to answer "did that change help?" without playing for twenty
  minutes pays for itself in a week.
- **Editor validation pass.** One menu item that asserts every definition has a
  prefab, every ability has five levels, no draft weight is zero by accident.
  Catches the class of bug that only shows up ten minutes into a run.

## 5. Two standing design decisions

**No energy system, deliberately.** It caps session length in a genre that lives
on long sessions and throttles the most engaged players. Progress is gated by
stage difficulty instead. (`docs/01-game-design.md` §9)

**No skill tree.** Progression is the in-run ability draft plus a flat list of
upgradeable base rooms — no prerequisites, no exclusive paths, no respec. That
is the genre standard because it stays legible on a phone. A branching tree is a
design change, and it changes the save format, so decide before P2.

## 6. Note on the setup guides

`05-m0-setup.md` and `06-m1-setup.md` describe wiring the scene by hand.
`RunSceneBuilder` now does that from a menu item. Keep them as a reference for
what the scene *contains*, but the builder is how it gets made.
