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

## 2. Prototype mode

The project is currently a **prototype**. Most systems are written and
compiling but switched off at `Assets/_Project/Data/PrototypeConfig.asset`.

Toggles rather than commented-out code, deliberately: commenting a system out
across dozens of files makes re-enabling a merge exercise, and code that does
not compile while disabled rots silently against every change made around it.

| Toggle | Now | On means |
|---|---|---|
| `waves` | **off** | Authored 8-wave Stage_01 instead of an endless timer |
| `boss` | **off** | Two-phase boss after the last wave |
| `runEndScreen` | **off** | Victory/defeat screen; off = death restarts the run |
| `abilityRarity` | **off** | Five rarity tiers scaling card magnitude |
| `saveAndCurrency` | **off** | Coins bank and a profile is written to disk |
| `healthBars` | **off** | Floating bars over zombies |
| `fullAbilityRoster` | **off** | 8 abilities × 5 levels instead of 3 × 3 |

`fullAbilityRoster` is read by `RunSceneBuilder` when authoring assets, so
changing it needs a rebuild — the rest take effect on the next play.

`GameBootstrap` publishes the config at `DefaultExecutionOrder(-10000)`, because
half the scene checks a toggle in its own `Awake` and Unity does not define the
order those run in.

### The bullet ladder

Every zombie's HP is **derived from the player's base damage**, so tiers are
authored as "dies in N bullets" rather than as raw HP:

| Tier | HP | Bullets | Time to kill | Speed |
|---|---|---|---|---|
| Shambler | 5 | **1** | 0.33 s | 2.5 |
| Runner | 10 | **2** | 0.67 s | 4.5 |
| Brute | 30 | **6** | 2.00 s | 1.4 |
| Boss | 200 | **40** | 13.3 s | 1.6 |

At the base fire rate of 3/s, before any upgrades. `StatSheet.BaseDamage` is
the single anchor — the builder reads it rather than keeping a copy, so
changing damage moves every tier together instead of silently breaking the
ladder.

**Per-wave HP scaling is off** (`hpGrowthPerWave = 1`). With it on, a Shambler
needed two bullets twenty seconds into every run, and nothing announced that the
baseline had moved. Difficulty in the prototype comes from spawn rate alone.

**Armour is 0 on everything with a countable bullet budget.** Armour and "dies
in exactly N bullets" pull against each other: at armour 1 a 5-damage round
lands 4, so a 30 HP Brute becomes eight shots rather than six. The Brute's
identity is knockback immunity instead.

The builder prints the ladder at wave 0 and wave 8 on every rebuild. If those
two columns disagree, scaling is on and the ladder is a wave-0 promise the game
stops keeping.

### Why rounds used to miss

Three independent faults, all of which had to be fixed for "one bullet, one
Shambler" to hold in practice rather than only on paper:

| Fault | Cost |
|---|---|
| Aimed at the target's **current** position | A Runner moves 0.90 m during a 9 m flight; the hit radius is 0.55 m |
| Firing arc was a flat **12°** | 1.91 m of allowed lateral error at 9 m, against a target 0.55 m wide |
| Target stickiness let a zombie sit at **9.65 m** while rounds expire at 9 m | Every shot at a drifted target died 0.65 m short |

Two more surfaced only after those were fixed, because each was a system that
was correct on its own:

| Fault | Cost |
|---|---|
| The round's homing steered toward the target's **current** position | Pursuit guidance, running every frame, erasing the turret's lead a frame at a time |
| Aim was solved from the **muzzle**, 1.16 m out along the barrel | Zombies attack from 1.4 m; once inside that the muzzle-to-target vector flips and the turret whips 180° |

Now: one shared `Ballistics.Intercept` serves the turret *and* the round, so the
two cannot disagree; the round re-solves its intercept as it flies rather than
chasing where the target stood; aim is solved about the turret **pivot** with the
round merely *spawning* at the barrel tip; the firing arc is the target's angular
size plus half of what the round can steer out; and it holds fire when the
intercept falls outside the ring.

`WeaponTelemetry` logs hits against rounds fired every 50 shots. Under ~85%
against walkers means aiming is wrong, not unlucky — this bug survived two fixes
that each sounded right, and a number would have settled it immediately.

Rounds **reserve their damage** against the target while in flight. That
reservation is a **preference, not an exclusion**:

- The current target is never dropped for being doomed — the engagement finishes.
- Target selection prefers a non-doomed zombie but falls back to a doomed one
  rather than returning nothing.

Both rules exist because the strict version broke exactly one tier. A Shambler
has 5 HP and a round carries 5 damage, so a single round in the air marked it
dead-on-arrival, the turret stopped tracking it, and if that round then missed
the Shambler walked in unengaged until it expired. Nothing above 5 HP could
reproduce it, which is why it looked like a level-1 problem.

It also bought nothing: at a 0.33 s fire interval against a 0.20 s flight time,
only one round is ever airborne, so overkill was not possible in the first place.
The reservation stays for when fire rate climbs past flight time.

### One progression: "Level"

