using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

using ScalePunch.Abilities;
using ScalePunch.Abilities.Effects;
using ScalePunch.Combat;
using ScalePunch.Data;
using ScalePunch.Enemies;
using ScalePunch.Stages;
using ScalePunch.Progression;
using ScalePunch.Weapons;

namespace ScalePunch.EditorTools
{
    public static partial class RunSceneBuilder
    {
        class MaterialSet
        {
            public Material ground, turret, barrel, flash, shambler, runner, brute, bullet, gem, line;
        }

        class DataSet
        {
            public CombatTuning tuning;
            public LevelCurve curve;
            public WeaponDefinition pistol;
            public EnemyDefinition[] zombies;
            public EnemyDefinition boss;
            public WaveDefinition[] waves;
            public StageDefinition stage;
            public AbilityLibrary library;
        }

        // ----------------------------------------------------------- materials

        static Material Mat(string name, Color colour, bool unlit)
        {
            string path = $"{MatDir}/{name}.mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);

            // "Sprites/Default" is unlit, transparent and present in every
            // pipeline - the safe choice for line and trail renderers, which do
            // not want lighting anyway.
            Shader shader = unlit
                ? Shader.Find("Sprites/Default")
                : Shader.Find("Universal Render Pipeline/Lit");

            if (shader == null) shader = Shader.Find("Standard");

            if (existing != null)
            {
                existing.shader = shader;
                ApplyColour(existing, colour);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var mat = new Material(shader) { name = name };
            ApplyColour(mat, colour);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        static void ApplyColour(Material mat, Color colour)
        {
            // URP uses _BaseColor; Sprites/Default and the built-in fallback use _Color.
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", colour);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", colour);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.1f);
        }

        static MaterialSet BuildMaterials() => new MaterialSet
        {
            ground   = Mat("M_Ground",    new Color(0.16f, 0.17f, 0.20f), false),
            turret   = Mat("M_Player",    new Color(0.35f, 0.75f, 0.95f), false),
            barrel   = Mat("M_Barrel",    new Color(0.14f, 0.15f, 0.18f), false),
            flash    = Mat("M_MuzzleFlash", new Color(1.00f, 0.85f, 0.45f), true),
            shambler = Mat("M_Shambler",  new Color(0.45f, 0.60f, 0.35f), false),
            runner   = Mat("M_Runner",    new Color(0.85f, 0.70f, 0.25f), false),
            brute    = Mat("M_Brute",     new Color(0.70f, 0.25f, 0.25f), false),
            bullet   = Mat("M_Bullet",    new Color(1.00f, 0.90f, 0.55f), true),
            gem      = Mat("M_XPGem",     new Color(0.35f, 0.95f, 0.70f), false),
            line     = Mat("M_RangeRing", new Color(0.25f, 1.00f, 0.55f, 0.55f), true)
        };

        // ---------------------------------------------------------------- data

        static DataSet BuildData()
        {
            var set = new DataSet
            {
                tuning = Asset<CombatTuning>("CombatTuning"),
                curve  = Asset<LevelCurve>("LevelCurve"),
                pistol = Asset<WeaponDefinition>("Weapon_Pistol")
            };

            // The weapon is pure multipliers over the player's stat sheet, so a
            // baseline pistol is all 1s. A shotgun is this asset with
            // extraProjectiles and spread raised - no code change.
            set.pistol.id = "pistol";
            set.pistol.displayName = "Pistol";
            // The zombie capsule is 0.5 radius and the round about 0.05, so 0.55
            // is the radius at which a visual contact and a registered hit agree.
            // Erring generous here is invisible; erring tight reads as bullets
            // passing through zombies.
            set.pistol.hitRadius = 0.55f;
            set.pistol.lifetime = 2.5f;
            set.pistol.damageMultiplier = 1f;
            set.pistol.fireRateMultiplier = 1f;
            set.pistol.rangeMultiplier = 1f;
            set.pistol.projectileSpeedMultiplier = 1f;
            set.pistol.extraProjectiles = 0;
            set.pistol.spreadDegrees = 0f;
            set.pistol.inaccuracyDegrees = 1.0f;

            // Barely any steering. 220 deg/s let rounds visibly curve after a
            // target, which reads as a guided missile, not a bullet. Just enough
            // to correct for a Runner crossing the line of fire.
            set.pistol.steerDegreesPerSecond = 45f;

            set.zombies = BuildZombies();
            set.boss = BuildBoss();
            set.waves = BuildWaves(set.zombies, set.boss);
            set.stage = BuildStage(set.waves, set.boss);
            set.library = BuildAbilities();

            // Echo the values actually written. Unity can run a queued menu item
            // against the pre-reload assembly if it is clicked while a compile is
            // finishing, and the only symptom is stale data with a clean log.
            // This line makes which code ran unambiguous.
            Debug.Log($"[ScalePunch] Data written: shambler {set.zombies[0].baseHP} HP, " +
                      $"runner {set.zombies[1].baseHP} HP, " +
                      $"brute {set.zombies[2].baseHP} HP / armour {set.zombies[2].armor}, " +
                      $"steer {set.pistol.steerDegreesPerSecond} deg/s, " +
                      $"stage '{set.stage.id}' {set.stage.WaveCount} waves / " +
                      $"{set.stage.TotalWaveSeconds():0}s + boss {set.boss.baseHP} HP");

            EditorUtility.SetDirty(set.stage);
            EditorUtility.SetDirty(set.boss);
            foreach (WaveDefinition w in set.waves) EditorUtility.SetDirty(w);

            EditorUtility.SetDirty(set.tuning);
            EditorUtility.SetDirty(set.curve);
            EditorUtility.SetDirty(set.pistol);
            return set;
        }

