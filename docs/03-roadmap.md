# Build Roadmap

Build **vertically, not horizontally**. A complete but ugly 60-second loop
beats a beautiful menu system attached to nothing. Every milestone below ends
in something you can put on a phone and play.

Timings assume one developer working steadily. Halve them for a small team,
double them if you are learning Unity as you go.

---

## M0 — Punch something (3-5 days)

**Goal:** a grey box that punches other grey boxes and it feels good.

- [ ] Unity project, URP mobile, git + `.gitignore` (already committed here)
- [ ] Ground plane, capsule player, top-down camera follow
- [ ] Floating virtual joystick movement
- [ ] Auto-attack: find nearest enemy in range, swing on a timer
- [ ] `Health` component, damage, death
- [ ] One enemy type that walks at the player
- [ ] Simple spawner: N enemies every M seconds
- [ ] Hitstop, knockback, hit flash, pooled damage numbers

**Exit test:** you punch 30 capsules on your phone and it is *satisfying* with
zero art. If it is not fun here, no amount of content fixes it. Stop and
retune attack speed, range, knockback, and hitstop until it is.

---

## M1 — A complete run (2-3 weeks)

**Goal:** the core loop, start to finish, repeatable.

- [ ] XP gems drop on death, magnet pickup, level curve
- [ ] Level-up pause + 3-card draft UI
- [ ] `AbilityDefinition` ScriptableObjects; **8 abilities** (4 active, 4 passive)
- [ ] Ability runtime: cooldowns, levels, stat recalculation
- [ ] Wave timeline from `WaveDefinition`; 8 waves over ~4 min
- [ ] 4 enemy types (Grunt, Runner, Brute, Spitter)
- [ ] One boss with two phases and telegraphed attacks
- [ ] Victory + defeat screens with a reward summary
- [ ] Object pooling pass across enemies, gems, VFX, numbers

**Exit test:** you play five runs back to back voluntarily and take different
builds. Get playtesters on it — you have stopped being able to judge it.

---

## M2 — Reasons to come back (3-4 weeks)

**Goal:** progression that survives the app being closed.

- [ ] Currency service (coins, gems, scrap)
- [ ] Save/load with versioning + migrations
- [ ] Gear: definitions, drop tables, 6 slots, 5 rarities, random affixes
- [ ] Inventory + equip UI, stat comparison, salvage to scrap
- [ ] Gear levelling and duplicate fusion
- [ ] Base scene with 6 upgradeable rooms feeding permanent stat bonuses
- [ ] Offline income from the Vault, 8-hour cap, collect-on-launch screen
- [ ] Stage select map; stage multipliers; first-clear rewards
- [ ] Meta ↔ Run scene flow, cleanly unloading run state

**Exit test:** a tester plays across three days and can articulate what they
are saving up for.

---

## M3 — Content and juice (3-4 weeks)

**Goal:** it stops looking like a prototype.

- [ ] Abilities to 25-30, including **5 evolutions**
- [ ] Enemies to 8 types + 3 bosses
- [ ] 30 stages across 3 themed chapters
- [ ] Replace grey boxes: low-poly character, enemies, environments
- [ ] Full VFX pass — impacts, trails, level-up burst, boss telegraphs
- [ ] Audio: punch layers with pitch variation, music, UI clicks, boss stings
- [ ] UI art pass, DOTween transitions everywhere, no instant screen swaps
- [ ] Onboarding: first four runs each reveal one system, no pop-up tutorials
- [ ] Settings, credits, privacy policy link

**Exit test:** 60 fps with 150 enemies on a mid-range Android from 2022.

---

## M4 — Business layer (1-2 weeks)

**Goal:** it can make money and you can see what players do.

- [ ] Ad mediation (AppLovin MAX or LevelPlay) + all placements from `04`
- [ ] Unity IAP: remove-ads, starter pack, gem packs, receipt validation
- [ ] Analytics events (see `04-monetization-and-launch.md` §4)
- [ ] Remote config for every balance number that matters
- [ ] Crashlytics
- [ ] Daily missions + login rewards
- [ ] GDPR/ATT consent flow — required, and a store rejection if missing

---

## M5 — Soft launch and tune (4-8 weeks)

**Goal:** find out whether the game actually retains before you spend on it.

- [ ] Release in 2-3 small English-speaking markets (Philippines, Canada)
- [ ] Buy ~$500-1000 of installs to get statistically usable cohorts
- [ ] Read D1/D7 retention, session length, funnel drop-off, ARPDAU
- [ ] Tune the economy through remote config, not new builds
- [ ] Iterate until D1 ≥ 35% and D7 ≥ 12%

**Do not skip this.** Launching globally on an untuned economy burns the
listing, and you only get one first-impression cohort.

---

## M6 — Global launch

- [ ] Store listing: icon, 5-8 screenshots, 30 s video, A/B tested
- [ ] ASO pass on title, short description, keywords
- [ ] Localise to 8-10 languages (the store listing first, then the game)
- [ ] Content roadmap for the first 90 days — an idle game with no updates
      churns out within a month

---

## Realistic totals

| Path | Time to shippable v1 |
|---|---|
| Solo, experienced in Unity | 3-4 months |
| Solo, learning Unity | 8-12 months |
| Small team (2 dev, 1 artist) | 2-3 months |

Buy art from the Asset Store for v1. Custom art is the single biggest schedule
risk on a project like this and it is not what determines whether the game
retains.
