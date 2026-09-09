# Current State — what actually exists

Written after the M1 commit. **43 scripts, ~3,300 lines.**

This document separates three very different things, because conflating them is
how projects convince themselves they are further along than they are:

| Mark | Meaning |
|---|---|
| **BUILT** | Code exists in `Assets/_Project/Scripts/`. Not compiled against a Unity install yet — no Unity in the environment these were written in. |
| **DESIGNED** | Specified in the docs. No code. |
| **NOT PLANNED** | Does not exist in the design, deliberately or otherwise. |

Nothing here has been run. There is no Unity project yet — only scripts and the
setup guides for wiring them up (`05-m0-setup.md`, `06-m1-setup.md`).

---

## 1. What the game is

A **stationary zombie-defence roguelite**. The player is a fixed emplacement
that never moves. Zombies converge from all directions; anything crossing the
player's engagement radius is acquired and shot automatically. There is no
movement input, no aiming input, and no fire button.

Every 20-ish seconds you level up, the run pauses, and you pick one of three
upgrade cards. That draft is the **only** interaction a run has.

---

## 2. System status at a glance

| System | Status | Notes |
|---|---|---|
| Player emplacement | **BUILT** | One player. No characters, no classes |
| Auto-targeting + shooting | **BUILT** | 4 target priorities, HUD toggle |
| Projectiles | **BUILT** | Pooled, swept hits, pierce, spread, soft homing |
| Zombies | **BUILT** | 1 behaviour coded, 3 stat variants specced |
| Damage / crit / knockback | **BUILT** | Single crit path via `DamageDealer` |
| Feel (hitstop, shake, flash, numbers) | **BUILT** | All tunable from one asset |
| XP + levelling | **BUILT** | Gems, magnet, quadratic curve |
| Ability draft | **BUILT** | Weighted offer, 8 abilities specced |
| Ability effects | **BUILT** | 3 actives coded |
| Run HUD | **BUILT** | XP, health, timer, kills, priority |
| Waves / stages | **DESIGNED** | Spawner still ramps on a plain timer |
| Boss | **DESIGNED** | No code |
| Run end (win/lose) | **DESIGNED** | **Player death currently does nothing** |
| Currencies | **DESIGNED** | Zero code |
| Gear | **DESIGNED** | Zero code |
| Base / idle rooms | **DESIGNED** | Zero code |
| Save / load | **DESIGNED** | Zero code — nothing persists |
| Rewards | **DESIGNED** | Zero code |
| Ads / IAP / analytics | **DESIGNED** | Zero code (M4) |
| **Energy system** | **NOT PLANNED** | Deliberately rejected — see §8 |
| **Skill tree** | **NOT PLANNED** | Not in the design at all — see §9 |

---

## 3. The player

**One player. No character select, no classes, no unlockable heroes.**

It is a `GameObject` carrying `Health`, `PlayerStats`, `AutoShoot`,
`LevelSystem`, `AbilitySystem` and `DraftController`. It has a turret child that
rotates to face its target, and a ring on the ground showing its radius.

### The stat block (all 15 stats — BUILT)

| Stat | Base | Read by |
|---|---|---|
| `MaxHP` | 100 | Health |
| `Damage` | 10 | Every damage source |
| `FireRate` | 3.0 /sec | AutoShoot |
| `Range` | 9.0 m | **The radius.** AutoShoot, RangeIndicator |
| `ProjectileSpeed` | 30 m/s | Projectile |
| `ProjectileCount` | 1 | AutoShoot (fractions roll for the extra round) |
| `Pierce` | 0 | Projectile |
| `CritChance` | 0.05 | DamageDealer, rolled **per round** |
| `CritMultiplier` | 2.0 | DamageDealer |
| `Lifesteal` | 0 | Projectile, heals at impact |
| `PickupRadius` | 1.5 | XPGemService magnet |
| `CooldownReduction` | 0 | AbilitySystem |
| `Armor` | 0 | **Declared, nothing reads it** |
| `Luck` | 0 | DraftController novelty bias only |
| `MoveSpeed` | — | **Reserved. The player does not move** |

Modifiers are flat or percent, and **percent bonuses are additive with each
other on purpose** — multiplicative stacking makes the economy explode by
stage 6 and cannot be walked back after launch.