        static T LoadData<T>(string file) where T : ScriptableObject
            => AssetDatabase.LoadAssetAtPath<T>($"{DataDir}/{file}.asset");

        /// <summary>
        /// Re-resolves every data asset from disk. Asset references do not survive
        /// a reimport, so anything held across one has to be looked up again.
        /// </summary>
        static void Reacquire(DataSet set)
        {
            set.boss = Keep(LoadData<EnemyDefinition>("Zombie_Boss"), set.boss);
            set.stage = Keep(LoadData<StageDefinition>("Stage_01"), set.stage);

            if (set.waves != null)
                for (int i = 0; i < set.waves.Length; i++)
                    set.waves[i] = Keep(LoadData<WaveDefinition>($"Wave_{i + 1:00}"), set.waves[i]);

            set.tuning = Keep(LoadData<CombatTuning>("CombatTuning"), set.tuning);
            set.curve = Keep(LoadData<LevelCurve>("LevelCurve"), set.curve);
            set.pistol = Keep(LoadData<WeaponDefinition>("Weapon_Pistol"), set.pistol);
            set.library = Keep(LoadData<AbilityLibrary>("AbilityLibrary"), set.library);

            string[] files = { "Zombie_Shambler", "Zombie_Runner", "Zombie_Brute" };
            var zombies = new EnemyDefinition[files.Length];

            for (int i = 0; i < files.Length; i++)
            {
                EnemyDefinition live = set.zombies != null && i < set.zombies.Length
                    ? set.zombies[i]
                    : null;
                zombies[i] = Keep(LoadData<EnemyDefinition>(files[i]), live);
            }

            set.zombies = zombies;
        }

        static EnemyDefinition Zombie(string file, string id, float hp, float damage,
                                      float speed, float knockbackResist, int xp,
                                      float scale, EnemyBehaviour behaviour, float armor = 0f)
        {
            EnemyDefinition def = Asset<EnemyDefinition>(file);

            def.id = id;
            def.baseHP = hp;
            def.baseDamage = damage;
            def.moveSpeed = speed;
            def.attackRange = 1.4f;
            def.attackInterval = 1.0f;
            def.behaviour = behaviour;
            def.knockbackResistance = knockbackResist;
            def.armor = armor;
            def.xpValue = xp;
            def.scale = scale;

            EditorUtility.SetDirty(def);
            return def;
        }

