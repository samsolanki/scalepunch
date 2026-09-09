# ScalePunch — Game Design Document

A survivors-like action roguelite with an idle base meta, in the mould of
*Endless Puncher* (Rollic Games). This document specifies **what** the game is.
See `03-roadmap.md` for the order in which to build it.

---

## 1. The loop

Three nested loops. Getting all three right is the whole game; everything else
is content.

```
CORE (3-5 min)      Punch waves -> collect XP -> draft 1 of 3 abilities
                    -> get stronger mid-run -> beat the boss or die

META (per session)  Spend run rewards -> upgrade gear + base rooms
                    -> permanent power -> clear the next stage

RETENTION (daily)   Idle vault income -> daily missions -> events
                    -> log back in
```

The player must feel **stronger every 20 seconds** inside a run, and **stronger
every session** outside it. If either breaks, retention dies.

---

## 2. Core combat

**The player is a fixed emplacement.** It does not move. There is no joystick,
no fire button, and no aiming input. The player stands their ground and the
weapon engages automatically.

**Camera:** top-down 3/4 view, fixed rotation, static over the player.

**The radius is the game.** The player has an engagement radius drawn on the
ground. Any zombie that crosses into it is acquired, tracked, and shot until it
dies or the player does. Everything the player buys — range, fire rate, damage,
pierce, multishot — is read through what happens at that ring.

```
        . - - - - - - .          zombies converge from every direction
      .                 .
    .     [ RADIUS ]      .      cross the ring -> acquired -> fired on
   .          @            .     @ = player, rotates to face its target
    .                     .
      .                 .
        ' - - - - - - '
```

**Firing sequence, every shot:**

1. **Acquire** — pick a target inside the radius by the active priority
   (closest by default; most-advanced, weakest and toughest are the others).
2. **Hold** — keep that target until it dies or leaves the radius. Re-picking
   every frame makes the turret twitch between equidistant zombies.
3. **Slew** — rotate toward it at a fixed turn rate. Do not fire while the aim
   error is outside the firing arc.
4. **Fire** — spawn N rounds across the weapon's spread cone, crit rolled
   **per round** so a shotgun blast can crit two pellets out of six.
5. **Travel** — rounds fly, steering gently toward their target so aim error
   against runners doesn't read as the gun being broken. They expire at the
   edge of the radius, so the ring means exactly what it shows.

**Feel checklist** — this is what separates a shipped game from a prototype:

| Element | Spec |
|---|---|
| Muzzle flash | 2-3 frames at the barrel, plus a light |
| Recoil | Small turret kick back along the barrel, 0.06 s |
| Fire shake | ~0.025 amplitude. **Much** smaller than impact shake — at 3+ shots/sec it is constant, and more than this is nausea |
| Tracer | Trail renderer on the round; the eye needs to see the shot travel |
| Hitstop | 40-70 ms, scaled by damage |
| Knockback | Small stagger on impact, ~1.6 units. Brutes immune, or sustained fire stunlocks them at the ring |
| Hit flash | Zombie flashes white for 0.08 s |
| Damage numbers | Pooled, arc upward, crits bigger and a different colour |
| Death | Ragdoll or squash-and-pop, plus a blood burst |
| Impact decal | Optional, pooled, faded — good on Brutes |

Budget real time for this. A mechanically identical game with and without juice
tests 2-3x apart on D1 retention.

### 2.1 What carries engagement without moment-to-moment input

Fully automatic combat with a stationary player means there is no execution
skill during a run — this is an **idle/incremental defence game**, not an
action roguelite, and it should be designed as one. The engagement has to come
from decisions rather than reflexes:

- **The level-up draft** (§4) is now the only in-run interaction. It carries
  the whole run and must be excellent.
- **Target priority** is a real tactical choice once Brutes and Spitters exist.
  Expose it as a HUD toggle, not a settings-menu option.
- **Watching the ring hold or break** is the tension. Waves must visibly
  threaten to overwhelm the radius, or nothing is at stake.
- **Between-run upgrades** (§7, §8) do the heavy lifting on retention.

