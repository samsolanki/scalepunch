# M0 Setup — wiring the scaffold into a Unity scene

The scripts in `Assets/_Project/Scripts/` are complete but inert. Unity scenes
and prefabs are binary-ish YAML that cannot sensibly be hand-authored outside
the editor, so this is the 30-minute manual pass that turns the code into a
playable build.

**Prerequisite:** Unity 6 LTS, 3D (URP) template, project created *at this repo
root* so `Assets/` lands next to `docs/`.

Install TextMeshPro when prompted (Window ▸ TextMeshPro ▸ Import TMP Essentials) —
`DamageNumber` needs it.

---

## 1. Tuning asset

`Assets/_Project/Data/` → right-click ▸ Create ▸ ScalePunch ▸ Combat Tuning.
Name it `CombatTuning`. Leave the defaults; they are the starting point, not
the answer.

## 2. Scene: `Run`

Create `Assets/_Project/Scenes/Run.unity`.

### Player
```
Player                    (empty GameObject at origin)
├─ CharacterController      radius 0.4, height 1.8, center (0, 0.9, 0)
├─ Health
├─ PlayerStats
├─ PlayerController
├─ AutoAttack
└─ Model                  (child — a Capsule with its collider REMOVED)
   └─ Origin              (child empty at (0, 1, 0.5) — the punch origin)
```
Assign on `PlayerController`: **Model** = the Model child.
Assign on `AutoAttack`: **Model** = Model, **Punch Origin** = Origin.

Removing the capsule's collider matters: `CharacterController` provides
collision, and a second collider on a child makes the player shove itself.

### Camera rig
```
CameraRig                 (empty GameObject)
└─ Main Camera            (child, localPosition ZERO, rotation x=52)
   ├─ CameraShake           → Tuning = CombatTuning
   └─ DamageNumberService   → see §4
```
Put `CameraFollow` on **CameraRig** (not the camera) and set Target = Player.
The rig moves, the camera shakes in local space; they never fight.

### Services
```
Systems                   (empty GameObject)
├─ Hitstop                  → Tuning = CombatTuning
└─ EnemySpawner             → Target = Player, Definitions = §3
```

## 3. Enemy prefab and definition

Build the prefab at `Assets/_Project/Prefabs/Enemy_Grunt.prefab`:
```
Enemy_Grunt               (Capsule, collider REMOVED — combat is registry-based,
│                          not physics-based, so enemies need no colliders)
├─ Health
├─ EnemyMovement
├─ Enemy
├─ HitFlash                 → Tuning = CombatTuning
└─ CombatFeedback           → Tuning = CombatTuning
```

Then Create ▸ ScalePunch ▸ Enemy Definition → `Enemy_Grunt`:
`id` = grunt, `prefab` = the prefab above, `baseHP` 20, `baseDamage` 8,
`moveSpeed` 2.5.

Drag that definition into the spawner's **Definitions** array.

Duplicate it twice for the M0 roster — a Runner (`moveSpeed` 4.5, `baseHP` 10)
and a Brute (`baseHP` 90, `moveSpeed` 1.4, `knockbackResistance` 1).

## 4. UI

```
Canvas                    (Screen Space - Overlay, Scaler: Scale With Screen Size,
│                          reference 1080×1920, Match 0.5)
├─ JoystickArea           (Image, alpha 0, Raycast Target ON,
│  │                       anchored to the LEFT HALF of the screen)
│  ├─ VirtualJoystick      (the script, on JoystickArea itself)
│  ├─ Background          (Image, 220×220, disabled by default)
│  └─ Handle              (Image, 90×90, disabled by default)
└─ DamageNumbers          (empty RectTransform, stretched full screen,
                           Raycast Target OFF)
```
Assign Background and Handle on the `VirtualJoystick`, then assign the
joystick to `PlayerController`.

Damage number prefab at `Assets/_Project/Prefabs/DamageNumber.prefab`:
a RectTransform with `TextMeshProUGUI` (centre-aligned, size 48, Raycast Target
OFF), a `CanvasGroup`, and the `DamageNumber` script.

On `DamageNumberService`: Tuning = CombatTuning, Prefab = that prefab,
Canvas Root = the DamageNumbers object, World Camera = Main Camera.

Make sure the scene has an **EventSystem** (GameObject ▸ UI ▸ Event System) or
the joystick receives nothing.

## 5. Ground

A Plane scaled to 10 (100×100 m) with its default collider kept — the
`CharacterController` needs something to stand on.

---

## Run it

Press play. You should be able to walk with WASD (the editor fallback), enemies
should stream in from off-screen, and punching should produce hitstop, a white
flash, knockback, floating numbers, and a camera kick.

## Then do the only part that matters

Build to your **actual phone** and play for ten minutes. M0's exit criterion is
not "it works" — it is *"punching 30 grey capsules is satisfying with no art"*.

Tune in this order, one at a time, in `CombatTuning`:

1. `hitstopNormal` — 0.03 to 0.07. Too high reads as lag, too low as nothing.
2. `knockbackNormal` — enemies should visibly move, not fly.
3. `AttackSpeed` on PlayerStats — start at 1.5-2.0; slower feels dead.
4. `AttackRange` + `halfAngle` — you want to feel generous, not accurate.
5. `shakeNormal` — the last thing to raise, the first thing players complain about.

If it is still not fun after that, the fix is in these numbers, not in more
features. Do not start M1 until it is.

## Known gaps at M0 (deliberate)

- No XP, levelling, or ability draft — that is M1.
- The spawner ramps on a timer; `WaveDefinition` timelines land in M1.
- Player death does nothing. No run-end screen yet.
- `Luck`, `CooldownReduction` and `Armor` exist in `StatType` but nothing reads
  them yet; they are there so M1 does not need a refactor.