There is a single number. It rises on kills, and it drives **both** the ability
draft and the spawn ramp.

Before, a "level" advanced on kills while a "wave" advanced on a 20-second
clock. Two progressions, two names, two bars — and the player had no way to tell
which one was making the game harder. They are the same thing now, shown once,
on the top track.

Driving difficulty from kills rather than the clock also makes the ramp
**self-balancing**: a player clearing fast earns harder levels, and one who is
struggling is not buried by a timer that does not care how they are doing.

| Level | Reached at | What changes |
|---|---|---|
| 1-2 | 0 / 5 kills | Shamblers only |
| 3 | 15 kills | Runners enter at 40% |
| 6 | 105 kills | Brutes enter at 8% |
| 10 | 275 kills | Runners 50%, Brutes 16% |
| 15 | — | Spawn interval floors; burst takes over |

### The spawn ramp

Difficulty comes from **composition and rate, never HP**. The bullet ladder is a
fixed promise, so a harder level means more zombies and tougher *kinds*, not the
same zombie with more health.

| Level | Interval | Burst | Spawns/min | Clear/min | Mix |
|---|---|---|---|---|---|
| 1-2 | 1.40s | 1 | 43 | 180 | Shambler 100% |
| 3 | 1.25s | 1 | 48 | 129 | Shambler 60%, **Runner 40%** |
| 6 | 1.02s | 1 | 59 | 98 | Shambler 48%, Runner 44%, **Brute 8%** |
| 10 | 0.72s | 1 | 83 | 79 | Shambler 34%, Runner 50%, Brute 16% |
| 15 | 0.35s | 1 | 171 | 65 | Shambler 25%, Runner 50%, Brute 25% |
| 20 | 0.35s | 2 | 343 | 65 | steady |
| 25 | 0.35s | 3 | 514 | 65 | steady |

*Clear/min* is what an **un-upgraded** player can kill, from fire rate against
the mix's weighted bullets-per-kill. The crossover is **level 10** — from
there, upgrades have to cover the gap. That is the floor the draft has to beat,
and the number to watch when tuning either side.

Two rules the shape depends on:

- **Levels 1-2 are Shamblers only.** A second tier means nothing until the player
  knows what one bullet does.
- **Interval and burst never ramp at once.** Interval carries levels 1-15; burst
  only starts once it floors. A burst step is an integer, so overlapping them
  roughly doubles the spawn rate in a single level — a staircase that reads as the
  game breaking rather than hardening. `SpawnRamp.OnValidate` warns if they
  overlap.

Authored in `SpawnRamp.asset`; the builder prints the whole table on rebuild.

### What the prototype actually is

Stationary player, auto-fire, zombies arriving forever on an accelerating timer.
A draft at 5 kills, another at 15, then 30/50/75. Three abilities, three levels
each: Shockwave, Heavy Rounds, Trigger Work. Death restarts. Nothing persists.

That is the thing being tested. If holding the ring is not fun at this size, no
toggle on the list above changes the answer.

## 2b. Status

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
| Draft rarity (5 tiers) + before/after card values | **BUILT** |
| Kill-triggered drafts (5, 15, 30, …) | **BUILT** |
| Run HUD: top level track (kill slider + level badge + total kills), bottom bar with health and boosters | **BUILT** |
| Booster slots showing active abilities + cooldown sweep | **BUILT** |
| Waves / stages | **BUILT** — 8 authored waves, ~3:30 |
| Boss | **BUILT** — two phases, telegraphed charge |
| Run end (win/lose) | **BUILT** — end screen, payout, retry |
| **Audio** | **DESIGNED** — *no audio system exists at all* |
| Currencies (coins/gems/scrap) | **BUILT** — no sink until P6 |
| Save / load + migrations | **BUILT** — local only, no cloud save |
| Gear, base rooms | **DESIGNED** — zero code |
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

### P1 — The run lifecycle — **DONE**

- [x] `WaveDefinition` / `StageDefinition` timelines replacing the timer ramp
- [x] One boss with two phases and a telegraphed charge
- [x] Victory and defeat screens with a reward summary
- [x] Restart flow
- [x] Failed runs pay out at a reduced rate (`failPayoutFraction`, default 0.35)

Still open from P1: **return-to-meta** has nowhere to return to until P2 builds
a meta scene, and the payout is **computed and displayed but not banked** —
`RunController.Payout()` is the seam a `CurrencyService` plugs into.

### P2 — Persistence and rewards — **DONE**

- [x] `CurrencyService`: coins, gems, scrap, all through Earn/Spend with
      analytics-ready reason tags
- [x] Run payout banked on win and loss, written to disk immediately
- [x] `SaveService`: versioned JSON, atomic writes, one backup, checksummed,
      **with the migrations chain in place from day one**
- [x] Editor tools for wiping and inspecting the profile
- [ ] **Cloud save** — needs Google Play Games / Firebase; moved to M4 with the
      rest of the SDK work. Must ship before launch

See [`docs/07-save-and-currency.md`](07-save-and-currency.md). Coins have no
sink until P6, so do not tune `coinsOnClear` seriously yet.

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
