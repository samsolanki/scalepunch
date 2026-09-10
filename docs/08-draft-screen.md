# The Draft Screen

The level-up card screen: what triggers it, everything it displays, and the
rarity table.

---

## 1. When it appears

**On kill count**, not on collected XP. `LevelCurve.source` defaults to `Kills`,
so a threshold means literally that many zombies.

| Draft | Total kills | Kills since last |
|---|---|---|
| 1st | **5** | 5 |
| 2nd | **15** | 10 |
| 3rd | 30 | 15 |
| 4th | 50 | 20 |
| 5th | 75 | 25 |
| 6th | 105 | 30 |
| 7th | 140 | 35 |
| 8th | 180 | 40 |
| 9th | 225 | 45 |
| 10th | 275 | 50 |
| 11th+ | formula | `5 + 8n + 0.5n²` |

Authored as **cumulative totals** on `LevelCurve.cumulativeThresholds`, because
that is how pacing is actually reasoned about — "first card at 5, second at 15" —
rather than as increments you have to add up in your head. Past the list the
quadratic formula extends it so there is no cliff where hand-authoring stops.

The run **pauses** while a draft is open (`TimeController.PushPause`). Level-ups
queue, so a kill that crosses two thresholds at once gives two drafts in
sequence, not one.

**XP gems do not spawn in Kills mode.** Pickups that grant nothing teach the
player the wrong model of where progress comes from. Set
`LevelCurve.source = XPGems` to bring them back, at the cost of the thresholds
no longer being kill counts.

---

## 2. Everything a card shows

Six elements, each earning its place:

| # | Element | Example | Notes |
|---|---|---|---|
| 1 | **Rarity band** | `EPIC` | Top of the card, coloured by tier |
| 2 | **Icon** | — | In a frame tinted the rarity colour |
| 3 | **Name** | `Fire Rate` | |
| 4 | **Level** | `NEW` / `Lv 3` | "NEW" reads far more strongly than "Lv 1" — it says at a glance which cards widen the build and which deepen it |
| 5 | **Description** | "Increases your attacks per second." | |
| 6 | **Before → after** | `3.0/s → 3.5/s` | The row players actually read |

Plus two treatments driven by the roll:
- The card **body is tinted** toward the rarity colour (22% — full saturation
  makes text unreadable).
- **Epic and Legendary glow.** Nothing below does.

### The before → after row

Computed from the live stat sheet, never hand-written. Hand-authored effect text
goes stale the moment a number changes, and it cannot show the player their
*own* current value — which is the entire reason the row is worth showing.

- **Passives** preview the first modifier's stat: current value → value after
  the card, with the rarity multiplier already applied.
  `StatSheet.Preview()` does this exactly rather than approximating, because a
  flat bonus does not simply add once percent bonuses exist — it goes inside the
  same `(base + flat) × (1 + percent)` as everything else.
- **Actives** preview damage per activation. A brand-new active has no before,
  so it shows `— → 34`.
- The "after" value is **always green**. Every card is an upgrade, and colouring
  the result is what makes the row scan in under a second.

Stat formatting is centralised in `StatFormat` so a card saying `5%` can never
disagree with a panel saying `0.05`:

| Stat | Reads as |
|---|---|
| Crit Chance, Lifesteal, Cooldown Reduction | `12.5%` |
| Crit Damage | `2.5x` |
| Fire Rate | `3.5/s` |
| Range, Pickup Radius | `9.5 m` |
| Bullet Speed | `45 m/s` |
| Max Health, Damage, Armour | `120` |
| Pierce, Rounds per Shot | `1.5` |

---

## 3. Rarity

**Rarity is rolled per card, not stored per ability.** That is the point: the
same ability offered twice should feel different. `+15% damage (Common)` and
`+38% damage (Epic)` are the same card doing very different work, and without
that second axis a pool of eight abilities goes stale in about three runs.

| Tier | Weight | Chance | Magnitude | Colour | Glow |
|---|---|---|---|---|---|
| **Common** | 50 | 50% | ×1.00 | Grey | — |
| **Uncommon** | 26 | 26% | ×1.35 | Green | — |
| **Rare** | 14 | 14% | ×1.80 | Blue | — |
| **Epic** | 7 | 7% | ×2.50 | Magenta | ✓ |
| **Legendary** | 3 | 3% | ×3.50 | Gold | ✓ |

Chances are at **Luck 0**. Magnitude multiplies the ability's authored value, so
a `+15% damage` passive rolling Legendary grants `+52.5%`.

**Rarity scales magnitude only — never cooldowns.** A Legendary that also fired
twice as often would compound two multipliers into a number nobody balanced.

### Luck

`Luck` finally does what the design always said it would (docs/01 §3): it shifts
weight up the table. Higher tiers gain more from it than lower ones, so Luck
raises the ceiling rather than nudging everything evenly:

```
weight(tier) = baseWeight × (1 + luck × tierIndex × 0.35)
```

At Luck 3, Legendary weight roughly triples while Common is untouched.

### Taking the same ability twice

An instance remembers the **best** magnitude it was ever taken at, not the
latest. Taking a Legendary and then a Common of the same ability must never be a
downgrade, or the draft starts punishing the player for levelling something up.

---

## 4. Offer rules

- Maxed abilities are never offered.
- If the player holds no active by the second draft, one is **forced** into the
  offer — a run of nothing but `+damage` cards has nothing happening in it.
- Distinct abilities are capped (`AbilityLibrary.maxDistinctAbilities`, 6).
  Without a cap every run ends holding everything and no two runs differ.
- Never fewer than three cards; the fallback fills any gap. A short offer reads
  as a bug.
- Luck also biases slightly toward abilities the player does not yet own.

---

## 5. Not built

Two things visible in the reference game's version of this screen that we do not
have:

- **The stage track** across the top — a kill-count progress bar with the boss at
  its far end, plus feature-unlock milestones (Talents, Inventory, Dungeons)
  gated along it. We show a wave counter instead.
- **Ability slots** along the bottom — four, with three locked behind later
  stages. We cap distinct abilities as a number rather than as visible slots.

Both are HUD/meta features rather than draft features, so they belong with the
meta layer at P6.