        static EnemyDefinition[] BuildZombies()
        {
            return new[]
            {
                // 5 HP against 5 damage: exactly one bullet per Shambler at
                // wave 0. That one-shot read is the whole point of the basic
                // zombie - it is how the player learns the gun works.
                Zombie("Zombie_Shambler", "shambler",  5f,  8f, 2.5f, 0f, 1, 1.00f, EnemyBehaviour.Shambler),
                // Medium tier: two bullets. Fast enough to cross the ring in the
                // time those two shots take, which is what makes fire rate the
                // stat that answers it.
                Zombie("Zombie_Runner",   "runner",   10f,  6f, 4.5f, 0f, 2, 0.85f, EnemyBehaviour.Runner),

                // Brutes are knockback-immune. Without that, sustained fire
                // stunlocks them at the edge of the ring and they stop being a
                // DPS check at all.
                // Armour on top of the health pool is what makes a Brute a real
                // damage check: it punishes many-small-hits builds specifically,
                // where raw HP just takes longer to chew through.
                // Big tier: 10 HP, two bullets, and armour 0 on purpose.
                //
                // Armour and "dies in exactly N bullets" pull against each other -
                // with armour 1 a 5 damage round only lands 4, so 10 HP would take
                // three shots, not two. Anything that must die in a countable
                // number of hits has to have no armour at all.
                Zombie("Zombie_Brute",    "brute",    10f, 18f, 1.4f, 1f, 4, 1.45f, EnemyBehaviour.Brute, armor: 0f)
            };
        }

        // --------------------------------------------------------- boss & stage

        static EnemyDefinition BuildBoss()
        {
            // Deliberately not a wall of HP. The boss is interesting because of
            // its charge, and a fight long enough to be boring is worse than one
            // that ends while the telegraph is still exciting. 180 HP against a
            // levelled-up build is roughly 25-40 seconds.
            EnemyDefinition def = Zombie("Zombie_Boss", "boss", 180f, 22f, 1.6f,
                                        knockbackResist: 1f, xp: 40, scale: 2.2f,
                                        behaviour: EnemyBehaviour.Brute, armor: 0f);

            // Longer reach and a slower swing than a Brute: the boss should feel
            // heavy, and its melee is not the threat — the charge is.
            def.attackInterval = 1.4f;
            def.attackRange = 2.0f;
            return def;
        }

        static WaveEntry Entry(EnemyDefinition enemy, float at, int count,
                               SpawnPattern pattern, float spread, float arc = 90f) => new()
        {
            enemy = enemy,
            timeOffset = at,
            count = count,
            pattern = pattern,
            spreadSeconds = spread,
            arcDegrees = arc
        };

        static WaveDefinition Wave(int number, string display, float duration, params WaveEntry[] entries)
        {
            WaveDefinition wave = Asset<WaveDefinition>($"Wave_{number:00}");
            wave.displayName = display;
            wave.entries = entries;

            // Every duration below is hand-checked against its entries' last
            // arrival; WaveDefinition.OnValidate is the backstop for hand edits
            // in the inspector, not for these.
            wave.duration = duration;

            EditorUtility.SetDirty(wave);
            return wave;
        }

        /// <summary>
        /// Eight waves, about four minutes, shaped as a curve rather than a ramp:
        /// each wave introduces or recombines one thing, and waves 4 and 7 are
        /// deliberately lighter. Unbroken escalation reads as flat — the dips are
        /// what make the peaks land.
        /// </summary>
        static WaveDefinition[] BuildWaves(EnemyDefinition[] zombies, EnemyDefinition boss)
        {
            EnemyDefinition shambler = zombies[0];
            EnemyDefinition runner = zombies[1];
            EnemyDefinition brute = zombies[2];

            return new[]
            {
                // 1 - teach the ring. A trickle from all sides, nothing dangerous.
                Wave(1, "Trickle", 22f,
                     Entry(shambler, 1f, 6, SpawnPattern.Ring, 8f)),

                // 2 - first pressure direction: they come from one side.
                Wave(2, "Pressure", 24f,
                     Entry(shambler, 0f, 8, SpawnPattern.Ring, 10f),
                     Entry(shambler, 12f, 6, SpawnPattern.Arc, 4f, 70f)),

                // 3 - runners. Punishes a build that took no fire rate.
                Wave(3, "Sprinters", 26f,
                     Entry(shambler, 0f, 6, SpawnPattern.Ring, 8f),
                     Entry(runner, 8f, 6, SpawnPattern.Arc, 3f, 60f),
                     Entry(runner, 17f, 8, SpawnPattern.Ring, 5f)),

                // 4 - lull. One Brute, alone, with room to see what it is.
                Wave(4, "The Big One", 24f,
                     Entry(brute, 2f, 1, SpawnPattern.Point, 0f),
                     Entry(shambler, 10f, 6, SpawnPattern.Ring, 9f)),

                // 5 - the combination the first four waves taught separately.
                Wave(5, "Combined Arms", 30f,
                     Entry(shambler, 0f, 10, SpawnPattern.Ring, 10f),
                     Entry(runner, 9f, 8, SpawnPattern.Arc, 4f, 80f),
                     Entry(brute, 16f, 2, SpawnPattern.Arc, 2f, 40f)),

                // 6 - a breach: everything through one gap at once.
                Wave(6, "Breach", 30f,
                     Entry(runner, 0f, 12, SpawnPattern.Point, 5f),
                     Entry(shambler, 6f, 12, SpawnPattern.Arc, 8f, 50f),
                     Entry(brute, 18f, 2, SpawnPattern.Point, 1f)),

                // 7 - second lull. Recover, and let the draft catch up.
                Wave(7, "Regroup", 22f,
                     Entry(shambler, 2f, 8, SpawnPattern.Ring, 12f)),

                // 8 - the wall before the boss.
                Wave(8, "The Wall", 34f,
                     Entry(shambler, 0f, 14, SpawnPattern.Ring, 10f),
                     Entry(runner, 7f, 12, SpawnPattern.Ring, 8f),
                     Entry(brute, 14f, 4, SpawnPattern.Ring, 6f),
                     Entry(runner, 24f, 10, SpawnPattern.Arc, 4f, 90f))
            };
        }

