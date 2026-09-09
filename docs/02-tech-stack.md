# Tech Stack & Project Architecture

## 1. Engine choice

**Recommendation: Unity 6 LTS.**

| Option | Verdict |
|---|---|
| **Unity 6 LTS** | **Pick this.** Every ad mediation SDK ships Unity-first; the entire mobile F2P tooling ecosystem (LevelPlay, AppLovin MAX, GameAnalytics, Firebase) assumes it. Rollic and every studio in this genre ship Unity. Asset Store covers your art gap. |
| Godot 4 | Genuinely good and free, but ad-mediation SDKs are community-maintained Android plugins. You will spend weeks on SDK plumbing instead of the game. Only choose this if you object to Unity's licensing on principle. |
| Web (Three.js / Phaser) | Fine for a playable prototype in days, wrong for a Play Store release. No native ad mediation, worse performance, WebView wrappers get rejected or perform badly. |
| Unreal | Overkill. Build size and mobile iteration speed both work against you. |

Everything below assumes Unity. The design in `01-game-design.md` and the
milestones in `03-roadmap.md` are engine-agnostic.

**Render pipeline:** URP, 3D, low-poly stylised. Mobile renderer, forward,
no post-processing beyond bloom and vignette. Low-poly is cheap to produce,
cheap to run, and matches the genre's visual language.

## 2. Packages and SDKs

**Unity packages**
- Input System (on-screen joystick)
- Addressables (asset loading, later patching without a store update)
- Burst + Collections (only if enemy counts force it; do not start here)
- TextMeshPro

**Third-party**
| Need | Pick | Why |
|---|---|---|
| Tweening | DOTween Pro | UI and juice; the industry default |
| Save | Easy Save 3, or hand-rolled JSON + AES | ES3 is worth the money |
| Ads | **AppLovin MAX** or **LevelPlay (ironSource)** | Mediation, not a single network. Never integrate AdMob alone |
| IAP | Unity IAP | Receipt validation included |
| Analytics | GameAnalytics (free) or Firebase | You cannot tune the economy without funnels |
| Remote config | Firebase Remote Config | Change balance without shipping a build |
| Crash | Firebase Crashlytics | |

Integrate ads and analytics at **M4**, not earlier — SDKs slow iteration and
break editor play mode.

## 3. Data-driven everything

Every number in `01-game-design.md` lives in a **ScriptableObject**, never in
code. This is the single most important architectural decision in the project:
balancing a game like this means changing hundreds of numbers hundreds of
times, and you must be able to do it without recompiling.

```
AbilityDefinition   id, name, icon, type, levels[], evolvesFrom, evolveRequires
EnemyDefinition     prefab, baseHP, baseDamage, moveSpeed, behaviour, xpValue
WaveDefinition      entries[] { timeOffset, enemyId, count, spawnPattern }
StageDefinition     waves[], bossId, hpMultiplier, dmgMultiplier, rewards
GearDefinition      slot, rarity, affixPool, statRanges
RoomDefinition      effectStat, perLevelValue, costBase, costGrowth
```

## 4. Project layout

```
Assets/
  _Project/
    Art/          Models, Materials, VFX, UI
    Audio/
    Data/         ScriptableObject instances (the balance sheet)
    Prefabs/      Enemies, Abilities, Pickups, UI
    Scenes/       Boot, Meta, Run
    Scripts/
      Core/       GameManager, SceneLoader, ServiceLocator, EventBus
      Combat/     PlayerController, AutoAttack, Health, DamageSystem, Hitstop
      Enemies/    EnemySpawner, EnemyAI, EnemyPool
      Abilities/  AbilitySystem, AbilityRuntime, DraftController, Evolution
      Loot/       XPSystem, DropTable, PickupMagnet
      Meta/       Inventory, GearService, BaseService, CurrencyService, IdleIncome
      Save/       SaveData, SaveService, Migrations
      UI/         HUD, DraftScreen, InventoryUI, BaseUI, ShopUI
      Services/   Ads, IAP, Analytics, RemoteConfig
  Plugins/        Third-party SDKs
```

**Three scenes:** `Boot` (load services, then jump), `Meta` (base, inventory,
shop, stage select), `Run` (the actual gameplay). Keep `Run` fully additive and
unloadable so a failed run cannot leak state into the meta.

## 5. Performance rules (mobile, non-negotiable)

1. **Pool everything.** Enemies, projectiles, damage numbers, XP gems, VFX,
   audio sources. Zero `Instantiate` during a run.
2. **No per-frame `GetComponent` or `Find`.** Cache in `Awake`.
3. **Spatial queries, not O(n²).** Use a uniform grid or `OverlapSphereNonAlloc`
   for "nearest enemy", never a loop over all enemies per attacker.
4. **GPU instancing + a shared atlas** for enemy materials.
5. **Damage numbers are the classic frame killer.** Pool them, cap concurrent
   count at ~40, use a single canvas with `CanvasGroup` batching.
6. **Target 60 fps on a 4-year-old mid-range Android.** Test on real hardware
   early; the editor lies. Profile with 150 enemies on screen, not 10.

## 6. Save data

Local JSON, versioned, with a migration path from day one:

```json
{
  "version": 3,
  "currencies": { "coins": 0, "gems": 0, "scrap": 0 },
  "gear":       [ { "defId": "gloves_epic", "level": 4, "affixes": [...] } ],
  "equipped":   { "gloves": "uid_1", "shoes": null },
  "rooms":      { "gym": 12, "kitchen": 8, "vault": 5 },
  "progress":   { "highestStage": 14, "unlockedAbilities": [...] },
  "lastSeenUtc": "2026-09-09T10:00:00Z"
}
```

Write a `Migrations` class before you ship v1. The first time you change the
save shape after launch without one, you wipe every player's account.

Add cloud save (Google Play Games / Firebase) before launch — device loss is
a top-3 one-star review cause in this genre.