---

## 4. Weapons

**The system is BUILT. The roster is not.**

`WeaponDefinition` is a ScriptableObject holding *multipliers over the player's
stat sheet*, never absolute numbers — so a pistol and a minigun scale off one
upgrade tree instead of needing parallel balance passes.

What a weapon controls: damage / fire-rate / range / projectile-speed
multipliers, extra projectiles, spread cone, inaccuracy, homing steer rate,
hit radius, projectile lifetime, and which projectile prefab it uses.

**Weapon assets authored so far: one** (`Weapon_Pistol`, all multipliers at 1).

The system already supports a shotgun (high `extraProjectiles`, wide
`spreadDegrees`), a minigun (high fire rate, high inaccuracy), a railgun (high
pierce, slow fire) with **no code changes** — they are just assets nobody has
created yet.

There is no weapon switching, no weapon unlocking, and no weapon UI.

---

## 5. Zombies

**One behaviour is coded: walk at the player, attack when in reach.**

`EnemyMovement` handles chase, decaying knockback, sampled crowd separation, and
a melee attack on an interval. It is transform-driven with no Rigidbody — 150
rigidbodies is roughly the difference between 60 and 25 fps on mid-range
Android.

### Specced for M0/M1 — three assets sharing one prefab, differing only by stats

| Type | HP | Speed | Note |
|---|---|---|---|
| Shambler | 20 | 2.5 | Filler |
| Runner | 10 | 4.5 | Punishes low fire rate |
| Brute | 90 | 1.4 | Knockback-immune |

`EnemyBehaviour` also declares **Spitter**, which needs ranged-attack code that
does not exist yet.

### DESIGNED but not built — five more

Spitter (outranges you), Bomber (explodes on death), Swarm (packs of 12), Elite
(buffs neighbours), Boss (multi-phase).

The design rule: **each type exists to make one upgrade line matter.** Spitters
sell range, Runners sell fire rate, Swarms sell pierce, Brutes sell raw damage.
An enemy that sells no upgrade is just more of the same enemy.

Scaling is `hp = baseHP * 1.12^wave * stageMultiplier`.

---

## 6. Abilities

**8 specced for M1 — 3 active, 5 passive.** The design calls for 25-30 at v1.

Each has 5 levels. On level-up you are offered 3 cards.

### Actives — effect code BUILT

| Ability | What it does |
|---|---|
| **Shockwave** | Instant damage + knockback around you. The panic button |
| **Frag Grenade** | Lobs at the **densest cluster**, not the nearest zombie, with a visible fuse |
| **Chain Lightning** | Arcs between zombies, damage decaying per hop |

Actives fire automatically on a cooldown. An `AbilityEffect` is an abstract
ScriptableObject, so **a new active is one asset plus a small class** — never a
change to `AbilitySystem`.

### Passives — pure stat modifiers

`+15% damage`, `+12% fire rate`, `+1 pierce`, `+0.5 rounds/shot`, `+1 m radius`.

`+1 m radius` is deliberately the **rarest card in the pool** (draft weight 0.4
against 1.2 for damage). Radius compounds with every other stat and is the one
upgrade a player can literally see working; common range cards flatten the
entire difficulty curve.

### Draft rules — BUILT

- Maxed abilities are never offered.
- If you hold no active by your second draft, one is **forced** into the offer —
  a run with only `+damage` cards has nothing happening in it.
- Distinct abilities are capped (set 6). Without a cap you end every run holding
  everything and no two runs differ.
- Level-ups **queue**: one big gem can grant two levels, and each owes a draft.
- `Luck` biases slightly toward abilities you do not yet own.

### Evolutions — DESIGNED, hooks only

`evolvesInto` / `evolutionRequires` exist on `AbilityDefinition` and the draft
already skips assets flagged `isEvolution`, but **nothing unlocks them**. M3.

---

## 7. XP system — BUILT

The full chain works:

```
zombie dies
  -> XPGemService.Drop() pools a gem at the corpse
  -> gem scatters briefly with decaying velocity
  -> once inside PickupRadius it LATCHES and accelerates in
     (latching, not per-frame: a gem already flying must never stall)
  -> on contact, LevelSystem.AddXP(value)
  -> while (xp >= needed) { level++; raise LevelledUp }
  -> DraftController queues a draft per level
  -> TimeController.PushPause(), three cards appear
```

