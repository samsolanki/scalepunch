using System.IO;
using UnityEditor;
using UnityEngine;
using ScalePunch.Meta;
using ScalePunch.Save;

namespace ScalePunch.EditorTools
{
    /// <summary>
    /// Profile inspection and reset from the editor.
    ///
    /// Balancing an economy means starting from zero over and over, and a
    /// persistent profile makes every run after the first one start from the
    /// wrong place. Without a one-click wipe, people test on a polluted save and
    /// draw the wrong conclusions from it.
    /// </summary>
    public static class SaveDebugMenu
    {
        [MenuItem("ScalePunch/Save/Log Profile", false, 100)]
        public static void LogProfile()
        {
            SaveService.Load();
            SaveData d = SaveService.Current;

            Debug.Log($"[Save] v{d.version} | coins {d.coins} gems {d.gems} scrap {d.scrap}\n" +
                      $"runs {d.totalRuns} ({d.totalVictories} won) | kills {d.totalKills} | " +
                      $"stage {d.highestStageCleared} | lifetime coins {d.lifetimeCoinsEarned}\n" +
                      $"last seen {d.lastSeenUtc}\n{SaveService.ProfilePath}" +
                      (SaveService.WasTampered ? "\nCHECKSUM MISMATCH — edited outside the game" : ""));
        }

        [MenuItem("ScalePunch/Save/Reveal Profile Folder", false, 101)]
        public static void Reveal() => EditorUtility.RevealInFinder(SaveService.ProfilePath);

        [MenuItem("ScalePunch/Save/Delete Profile", false, 102)]
        public static void DeleteProfile()
        {
            if (!EditorUtility.DisplayDialog(
                    "Delete profile?",
                    $"Permanently wipes coins, gems, scrap and all progress.\n\n{SaveService.ProfilePath}",
                    "Delete", "Cancel"))
                return;

            SaveService.Delete();
        }

        [MenuItem("ScalePunch/Save/Grant 10,000 Coins", false, 120)]
        public static void GrantCoins()
        {
            SaveService.Load();
            CurrencyService.Earn(CurrencyType.Coins, 10_000, "editor_debug");
            SaveService.Save();

            Debug.Log($"[Save] Coins now {CurrencyService.Coins}.");
        }

        [MenuItem("ScalePunch/Save/Delete Profile", true)]
        public static bool DeleteProfileValidate() => File.Exists(SaveService.ProfilePath);
    }
}