        static StageDefinition BuildStage(WaveDefinition[] waves, EnemyDefinition boss)
        {
            StageDefinition stage = Asset<StageDefinition>("Stage_01");

            stage.id = "stage_01";
            stage.displayName = "Stage 1";
            stage.stageNumber = 1;
            stage.waves = waves;
            stage.boss = boss;
            stage.bossIntroSeconds = 2.5f;
            stage.hpMultiplier = 1f;
            stage.damageMultiplier = 1f;
            stage.coinsOnClear = 250;
            stage.coinsPerKill = 2;
            stage.failPayoutFraction = 0.35f;

            EditorUtility.SetDirty(stage);
            return stage;
        }

        // ----------------------------------------------------------- abilities

        static AbilityLevel[] NewLevels(int count)
        {
            var levels = new AbilityLevel[count];
            for (int i = 0; i < count; i++) levels[i] = new AbilityLevel();
            return levels;
        }

        static AbilityDefinition Active(string file, string display, AbilityEffect effect,
                                        float[] cooldown, float[] damage, float[] radius,
                                        int[] targets, string[] descriptions, float weight)
        {
            AbilityDefinition def = Asset<AbilityDefinition>(file);

            def.id = file;
            def.displayName = display;
            def.kind = AbilityKind.Active;
            def.effect = effect;
            def.draftWeight = weight;
            def.levels = NewLevels(cooldown.Length);

            for (int i = 0; i < def.levels.Length; i++)
            {
                def.levels[i].description = descriptions[i];
                def.levels[i].cooldown = cooldown[i];
                def.levels[i].damageMultiplier = damage[i];
                def.levels[i].radius = radius[i];
                def.levels[i].targets = targets[i];
                def.levels[i].modifiers = new StatModifier[0];
            }

            EditorUtility.SetDirty(def);
            return def;
        }

        static AbilityDefinition Passive(string file, string display, StatType stat,
                                         ModifierKind kind, float perLevel, int levelCount,
                                         float weight, string unitLabel)
        {
            AbilityDefinition def = Asset<AbilityDefinition>(file);

            def.id = file;
            def.displayName = display;
            def.kind = AbilityKind.Passive;
            def.effect = null;
            def.draftWeight = weight;
            def.levels = NewLevels(levelCount);

            string text = kind == ModifierKind.Percent
                ? $"+{perLevel * 100f:0.#}% {unitLabel}"
                : $"+{perLevel:0.##} {unitLabel}";

            for (int i = 0; i < def.levels.Length; i++)
            {
                def.levels[i].description = text;
                def.levels[i].modifiers = new[]
                {
                    new StatModifier { stat = stat, kind = kind, value = perLevel }
                };
            }

            EditorUtility.SetDirty(def);
            return def;
        }