**Curve:** `cost(n) = 5 + 8n + 0.5n²`, soft-capped at level 40 so long runs do
not stall completely. Target: first draft within 25 seconds of run 1.

**Gem cap:** 200 concurrent. Past that, value is folded into a live gem rather
than dropped — XP is never silently lost, the drop just is not rendered.

---

## 8. Energy system — NOT PLANNED, deliberately

**There is no energy or stamina system, and the design argues against adding
one** (`docs/01-game-design.md` §9).

Energy caps session length in a genre that lives on long sessions. It is the
single most common reason clones of this game fail their soft launch: it
throttles exactly the players who are most engaged, and the revenue it protects
is smaller than the retention it costs.

**Progress is gated by stage difficulty instead** — you replay a stage because
you are not strong enough for it yet, not because a meter is empty.

If you want energy anyway, say so and I will spec it — but I would push back
once first.

---

## 9. Skill tree — NOT PLANNED

**There is no skill tree in this design.** Nothing in the code or the docs
implements one. Progression is split across two layers instead:

| Layer | Lifetime | Status |
|---|---|---|
| **Ability draft** | One run. Reset every run | **BUILT** |
| **Base rooms** | Permanent, across all runs | **DESIGNED**, zero code |

The base rooms are the closest thing to a permanent upgrade tree:

| Room | Effect | Cost curve |
|---|---|---|
| Armoury | +2% damage/level | `100 * 1.18^n` |
| Infirmary | +2% max HP/level | `100 * 1.18^n` |
| Radar | +0.15 m radius/level | `150 * 1.20^n` |
| Workshop | +1.5% fire rate/level | `150 * 1.20^n` |
| Vault | Offline coin income | `200 * 1.22^n` |
| Forge / Lab | Unlock gear tiers, ability rerolls | Milestone |

That is a **flat list of upgradeable rooms**, not a branching tree — no
prerequisites, no mutually exclusive paths, no respec. It is the standard shape
for this genre because it is legible on a phone screen.

If you want a real branching skill tree with exclusive paths, that is a design
change, not a missing feature. Worth deciding before the meta layer is built,
because it changes the save format.

---

## 10. Rewards — DESIGNED, zero code

**Nothing pays out yet. The player dies and nothing happens** — no screen, no
currency, no drop, no save.

The full designed economy (`docs/01-game-design.md` §7-9):

| Currency | Earned from | Spent on |
|---|---|---|
| Coins | Runs, offline Vault | Base rooms, gear levels |
| Gems | IAP, first clears, dailies | Chests, revives, speedups |
| Scrap | Salvaging gear | Gear upgrades |

**Gear:** 6 slots (Weapon, Barrel, Magazine, Scope, Armour, Rig), 5 rarities,
random affixes, level-up with coins + scrap, fuse 3 duplicates into the next
rarity.

**Offline income:** the Vault accrues coins while away, capped at 8 hours — that
cap is what forces the daily return.

Failed runs must still pay out, at a reduced rate. Never send a player away
empty-handed.

---

## 11. What is missing before this is a game

In the order it should be built:

1. **Run lifecycle** — waves from a timeline, a boss, and win/lose screens.
   Right now a run has no beginning, no end, and no consequence.
2. **Rewards + save** — currencies, a run payout, and persistence. Without
   these, closing the app erases everything and there is no reason to return.
3. **Gear + base rooms** — the permanent progression layer.
4. **Content** — more zombies, more abilities, more weapons, more stages.
5. **Business layer** — ads, IAP, analytics (M4).

Step 1 is the gap that matters most. Everything after it is meaningless until a
run can be won or lost.

## 12. One open design risk

With a stationary player and fully automatic fire, **a run has no
moment-to-moment input** — the draft is the entire interaction. That makes this
an idle defence game rather than an action roguelite. That is a legitimate genre
and it fits the idle base meta, but it must be designed for deliberately.

If playtesters describe runs as *watching* rather than *playing*, the fix is one
manually triggered ability on a cooldown — a grenade button, a barricade — not
more passive content. One button converts watching into playing.
