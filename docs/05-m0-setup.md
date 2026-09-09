# M0 Setup — wiring the scaffold into a Unity scene

The scripts in `Assets/_Project/Scripts/` are complete but inert. Unity scenes
and prefabs are editor-authored YAML that cannot sensibly be hand-written, so
this is the ~30-minute manual pass that turns the code into a playable build.

> **Fast path:** run **ScalePunch ▸ Build Run Scene** from the menu bar. It
> creates every asset, prefab and GameObject below and wires all ~70 references
> automatically, then saves the scene to `Assets/_Project/Scenes/Run.unity`.
> It is re-runnable and preserves asset GUIDs, so rebuild any time something
> comes unwired. The rest of this document explains what it builds and, more
> importantly, **what to tune once it runs**.

**What M0 is:** a stationary survivor with an engagement radius drawn on the
ground. Zombies converge from all directions; anything crossing the ring is
acquired and shot automatically. No movement, no joystick, no fire button.

**Prerequisite:** Unity 6 LTS, 3D (URP) template, project created *at this repo
root* so `Assets/` lands next to `docs/`.

Install TextMeshPro when prompted (Window ▸ TextMeshPro ▸ Import TMP Essentials) —
`DamageNumber` needs it.

---

## 1. Data assets

In `Assets/_Project/Data/`:

- Create ▸ ScalePunch ▸ **Combat Tuning** → name it `CombatTuning`.
- Create ▸ ScalePunch ▸ **Weapon Definition** → name it `Weapon_Pistol`.
  Leave the multipliers at 1; the player's stat sheet supplies the real numbers.
- Create ▸ ScalePunch ▸ **Enemy Definition** → `Zombie_Shambler`
  (`baseHP` 20, `baseDamage` 8, `moveSpeed` 2.5).

Defaults are a starting point, not an answer.

## 2. Projectile prefab

`Assets/_Project/Prefabs/Projectile_Bullet.prefab`:
```
Projectile_Bullet         (small Capsule or Quad, scale ~0.15, collider REMOVED)
├─ Projectile               → Trail = the TrailRenderer below
└─ TrailRenderer            time 0.08, width 0.12 → 0, unlit additive material
```
Rounds need no collider and no rigidbody — hit detection is a swept segment
test against `EnemyRegistry`, not physics.

Assign this prefab to `Weapon_Pistol`'s **Projectile Prefab**.

The trail is not decoration. At 30 m/s a round crosses the radius in a third of
a second; without a tracer the player sees no shot at all, only zombies falling
over.

## 3. Zombie prefab

`Assets/_Project/Prefabs/Zombie_Shambler.prefab`:
```
Zombie_Shambler           (Capsule, collider REMOVED — combat is registry-based)
├─ Health
├─ EnemyMovement
├─ Enemy
├─ HitFlash                 → Tuning = CombatTuning
└─ CombatFeedback           → Tuning = CombatTuning
```
Assign it to `Zombie_Shambler`'s **Prefab** field.

Then duplicate the *definition* twice for the M0 roster:
- **Runner** — `moveSpeed` 4.5, `baseHP` 10, `behaviour` Runner
- **Brute** — `baseHP` 90, `moveSpeed` 1.4, `knockbackResistance` 1, `scale` 1.4

Three definitions can share one prefab; they differ only by stats and tint.

## 4. Scene: `Run`

Create `Assets/_Project/Scenes/Run.unity`.

### Player
```
Player                    (empty GameObject at origin)
├─ Health
├─ PlayerStats
├─ AutoShoot                → Weapon = Weapon_Pistol, Tuning = CombatTuning,
│                             Turret = Turret, Muzzle = Muzzle
├─ Turret                 (child — Capsule with its collider REMOVED)
│  └─ Muzzle              (child empty at (0, 1.2, 0.6) — barrel tip)
└─ RangeRing              (child at y=0)
   ├─ LineRenderer          width 0.08, loop ON, unlit additive material,
   │                        Use World Space OFF
   └─ RangeIndicator        → Weapon = AutoShoot on Player
```
Remove the capsule collider on Turret: the player is stationary and nothing
needs to collide with it.

