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

**Camera:** top-down 3/4 view, fixed rotation, follows player with slight lead.

**Movement:** floating virtual joystick (touch anywhere on the left half).
Player never stops moving in practice — the game is kiting.

**Attacking is automatic.** The player only controls position. This is
non-negotiable for the genre; manual attack buttons kill session length on
mobile.

- Auto-punch targets the **nearest enemy within range**.
- Punch = short cone/sphere overlap, not a projectile.
- Fixed attack interval driven by `attackSpeed` stat.

**Feel checklist** (this is what separates a shipped game from a prototype):

| Element | Spec |
|---|---|
| Hitstop | 40-70 ms freeze on hit, scaled by damage |
| Knockback | Impulse away from player, 0.15 s, decays |
| Screen shake | 0.1 amplitude on normal hit, 0.4 on boss hit |
| Damage numbers | Pooled, arc upward, crit = bigger + different colour |
| Hit flash | Enemy material flashes white for 0.08 s |
| Death | Ragdoll or squash-and-pop + coin/XP burst |
| Controller rumble | Light on hit, heavy on crit |

Budget real time for this. A mechanically identical game with and without
juice tests 2-3x apart on D1 retention.

---

## 3. Stats

The full stat block. Every ability, gear piece, and base room modifies one of
these — nothing else.

| Stat | Base | Notes |
|---|---|---|
| `maxHP` | 100 | |
| `damage` | 10 | Per punch |
| `attackSpeed` | 1.0 | Attacks/sec |
| `attackRange` | 2.0 | Metres |
| `moveSpeed` | 4.0 | m/s |
| `critChance` | 0.05 | |
| `critMultiplier` | 2.0 | |
| `armor` | 0 | Flat reduction, then % |
| `lifesteal` | 0 | Fraction of damage dealt |
| `pickupRadius` | 1.5 | XP magnet |
| `cooldownReduction` | 0 | Ability cooldowns |
| `luck` | 0 | Biases draft rarity + drop rolls |

**Damage formula:**

```
raw   = (damage + gearFlat) * (1 + sum(percentBonuses))
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
| Shockwave | Radial burst, 15 dmg, 3 s cd | +dmg, +radius |
| Uppercut | Launches nearest enemy, 30 dmg | +dmg, +targets |
| Spin Kick | 360° sweep on a timer | +dmg, +duration |
| Glove Toss | Homing projectile | +count, +pierce |
| Ground Slam | Damage + slow in a circle | +slow %, +radius |
| Lightning Fist | Chains to 3 enemies | +chains |
| Shadow Clone | Mirrors your punches at 40% | +clones |

### 4.2 Passives — stat modifiers
`+15% damage`, `+10% attack speed`, `+20 max HP`, `+5% crit`,
`+1 m pickup radius`, `+3% lifesteal`, `+10% move speed`, thorns, regen.

### 4.3 Evolutions — the retention hook
Max an active (L5) **and** hold a specific passive → the card pool offers a
one-time evolved version.

```
Shockwave L5   + Crit passive       -> Seismic Fist   (crits cause aftershocks)
Glove Toss L5  + Attack Speed       -> Gatling Gloves (continuous stream)
Lightning L5   + Cooldown Reduction -> Storm Aura     (permanent chain aura)
```

Evolutions are what make players replay the same stage. Ship at least 5.

**Draft rules:** never offer maxed abilities; weight toward completing an
evolution once the prerequisite is held; guarantee at least one active in the
first two drafts so runs don't stall.

---

## 5. Enemies

| Type | Behaviour | Role |
|---|---|---|
| Grunt | Walks straight at player | Filler, XP |
| Runner | Fast, low HP | Punishes standing still |
| Brute | Slow, high HP, knockback-immune | DPS check |
| Spitter | Ranged, keeps distance | Punishes pure kiting |
| Bomber | Explodes on death | Punishes melee hugging |
| Swarmling | Spawns in packs of 12 | Rewards AoE builds |
| Elite | Buffs nearby enemies, aura | Priority target |
| Boss | Multi-phase, telegraphed attacks | Stage gate |

**Scaling:**
```
hp(wave)     = baseHP  * pow(1.12, wave) * stageMultiplier
damage(wave) = baseDmg * pow(1.08, wave) * stageMultiplier
```

**Spawning:** ring spawn just outside camera bounds, never in view.
Waves defined as ScriptableObject timelines: `{time, enemyType, count, pattern}`.
Hard-cap concurrent enemies (~150 on mobile) and pool everything.

---

## 6. Stage structure

- A **run** is one stage attempt: ~4 minutes, 8-10 waves, boss at the end.
- Stages are grouped into **chapters** of 10, each with a theme and a boss.
- Clearing a stage unlocks the next and grants a first-clear bonus.
- Failed runs still pay out (reduced) — never send a player away empty-handed.

Difficulty comes from the stage multiplier, not from new mechanics, so content
is cheap to produce once the systems exist.

---

## 7. Gear

Six slots, five rarities.

**Slots:** Gloves (damage), Shoes (move speed), Belt (max HP), Ring (crit),
Amulet (cooldown reduction), Headgear (armor).

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
| Gym | +2% damage per level | `100 * 1.18^n` |
| Kitchen | +2% max HP per level | `100 * 1.18^n` |
| Track | +1% move speed per level | `150 * 1.20^n` |
| Vault | +coins/hour while offline | `200 * 1.22^n` |
| Forge | Unlocks gear tiers, cheaper upgrades | Milestone |
| Lab | Ability reroll charges, unlock new cards | Milestone |

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
| First ability draft | 25 s into run 1 |
| First gear drop | End of run 1 |
| Base unlocked | After run 2 |
| Run length | 3-5 min |
| Session length | 8-12 min |
| Sessions/day | 3-5 |

Tutorial: zero pop-ups. Drop the player into a punching run immediately and
reveal one system per run for the first four runs.
