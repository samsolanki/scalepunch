using System;
using UnityEngine;
using ScalePunch.Save;

namespace ScalePunch.Meta
{
    /// <summary>
    /// The only thing allowed to change a balance.
    ///
    /// Static for the same reason SaveService is: balances outlive scenes.
    /// Everything routes through Earn/Spend so that when analytics arrives at
    /// M4, a single pair of methods covers every source and sink in the game —
    /// and the source/sink ratio is the only way to catch coin inflation before
    /// it makes the base upgrades trivial (docs/04 §4).
    /// </summary>
    public static class CurrencyService
    {
        /// <summary>Currency, new balance, signed delta.</summary>
        public static event Action<CurrencyType, long, long> Changed;

        static SaveData Data => SaveService.Current;

        public static long Get(CurrencyType currency) => currency switch
        {
            CurrencyType.Coins => Data.coins,
            CurrencyType.Gems => Data.gems,
            CurrencyType.Scrap => Data.scrap,
            _ => 0L
        };

        public static long Coins => Data.coins;
        public static long Gems => Data.gems;
        public static long Scrap => Data.scrap;

        /// <summary>
        /// Adds currency. <paramref name="reason"/> is not decoration — it is the
        /// analytics source tag, and an untagged source is invisible in the
        /// economy funnel.
        /// </summary>
        public static void Earn(CurrencyType currency, long amount, string reason)
        {
            if (amount <= 0) return;

            long balance = Get(currency);

            // Saturate rather than wrap. A player who somehow reaches the ceiling
            // should stall there, not wake up with a negative balance.
            long updated = balance > long.MaxValue - amount ? long.MaxValue : balance + amount;

            Set(currency, updated);

            if (currency == CurrencyType.Coins)
            {
                // Saturates like the balance above. Lifetime totals only ever grow,
                // so this is the counter most likely to reach the ceiling first.
                Data.lifetimeCoinsEarned = Data.lifetimeCoinsEarned > long.MaxValue - amount
                    ? long.MaxValue
                    : Data.lifetimeCoinsEarned + amount;
            }

            Changed?.Invoke(currency, updated, amount);
        }

        /// <summary>Returns false and changes nothing if the player cannot afford
        /// it. Callers must check — never assume a spend succeeded.</summary>
        public static bool Spend(CurrencyType currency, long amount, string reason)
        {
            if (amount <= 0) return true;

            long balance = Get(currency);
            if (balance < amount) return false;

            long updated = balance - amount;
            Set(currency, updated);

            Changed?.Invoke(currency, updated, -amount);
            return true;
        }

        public static bool CanAfford(CurrencyType currency, long amount) => Get(currency) >= amount;

        static void Set(CurrencyType currency, long value)
        {
            switch (currency)
            {
                case CurrencyType.Coins: Data.coins = value; break;
                case CurrencyType.Gems: Data.gems = value; break;
                case CurrencyType.Scrap: Data.scrap = value; break;
            }
        }

        /// <summary>Formats a balance for UI: 1.2K, 3.4M, 5.6B.</summary>
        public static string Format(long value)
        {
            if (value < 1000) return value.ToString();
            if (value < 1_000_000) return $"{value / 1000f:0.#}K";
            if (value < 1_000_000_000) return $"{value / 1_000_000f:0.#}M";

            return $"{value / 1_000_000_000f:0.#}B";
        }
    }
}