### Camera
```
CameraRig                 (position (0, 19, -13), rotation x=55)
└─ Main Camera            (localPosition ZERO, localRotation identity)
   ├─ CameraShake           → Tuning = CombatTuning
   └─ DamageNumberService   → see §5
```
The player never moves, so the rig never moves either — but the rig must still
exist, because shake is applied in local space. `CameraFollow` is in the repo
for later; leave it off the scene.

`CameraShake` writes `localPosition` and zeroes it when idle, so the camera
**must** be a child of a positioned rig, at `localPosition` zero. A root camera
gets snapped to the world origin on the first idle `LateUpdate`.

### Services
```
Systems                   (empty GameObject)
├─ Hitstop                  → Tuning = CombatTuning
├─ ProjectileService
└─ EnemySpawner             → Target = Player,
                              Definitions = the three zombie definitions,
                              Spawn Radius = 16
```
**Spawn Radius must exceed the engagement radius** (base 9). A zombie that
spawns already inside the ring robs the player of the approach, which is the
only tension the game has.

### Ground
A Plane scaled to 10 (100×100 m). Keep its collider or not — nothing uses
physics — but keep it visually darker than the range ring.

## 5. UI

```
Canvas                    (Screen Space - Overlay, Scaler: Scale With Screen Size,
│                          reference 1080×1920, Match 0.5)
└─ DamageNumbers          (empty RectTransform, stretched full screen,
                           Raycast Target OFF)
```

Damage number prefab at `Assets/_Project/Prefabs/DamageNumber.prefab`:
a RectTransform with `TextMeshProUGUI` (centre-aligned, size 48, Raycast Target
OFF), a `CanvasGroup`, and the `DamageNumber` script.

On `DamageNumberService`: Tuning = CombatTuning, Prefab = that prefab,
Canvas Root = the DamageNumbers object, World Camera = Main Camera.

No EventSystem or joystick is needed — M0 takes no input at all.

---

## Run it

Press play. Zombies should walk in from off screen, the turret should snap to
the first one crossing the ring, and rounds should trace out, land, flash the
zombie white, kick the camera, and throw a damage number.

## Then do the only part that matters

Build to your **actual phone** and watch it for ten minutes. M0's exit criterion
is not "it works" — it is *"holding the ring against 30 grey capsules is
satisfying with no art"*.

Tune in this order, one at a time, in `CombatTuning` and `PlayerStats`:

1. **`FireRate`** (start 3, try 2-6). The single biggest feel lever. Too slow
   reads as broken; too fast reads as free.
2. **`Range`** vs zombie `moveSpeed`. You want zombies to *nearly* reach you.
   If nothing gets close, the run has no tension; if everything does, it is
   unfair. This ratio is the whole difficulty curve.
3. **`ProjectileSpeed`** (20-40). Too slow and rounds visibly trail behind
   runners; too fast and there is no tracer to see.
4. **`hitstopNormal`** (0.03-0.07). Too high reads as lag, too low as nothing.
5. **`knockbackNormal`** (~1.6). Zombies should stagger, not fly. Keep Brutes
   at `knockbackResistance` 1 or sustained fire stunlocks them at the ring.
6. **`shakeOnFire`** (~0.025) — last. At 3+ shots/sec this is constant;
   anything above ~0.04 is nausea, not punch.

If it is still not fun after that, the fix is in these numbers, not in more
features. Do not start M1 until it is.

## Known gaps at M0 (deliberate)

- No XP, levelling, or ability draft — that is M1, and it is the only in-run
  interaction the design has. See `docs/01-game-design.md` §2.1.
- The spawner ramps on a timer; `WaveDefinition` timelines land in M1.
- Player death does nothing. No run-end screen yet.
- **Lifesteal is not wired.** With projectiles, healing happens at the point of
  impact rather than the point of firing, and the round would need a reference
  back to the shooter. The stat exists; nothing reads it.
- `Armor`, `Luck` and `CooldownReduction` are likewise declared but unread —
  they are there so M1 does not need a refactor.
- `EnemyBehaviour` is authoring intent only. Shambler, Runner and Brute
  genuinely differ by stats alone; Spitter needs ranged-attack code at M1.
- Target priority is a serialised field, not a HUD control yet.
