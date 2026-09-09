using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

using ScalePunch.Abilities;
using ScalePunch.Abilities.Effects;
using ScalePunch.Combat;
using ScalePunch.Data;
using ScalePunch.Enemies;
using ScalePunch.Progression;
using ScalePunch.Weapons;

namespace ScalePunch.EditorTools
{
    public static partial class RunSceneBuilder
    {
        class MaterialSet
        {
            public Material ground, turret, shambler, runner, brute, bullet, gem, line;
        }

        class DataSet
        {
            public CombatTuning tuning;
            public LevelCurve curve;
            public WeaponDefinition pistol;
            public EnemyDefinition[] zombies;
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
            set.pistol.hitRadius = 0.35f;
            set.pistol.lifetime = 2.5f;
            set.pistol.damageMultiplier = 1f;
            set.pistol.fireRateMultiplier = 1f;
            set.pistol.rangeMultiplier = 1f;
            set.pistol.projectileSpeedMultiplier = 1f;
            set.pistol.extraProjectiles = 0;
            set.pistol.spreadDegrees = 0f;
            set.pistol.inaccuracyDegrees = 1.5f;
            set.pistol.steerDegreesPerSecond = 220f;

            set.zombies = BuildZombies();
            set.library = BuildAbilities();

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
                                      float scale, EnemyBehaviour behaviour)
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
            def.xpValue = xp;
            def.scale = scale;

            EditorUtility.SetDirty(def);
            return def;
        }

        static EnemyDefinition[] BuildZombies()
        {
            return new[]
            {
                Zombie("Zombie_Shambler", "shambler", 20f,  8f, 2.5f, 0f, 1, 1.00f, EnemyBehaviour.Shambler),
                Zombie("Zombie_Runner",   "runner",   10f,  6f, 4.5f, 0f, 1, 0.85f, EnemyBehaviour.Runner),

                // Brutes are knockback-immune. Without that, sustained fire
                // stunlocks them at the edge of the ring and they stop being a
                // DPS check at all.
                Zombie("Zombie_Brute",    "brute",    90f, 18f, 1.4f, 1f, 4, 1.45f, EnemyBehaviour.Brute)
            };
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
