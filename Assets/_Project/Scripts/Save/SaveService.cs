using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ScalePunch.Save
{
    /// <summary>
    /// Loads and stores the player's profile.
    ///
    /// A static class, not a MonoBehaviour: the profile has to outlive every
    /// scene load, and a DontDestroyOnLoad singleton for something with no
    /// per-frame work is a lifetime problem with no upside.
    ///
    /// Writes are atomic and keep one backup. A profile lost to a crash
    /// mid-write is unrecoverable for that player and reliably becomes a
    /// one-star review.
    /// </summary>
    public static class SaveService
    {
        const string FileName = "profile.json";
        const string BackupName = "profile.bak";
        const string TempName = "profile.tmp";

        /// <summary>Not security. A client-side save can always be edited by
        /// someone determined; this only makes casual edits detectable so a
        /// tampered profile can be logged rather than silently trusted.</summary>
        const string ChecksumSalt = "scalepunch.v1";

        [Serializable]
        class Envelope
        {
            public string payload;
            public string checksum;
        }

        static SaveData _current;

        /// <summary>The live profile. Loads from disk on first access.</summary>
        public static SaveData Current
        {
            get
            {
                if (_current == null) Load();
                return _current;
            }
        }

        public static bool IsLoaded => _current != null;
        /// <summary>True if the last load found a checksum mismatch.</summary>
        public static bool WasTampered { get; private set; }

        /// <summary>Raised after any successful save, for anything that mirrors
        /// profile state.</summary>
        public static event Action<SaveData> Saved;

        static string Dir => Application.persistentDataPath;
        static string Path(string file) => System.IO.Path.Combine(Dir, file);

        // ------------------------------------------------------------ loading

        public static void Load()
        {
            WasTampered = false;

            _current = ReadFrom(Path(FileName)) ?? ReadFrom(Path(BackupName));

            if (_current == null)
            {
                _current = SaveData.NewProfile();
                Debug.Log("[Save] No profile found — starting a new one.");
                return;
            }

            if (SaveMigrations.Apply(_current)) Save();
        }

        static SaveData ReadFrom(string path)
        {
            if (!File.Exists(path)) return null;

            try
            {
                string raw = File.ReadAllText(path);
                Envelope envelope = JsonUtility.FromJson<Envelope>(raw);

                if (envelope == null || string.IsNullOrEmpty(envelope.payload))
                {
                    Debug.LogWarning($"[Save] '{System.IO.Path.GetFileName(path)}' is not a valid profile.");
                    return null;
                }

                if (Checksum(envelope.payload) != envelope.checksum)
                {
                    // Loaded anyway. Refusing a mismatched save would destroy real
                    // progress every time a hashing detail changes, which costs
                    // far more than the cheating it prevents.
                    WasTampered = true;
                    Debug.LogWarning("[Save] Checksum mismatch — profile was edited outside the game.");
                }

                return JsonUtility.FromJson<SaveData>(envelope.payload);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] Failed to read '{System.IO.Path.GetFileName(path)}': {e.Message}");
                return null;
            }
        }

        // ------------------------------------------------------------- saving

        public static void Save()
        {
            if (_current == null) return;

            _current.StampSeen();

            try
            {
                string payload = JsonUtility.ToJson(_current);
                string json = JsonUtility.ToJson(new Envelope
                {
                    payload = payload,
                    checksum = Checksum(payload)
                });

                // Write to a temp file, then swap. A process killed partway
                // through leaves the previous profile untouched instead of a
                // half-written one.
                string temp = Path(TempName);
                string target = Path(FileName);
                string backup = Path(BackupName);

                File.WriteAllText(temp, json);

                if (File.Exists(target))
                {
                    SwapIn(temp, target, backup);
                }
                else
                {
                    File.Move(temp, target);
                }

                Saved?.Invoke(_current);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] Write failed: {e.Message}");
            }
        }

        /// <summary>
        /// Promotes the temp file to be the profile, keeping the old one as the
        /// backup.
        ///
        /// File.Replace is the atomic path and the one to prefer, but it is not
        /// implemented on every runtime Unity targets and throws rather than
        /// degrading. The fallback is a normal copy-then-move: a wider window
        /// for a crash to land in, but still never leaves the target
        /// half-written, and it beats failing the save entirely.
        /// </summary>
        static void SwapIn(string temp, string target, string backup)
        {
            try
            {
                File.Replace(temp, target, backup);
                return;
            }
            catch (PlatformNotSupportedException) { }
            catch (NotSupportedException) { }
            catch (IOException) { }

            File.Copy(target, backup, true);
            File.Delete(target);
            File.Move(temp, target);
        }

        /// <summary>Wipes the profile. Debug and "reset progress" only.</summary>
        public static void Delete()
        {
            foreach (string file in new[] { FileName, BackupName, TempName })
            {
                string path = Path(file);
                if (File.Exists(path)) File.Delete(path);
            }

            _current = SaveData.NewProfile();
            Debug.Log("[Save] Profile deleted.");
        }

        public static string ProfilePath => Path(FileName);

        static string Checksum(string payload)
        {
            using var sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(payload + ChecksumSalt));

            var sb = new StringBuilder(hash.Length * 2);
            foreach (byte b in hash) sb.Append(b.ToString("x2"));

            return sb.ToString();
        }
    }
}