If playtesters describe runs as "watching", add an active ability on a manual
cooldown — a grenade or a wall — before adding more content. One button is
enough to convert watching into playing.

## 3. Stats

The full stat block. Every ability, gear piece, and base room modifies one of
these — nothing else.

| Stat | Base | Notes |
|---|---|---|
| `maxHP` | 100 | |
| `damage` | 10 | Per round, before the weapon multiplier |
| `fireRate` | 3.0 | Shots/sec, before the weapon multiplier |
| `range` | 9.0 | **The radius.** Metres |
| `projectileSpeed` | 30 | m/s |
| `projectileCount` | 1 | Rounds per shot; fractions roll for the extra |
| `pierce` | 0 | Extra zombies each round passes through |
| `critChance` | 0.05 | Rolled per round |
| `critMultiplier` | 2.0 | |
| `armor` | 0 | Flat reduction, then % |
| `lifesteal` | 0 | Fraction of damage dealt |
| `pickupRadius` | 1.5 | XP magnet |
| `cooldownReduction` | 0 | Ability cooldowns |
| `luck` | 0 | Biases draft rarity + drop rolls |

A **weapon** is a set of multipliers over this sheet, never a set of absolute
numbers — so a pistol and a minigun scale off one upgrade tree instead of
needing parallel balance passes.

**Damage formula:**

```
raw   = (damage + gearFlat) * (1 + sum(percentBonuses)) * weapon.damageMult
crit  = roll(critChance) ? critMultiplier : 1
final = max(1, raw * crit - target.armor)
```

Keep additive percent bonuses additive. Multiplicative stacking makes the
economy explode by stage 6 and is very hard to walk back later.

---

## 4. Abilities (the run draft)

On level-up, pause and present **3 cards**. This screen is the game's
monetization anchor (see `04-monetization-and-launch.md`).

Roughly 25-30 abilities for v1, in three classes:

### 4.1 Actives — auto-firing secondary attacks
Each has its own cooldown and levels 1-5.

| Ability | L1 effect | Scaling |
|---|---|---|
| Frag Grenade | Lobbed AoE at the densest cluster, 6 s cd | +dmg, +radius |
| Sentry Turret | Second auto-firing gun, 20 s duration | +duration, +dmg |
| Flamethrower | Cone of burning damage over time | +cone, +burn |
| Shockwave | Radial knockback + damage, clears the ring | +dmg, +radius |
| Barbed Wire | Slows zombies crossing the ring | +slow %, +dmg |
| Chain Lightning | Arcs between 3 zombies on hit | +chains |
| Airstrike | Periodic bombardment of the far edge | +bombs |

### 4.2 Passives — stat modifiers
`+15% damage`, `+10% fire rate`, `+1 m radius`, `+20 max HP`, `+5% crit`,
`+1 pierce`, `+0.5 projectiles`, `+20% projectile speed`, `+1 m pickup radius`,
`+3% lifesteal`, regen.

`+1 m radius` is the most valuable passive in the game and should be rare —
it is the one upgrade the player can literally see working.

### 4.3 Evolutions — the retention hook
Max an active (L5) **and** hold a specific passive → the card pool offers a
one-time evolved version.

```
Sentry L5      + Fire Rate          -> Gun Emplacement (two permanent sentries)
Flamethrower L5 + Pierce            -> Napalm Ring     (the radius itself burns)
Chain Lightning L5 + Crit           -> Tesla Coil      (crits arc to everything)
```

Evolutions are what make players replay the same stage. Ship at least 5.

**Draft rules:** never offer maxed abilities; weight toward completing an
evolution once the prerequisite is held; guarantee at least one active in the
first two drafts so runs don't stall.

---

## 5. Enemies

| Type | Behaviour | Role |
|---|---|---|
| Shambler | Walks straight at the player | Filler, XP |
| Runner | Fast, low HP | Punishes low fire rate — crosses the ring before it dies |
| Brute | Slow, high HP, knockback-immune | DPS check |
| Spitter | Stops at the ring's edge and shoots back | Punishes low range |
| Bomber | Explodes on death near the player | Punishes letting them close |
| Swarm | Spawns in packs of 12 | Rewards pierce and multishot builds |
| Elite | Buffs nearby zombies, aura | Priority-targeting choice |
| Boss | Multi-phase, telegraphed attacks | Stage gate |

