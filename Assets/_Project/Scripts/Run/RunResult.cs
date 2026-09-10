namespace ScalePunch.Run
{
    public enum RunOutcome { InProgress, Victory, Defeat }

    /// <summary>
    /// Everything one run produced. Handed to the end screen for display and,
    /// at P2, to the currency service for the payout.
    /// </summary>
    public readonly struct RunResult
    {
        public readonly RunOutcome Outcome;
        public readonly float Duration;
        public readonly int WavesSurvived;
        public readonly int TotalWaves;
        public readonly int Kills;
        public readonly int Level;
        /// <summary>Coins this run paid out.</summary>
        public readonly int Coins;
        /// <summary>Balance after the payout was banked. Carried on the result so
        /// the end screen cannot accidentally read the balance before the
        /// deposit and show a stale total.</summary>
        public readonly long CoinBalance;

        public bool IsVictory => Outcome == RunOutcome.Victory;

        public RunResult(RunOutcome outcome, float duration, int wavesSurvived, int totalWaves,
                         int kills, int level, int coins, long coinBalance)
        {
            Outcome = outcome;
            Duration = duration;
            WavesSurvived = wavesSurvived;
            TotalWaves = totalWaves;
            Kills = kills;
            Level = level;
            Coins = coins;
            CoinBalance = coinBalance;
        }
    }
}