        static AbilityLibrary BuildAbilities()
        {
            var burst     = Asset<RadialBurstEffect>("Effect_RadialBurst");
            var grenade   = Asset<GrenadeEffect>("Effect_Grenade");
            var lightning = Asset<ChainLightningEffect>("Effect_ChainLightning");

            EditorUtility.SetDirty(burst);
            EditorUtility.SetDirty(grenade);
            EditorUtility.SetDirty(lightning);

            var abilities = new List<AbilityDefinition>
            {
                Active("Ability_Shockwave", "Shockwave", burst,
                    new[] { 6.0f, 5.5f, 5.0f, 4.5f, 4.0f },
                    new[] { 1.2f, 1.5f, 1.8f, 2.1f, 2.5f },
                    new[] { 4.0f, 4.5f, 5.0f, 5.5f, 6.0f },
                    new[] { 0, 0, 0, 0, 0 },
                    new[]
                    {
                        "Blast 120% damage in 4 m, every 6 s",
                        "Blast 150% damage in 4.5 m, every 5.5 s",
                        "Blast 180% damage in 5 m, every 5 s",
                        "Blast 210% damage in 5.5 m, every 4.5 s",
                        "Blast 250% damage in 6 m, every 4 s"
                    },
                    1.0f),

                Active("Ability_Grenade", "Frag Grenade", grenade,
                    new[] { 7.0f, 6.5f, 6.0f, 5.5f, 5.0f },
                    new[] { 2.0f, 2.4f, 2.8f, 3.2f, 3.8f },
                    new[] { 3.0f, 3.3f, 3.6f, 4.0f, 4.5f },
                    new[] { 0, 0, 0, 0, 0 },
                    new[]
                    {
                        "Lob 200% damage at the biggest pack, every 7 s",
                        "Lob 240% damage at the biggest pack, every 6.5 s",
                        "Lob 280% damage at the biggest pack, every 6 s",
                        "Lob 320% damage at the biggest pack, every 5.5 s",
                        "Lob 380% damage at the biggest pack, every 5 s"
                    },
                    1.0f),

                Active("Ability_Lightning", "Chain Lightning", lightning,
                    new[] { 4.0f, 3.6f, 3.2f, 2.8f, 2.4f },
                    new[] { 0.9f, 1.05f, 1.2f, 1.35f, 1.5f },
                    new[] { 0f, 0f, 0f, 0f, 0f },
                    new[] { 3, 4, 5, 6, 8 },
                    new[]
                    {
                        "Arc 90% damage through 3 zombies, every 4 s",
                        "Arc 105% damage through 4 zombies, every 3.6 s",
                        "Arc 120% damage through 5 zombies, every 3.2 s",
                        "Arc 135% damage through 6 zombies, every 2.8 s",
                        "Arc 150% damage through 8 zombies, every 2.4 s"
                    },
                    1.0f),

                Passive("Ability_Damage",    "Heavy Rounds", StatType.Damage,          ModifierKind.Percent, 0.15f, 5, 1.2f, "damage"),
                Passive("Ability_FireRate",  "Trigger Work", StatType.FireRate,        ModifierKind.Percent, 0.12f, 5, 1.2f, "fire rate"),
                Passive("Ability_Pierce",    "Penetrator",   StatType.Pierce,          ModifierKind.Flat,    1f,    5, 0.9f, "pierce"),
                Passive("Ability_Multishot", "Split Barrel", StatType.ProjectileCount, ModifierKind.Flat,    0.5f,  5, 0.7f, "rounds per shot"),

                // Deliberately the rarest card in the pool. Radius compounds with
                // every other stat and is the one upgrade a player can literally
                // see working - common range cards flatten the difficulty curve.
                Passive("Ability_Range",     "Long Sight",   StatType.Range,           ModifierKind.Flat,    1f,    5, 0.4f, "m radius")
            };

            // Weight 0 keeps this out of the normal weighted draw; it is only ever
            // reached through the library's explicit fallback slot, when every
            // owned ability is maxed.
            AbilityDefinition patch = Passive("Ability_Patch", "Field Dressing",
                StatType.MaxHP, ModifierKind.Flat, 20f, 99, 0f, "max HP");

            AbilityLibrary library = Asset<AbilityLibrary>("AbilityLibrary");
            library.abilities = abilities;
            library.fallback = patch;
            library.maxDistinctAbilities = 6;

            EditorUtility.SetDirty(library);
            return library;
        }
    }
}
