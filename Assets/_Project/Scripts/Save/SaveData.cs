using System;

namespace ScalePunch.Save
{
    /// <summary>
    /// The entire persisted profile. One flat serialisable object, because
    /// JsonUtility only handles public fields on [Serializable] types and a flat
    /// shape is far easier to migrate than a nested one.
    ///
    /// **Adding a field is free. Renaming, removing, or changing the type of one
    /// is a migration** — see SaveMigrations. Do not skip that step because the
    /// change looks harmless in the editor: the editor has no old saves in it.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        /// <summary>Stamped by SaveMigrations. 0 means a save written before
        /// versioning existed, or a fresh profile.</summary>
        public int version;

        // Currencies are long, not int. An idle game with offline income and a
        // multiplier economy crosses 2.1 billion sooner than feels possible, and
        // an overflowed balance going negative is unrecoverable for the player.
        public long coins;
        public long gems;
        public long scrap;

        // ------------------------------------------------------------ progress
        public int highestStageCleared;
        public int totalRuns;
        public int totalVictories;
        public int totalKills;
        public long lifetimeCoinsEarned;

        /// <summary>ISO-8601 UTC. The Vault's offline income reads this at P6;
        /// stored as a string because JsonUtility cannot serialise DateTime.</summary>
        public string lastSeenUtc;

        public static SaveData NewProfile() => new()
        {
            version = SaveMigrations.CurrentVersion,
            lastSeenUtc = DateTime.UtcNow.ToString("o")
        };

        public DateTime LastSeen()
            => DateTime.TryParse(lastSeenUtc, null,
                                 System.Globalization.DateTimeStyles.RoundtripKind,
                                 out DateTime parsed)
                ? parsed
                : DateTime.UtcNow;

        public void StampSeen() => lastSeenUtc = DateTime.UtcNow.ToString("o");
    }
}