Each type exists to make one upgrade line matter. Spitters that outrange you
sell range; Runners sell fire rate; Swarms sell pierce; Brutes sell raw damage.
An enemy that does not sell an upgrade is just more of the same enemy.

**Scaling:**
```
hp(wave)     = baseHP  * pow(1.12, wave) * stageMultiplier
damage(wave) = baseDmg * pow(1.08, wave) * stageMultiplier
```

**Spawning:** ring spawn just outside camera bounds, never in view, and always
outside the engagement radius — a zombie that spawns already inside the ring
robs the player of the approach, which is the only tension the game has.
Waves defined as ScriptableObject timelines: `{time, enemyType, count, pattern}`.
Hard-cap concurrent enemies (~150 on mobile) and pool everything.

---

## 6. Stage structure

- A **run** is one stand: ~4 minutes, 8-10 waves, boss at the end. The player
  holds one position for the whole run.
- Stages are grouped into **chapters** of 10, each with a theme and a boss.
- Clearing a stage unlocks the next and grants a first-clear bonus.
- Failed runs still pay out (reduced) — never send a player away empty-handed.

Difficulty comes from the stage multiplier, not from new mechanics, so content
is cheap to produce once the systems exist.

---

## 7. Gear

Six slots, five rarities.

**Slots:** Weapon (damage), Barrel (range), Magazine (fire rate),
Scope (crit), Armour (max HP), Rig (cooldown reduction).

**Rarities:** Common → Rare → Epic → Legendary → Mythic.
Each rarity raises the stat roll range and adds one extra random affix.

**Acquisition:** boss drops, chests, ad-gated free chest, shop.

**Upgrading:** two systems, both standard and both effective:
- **Level up** with coins + Scrap. Cost `base * pow(1.15, level)`.
- **Fuse** three duplicates of the same rarity into one of the next rarity.

Duplicates must always be useful, or drops feel like nothing.

---

## 8. The base (idle meta)

The player's "home" — a small hub with upgradeable rooms. This is what
distinguishes the game from a pure survivors-like and drives daily returns.

| Room | Effect | Cost curve |
|---|---|---|
| Armoury | +2% damage per level | `100 * 1.18^n` |
| Infirmary | +2% max HP per level | `100 * 1.18^n` |
| Radar | +0.15 m radius per level | `150 * 1.20^n` |
| Workshop | +1.5% fire rate per level | `150 * 1.20^n` |
| Vault | +coins/hour while offline | `200 * 1.22^n` |
| Forge | Unlocks gear tiers, cheaper upgrades | Milestone |
| Lab | Ability reroll charges, unlock new cards | Milestone |

Radar is the room to watch in tuning: radius compounds with every other stat,
so its cost curve must be steeper than it looks like it should be.

**Offline income:** Vault accrues coins while away, capped at 8 hours (this cap
is what forces the daily return). Show the collect screen on launch with a
"double it" ad button.

---

## 9. Currencies

| Currency | Source | Sink |
|---|---|---|
| Coins | Runs, Vault | Base rooms, gear levels |
| Gems | IAP, first clears, dailies | Chests, revives, speedups |
| Scrap | Salvaging gear | Gear upgrades |

**Do not add an energy system.** It caps session length in a genre that lives
on long sessions, and it is the single most common reason clones of this game
fail their soft launch. Gate progress with stage difficulty instead.

---

## 10. Progression pacing targets

| Metric | Target |
|---|---|
| First run starts within | 15 s of first launch |
| First zombie enters the radius | 4 s into run 1 |
| First ability draft | 25 s into run 1 |
| First gear drop | End of run 1 |
| Base unlocked | After run 2 |
| Run length | 3-5 min |
| Session length | 8-12 min |
| Sessions/day | 3-5 |

Tutorial: zero pop-ups. Drop the player straight into a stand, let the gun kill
the first zombie for them, and reveal one system per run for the first four
runs.
