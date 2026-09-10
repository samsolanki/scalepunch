using UnityEngine;

namespace ScalePunch.Save
{
    /// <summary>
    /// Upgrades old saves to the current shape.
    ///
    /// This exists on day one, before there is anything to migrate, on purpose:
    /// the first time a shipped save format changes without a migration path,
    /// every existing player's profile is wiped, and there is no way to undo it
    /// after the build is live.
    ///
    /// To add a version: bump <see cref="CurrentVersion"/>, add a case for the
    /// version you are migrating *from*, and never edit an existing step —
    /// players arrive here from every version you have ever shipped.
    /// </summary>
    public static class SaveMigrations
    {
        public const int CurrentVersion = 1;

        /// <summary>Returns true if anything changed and the save should be
        /// rewritten to disk.</summary>
        public static bool Apply(SaveData data)
        {
            if (data == null) return false;

            if (data.version > CurrentVersion)
            {
                // A profile written by a newer build — usually a player who
                // downgraded, or a TestFlight/internal tester switching tracks.
                // Loading it as-is may drop fields this build does not know, but
                // wiping it is strictly worse.
                Debug.LogWarning($"[Save] Profile is version {data.version}, this build " +
                                 $"understands {CurrentVersion}. Loading as-is; fields from " +
                                 "the newer build will not round-trip.");
                return false;
            }

            if (data.version == CurrentVersion) return false;

            int from = data.version;

            while (data.version < CurrentVersion)
            {
                switch (data.version)
                {
                    case 0:
                        MigrateV0ToV1(data);
                        break;

                    default:
                        // A gap in the chain is a bug, not a player problem.
                        // Stamp forward so they can keep playing, and shout.
                        Debug.LogError($"[Save] No migration from version {data.version}. " +
                                       "Stamping to current — data may be inconsistent.");
                        data.version = CurrentVersion;
                        break;
                }
            }

            Debug.Log($"[Save] Migrated profile {from} -> {data.version}.");
            return true;
        }

        /// <summary>
        /// Pre-versioning saves. Nothing shipped before v1, so this only has to
        /// stamp the version and repair fields that default badly.
        /// </summary>
        static void MigrateV0ToV1(SaveData data)
        {
            if (string.IsNullOrEmpty(data.lastSeenUtc)) data.StampSeen();

            // A negative balance is unspendable and unrecoverable; clamp rather
            // than trusting a value that should never have been written.
            // System.Math, not Mathf: Mathf has no long overload, so this would
            // silently truncate any balance above 2.1 billion on its way through
            // int — destroying exactly the large saves it was meant to repair.
            data.coins = System.Math.Max(0L, data.coins);
            data.gems = System.Math.Max(0L, data.gems);
            data.scrap = System.Math.Max(0L, data.scrap);

            data.version = 1;
        }
    }
}
