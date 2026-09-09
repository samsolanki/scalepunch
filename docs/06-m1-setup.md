# M1 Setup — XP, the level-up draft, and target priority

Builds on the `Run` scene from [`05-m0-setup.md`](05-m0-setup.md). Same rule as
before: the scripts are complete, the editor pass is manual.

> **Fast path:** run **ScalePunch ▸ Build Run Scene** from the menu bar. It
> creates every asset, prefab and GameObject below and wires all ~70 references
> automatically, then saves the scene to `Assets/_Project/Scenes/Run.unity`.
> It is re-runnable and preserves asset GUIDs, so rebuild any time something
> comes unwired. The rest of this document explains what it builds and, more
> importantly, **what to tune once it runs**.

**What this adds:** zombies drop XP, XP levels you up, a level-up pauses the run
and offers three cards, and the HUD carries an XP bar plus a target-priority
toggle. With a stationary player and automatic fire, **the draft is the only
interaction a run has** — treat it as the main event, not a menu.

---

## 1. Time is now centrally owned

`Hitstop` is gone, replaced by `Core/TimeController`. Nothing else may write
`Time.timeScale`.

The reason is a real bug: a draft opening during a hitstop had the hitstop
expire a frame later and set `timeScale` back to 1 — unpausing the run
underneath the open draft screen. Pauses are reference-counted and always beat
freezes.

**On the `Systems` object:** delete the `Hitstop` component, add
`TimeController`, and assign Tuning = `CombatTuning`.

## 2. Data assets

In `Assets/_Project/Data/`:

**Level curve** — Create ▸ ScalePunch ▸ Level Curve → `LevelCurve`.
Defaults give a first draft around 20 seconds in. Verify that on device; the
design target is 25 seconds (docs/01 §10) and it is the single most important
number in onboarding.

**Effects** — Create ▸ ScalePunch ▸ Effects ▸ …
- `Effect_RadialBurst` — Shockwave
- `Effect_Grenade` — Frag Grenade
- `Effect_ChainLightning` — Chain Lightning

**Abilities** — Create ▸ ScalePunch ▸ Ability Definition, one per row. Fill in
all five levels of each; the `description` is what the player actually decides
on, so write the number, not the adjective.

| Asset | Kind | Effect | L1 description | Weight |
|---|---|---|---|---|
| `Ability_Shockwave` | Active | RadialBurst | Blast 120% damage around you, 6 s | 1.0 |
| `Ability_Grenade` | Active | Grenade | Lob 200% damage at the largest pack, 7 s | 1.0 |
| `Ability_Lightning` | Active | ChainLightning | Arc 90% damage through 3 zombies, 4 s | 1.0 |
| `Ability_Damage` | Passive | — | +15% damage | 1.2 |
| `Ability_FireRate` | Passive | — | +12% fire rate | 1.2 |
| `Ability_Pierce` | Passive | — | +1 pierce | 0.9 |
| `Ability_Multishot` | Passive | — | +0.5 rounds per shot | 0.7 |
| `Ability_Range` | Passive | — | +1 m radius | **0.4** |

`Ability_Range` is deliberately the rarest card in the pool. Radius compounds
with every other stat and is the one upgrade the player can literally see
working — common range cards flatten the whole difficulty curve.

Passive modifiers go in the `modifiers` list per level; percent values are
fractions (`0.15` = +15%).

**Library** — Create ▸ ScalePunch ▸ Ability Library → `AbilityLibrary`.
Drop all eight in, set `maxDistinctAbilities` to 6, and set `fallback` to a
small repeatable heal (make `Ability_Patch`, Passive, `+20 max HP`, weight 0 so
it is never drafted normally).

The distinct cap is what forces a build. Without it the player ends every run
holding everything and no two runs differ.

## 3. Prefabs

**XP gem** — `Assets/_Project/Prefabs/XPGem.prefab`: a small emissive Quad or
Sphere (scale ~0.25, collider REMOVED) with the `XPGem` script.

