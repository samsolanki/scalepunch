# Asset Production List

Everything that needs a model and animation, taken from what the code actually
spawns. Numbers below are the real values in the assets — match them or the
animation will not line up with the behaviour.

---

## ⚠ Read this first: there is no animation system yet

There is **no `Animator` anywhere in the project**. Zero. Every bit of motion
today is procedural:

- `HitFlinch` tilts the body on hit
- `BossController` scale-pulses during its wind-up
- `MuzzleFlash` pops a quad at the barrel

So animated models will not play until an animation layer is added and wired to
`EnemyMovement` / `AutoShoot` / `BossController` states. **Make the models and
clips anyway** — that work is independent — but budget for the integration, and
expect `HitFlinch` and the boss scale-pulse to be replaced by real clips.

---

## 1. Characters — 5 models

| # | Model | Scale | Move speed | Notes |
|---|---|---|---|---|
| 1 | **Player / Survivor** | 1.0 | **0 — never moves** | Stationary emplacement. Rotates to face its target |
| 2 | **Shambler** | 1.00 | 2.5 | The baseline zombie |
| 3 | **Runner** | 0.85 | 4.5 | Small, thin, leaning forward |
| 4 | **Brute** | 1.45 | 1.4 | Wide, hunched, long arms |
| 5 | **Boss** | **2.2** | 1.6 | Distinct shape entirely, not a scaled Brute |

**Minimum to ship the prototype: 2 models** — one ordinary zombie and one boss.
Shambler, Runner and Brute currently share a single prefab and differ only by
scale and tint, so one zombie model covers all three.

**But make all five.** Per `docs/09-art-direction.md`, tiers must be
distinguishable by **silhouette**, not colour — a player has to identify a threat
in a crowd of 150 while shooting, and colour alone fails the moment two tiers
overlap.

## 2. Weapons — 1 model

| # | Model | Notes |
|---|---|---|
| 1 | **Pistol** | The only weapon that exists. Attaches at the `Muzzle` transform |

Planned but **not authored** — do not model these yet: Shotgun, Minigun,
Railgun. The `WeaponDefinition` system already supports them with zero code
changes (they are multiplier sets), but no assets exist and nothing references
them.

Also needed, though it is VFX rather than a model: a **bullet tracer** (currently
a sphere with a trail).

## 3. Animation clips — 20 total

### Player — 4 clips

| Clip | Length | Notes |
|---|---|---|
| Idle | loop | Weapon raised, subtle breathing |
| **Fire** | **≤ 0.15 s** | See the warning below |
| Hit | 0.3 s | Optional — `HitFlinch` does this procedurally today |
| Death | 1.0 s | One-shot |

**No walk cycle and no turn animation.** The player never moves, and the
transform rotates to aim.

> **Fire rate reaches 6+ shots per second** with upgrades (base 3/s, and
> Trigger Work stacks +12% per level). A fire clip longer than ~0.15 s will
> never finish before the next shot and will visibly stutter. Either keep it
> very short or build it as an **additive layer** over Idle.

### Ordinary zombies — 3 clips each

| Clip | Length | Notes |
|---|---|---|
| Walk | loop | The only locomotion — they always advance |
| Attack | **1.0 s** | Must match `attackInterval = 1.0` |
| Death | 1.0-1.5 s | One-shot |

**No idle needed** — a zombie is always walking or attacking.

Tune the walk cycle to each tier's speed: Shambler 2.5, Runner 4.5 (a real run),
Brute 1.4 (a heavy lumber). If they share a rig, one cycle retimed per tier is
acceptable for v1.

### Boss — 7 clips

Timings are already fixed in `BossController`. **Match them exactly**:

| Clip | Length | Driven by |
|---|---|---|
| Walk | loop | `moveSpeed 1.6` |
| Attack | **1.4 s** | `attackInterval` |
| **Telegraph** | **1.2 s** | `telegraphSeconds` — the wind-up |
| **Charge** | **0.75 s** loop | `chargeSeconds` |
| **Recover** | **0.9 s** | `recoverSeconds` |
| Enrage | ~1.0 s | Fires once at 50% HP |
| Death | 2.0 s | One-shot |

The **telegraph is the most important animation in the game**. A stationary
player cannot dodge, so the charge has to be answerable rather than avoidable —
burn the boss down during the wind-up, or eat the hit. If the telegraph is not
unmistakable at a glance, the charge reads as an unfair hit.

---

## 4. Technical requirements

**Pivot and facing**
- Pivot at the **feet**, at world origin — not the model's centre.
- Model faces **+Z** (Unity forward). `EnemyMovement` and `AutoShoot` both use
  `LookRotation`, so a model authored facing -Z or +X will walk backwards or
  sideways.

**Poly budget**
- Ordinary zombies: **300-800 tris**
- Boss: **2,000-3,000 tris**
- Player: **1,500-2,500 tris**

**Materials**
- **One shared material** for all ordinary zombies, tinted per tier via
  `MaterialPropertyBlock` — this is already how `HitFlash` works.
- Vertex colours, or a single shared **256px colour atlas**. Never per-enemy
  textures; 150 material instances is a silent memory and batching disaster.
- Flat shaded. No normal maps — invisible at this camera and not affordable.

**Rigging — the one that bites later**
- Keep bone counts **low, ~20 or fewer** per zombie.
- At 150 skinned meshes on screen, `SkinnedMeshRenderer` skinning becomes a real
  cost. If the profiler complains after integration, the fix is **baked vertex
  animation** (animation baked to a texture, played in a shader), which
  instances perfectly but requires the clips to be finalised first.
- Enable **Optimize Game Objects** on the imported rigs.

**Prefab slot**
Each enemy prefab is a root with a single `Body` child. The model replaces
`Body`. Keep the root empty — it is what rotates and moves, and scaling or
tilting it would steer the zombie off course.

---

## 5. Environment

Not characters, but the same pass:

- **Ground plane** — dark, near-flat, subtle tiling at most.
- **2-3 repeated props** — silhouetted, unlit, set dressing for the ad creative.
  The reference game uses scattered chairs.
- Nothing bright. **The engagement ring must be the brightest thing on the
  ground** — it is the entire read of the game.

---

## 6. Summary

| Category | Count |
|---|---|
| Character models | **5** (2 to ship the prototype) |
| Weapon models | **1** |
| Animation clips | **20** (4 player + 9 zombie + 7 boss) |
| Environment props | 2-3 + ground |

**Before modelling anything**, run the readability check in
`docs/09-art-direction.md` §9: set tint and scale per tier on the grey capsules
and confirm you can tell them apart in a crowd of 100, on a phone, while
shooting. If capsules already read clearly, the models are a skin over a working
design. If they do not, the tier shapes need rethinking before anyone models
them.