**Draft card** — `Assets/_Project/Prefabs/DraftCard.prefab`:
```
DraftCard                 (Image background, Button)
├─ KindStripe             (Image, thin bar down one edge)
├─ Icon                   (Image)
├─ NameLabel              (TMP_Text)
├─ LevelLabel             (TMP_Text — reads "NEW" or "Lv 3")
├─ DescriptionLabel       (TMP_Text)
└─ DraftCard              (the script, on the root)
```
Cards must be big. This screen is played with a thumb, at a glance, several
times a run.

## 4. Scene additions

### On `Player`
```
Player
├─ (existing) Health, PlayerStats, AutoShoot
├─ LevelSystem              → Curve = LevelCurve
├─ AbilitySystem            → Stats = PlayerStats, Weapon = AutoShoot
└─ DraftController          → Library = AbilityLibrary, Abilities = AbilitySystem,
                              Levels = LevelSystem, Stats = PlayerStats
```

### On `Systems`
```
Systems
├─ TimeController           → Tuning = CombatTuning
├─ ProjectileService
├─ EnemySpawner
└─ XPGemService             → Prefab = XPGem, Player = Player transform,
                              PlayerStats = PlayerStats, LevelSystem = LevelSystem
```

### On `Canvas`
```
Canvas
├─ DamageNumbers            (existing)
├─ HUD
│  ├─ XPBar               (Image, Type = Filled, Horizontal) — full width, TOP edge
│  ├─ LevelLabel          (TMP_Text)
│  ├─ HealthBar           (Image, Type = Filled)
│  ├─ TimerLabel / KillsLabel
│  ├─ PriorityButton      (Button + TMP_Text)
│  │  └─ TargetPriorityToggle  → Weapon = AutoShoot, Label = its text
│  └─ RunHUD                → Levels, PlayerHealth, Weapon, and the fields above
└─ DraftPanel             (full-screen dim Image, CanvasGroup, INACTIVE by default)
   ├─ Card1 / Card2 / Card3   (DraftCard prefab instances)
   └─ DraftScreen             → Draft = DraftController, Abilities = AbilitySystem,
                                Panel = DraftPanel, Cards = the three, CanvasGroup
```

Put the XP bar along the **top edge, full width**. It is the only promise the
run makes, and a thumb must never cover it.

You now need an **EventSystem** in the scene (GameObject ▸ UI ▸ Event System) —
M0 took no input, M1 takes button presses.

---

## Tuning targets

| Thing | Target | Why |
|---|---|---|
| First draft | ≤ 25 s into run 1 | The hook. Later than this and players leave |
| Drafts per run | 8-12 | Fewer feels static; more makes each meaningless |
| Draft decision time | 2-4 s | If they read every card carefully, they are too wordy |
| Gem collection | Feels greedy | Gems accelerate into you; a slow float reads as a chore |

## What is still open in M1

Delivered here: XP and levelling, the draft, eight abilities with three active
effects, and the priority toggle. Still outstanding from `03-roadmap.md`:

- **`WaveDefinition` timelines.** The spawner still ramps on a timer.
- **Spitter.** Needs ranged-attack code; Shambler, Runner and Brute are stat
  variants and already work.
- **The boss** and its two telegraphed phases.
- **Victory and defeat screens** with a reward summary. Player death currently
  does nothing.
- **Evolutions.** `evolvesInto` / `evolutionRequires` exist on
  `AbilityDefinition` and the draft already skips `isEvolution` assets, but
  nothing unlocks them yet. That is M3.
- **Ability VFX pooling.** The three effects still `Instantiate` their visuals.
  They fire once per cooldown, not per frame, so it is tolerable until the M3
  VFX pass.

## Watch for this on device

If testers describe runs as *watching* rather than playing, that is the
stationary-player tradeoff showing (docs/01 §2.1). The fix is one manually
triggered ability on a cooldown — a grenade button, a barricade — **not** more
passive content. One button converts watching into playing.
