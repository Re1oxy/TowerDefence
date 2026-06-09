namespace TowerDefense.Models
{
    /// <summary>
    /// Tracks all runtime game state: health, gold, score,
    /// wave progress, countdown timer, and game-over flags.
    /// </summary>
    public class GameState
    {
        public int CastleHealth { get; private set; }
        public int MaxCastleHealth { get; private set; }
        public int Gold { get; private set; }
        public int Score { get; private set; }
        public int CurrentWave { get; private set; }
        public int TotalWaves { get; private set; }
        public int EnemiesKilled { get; private set; }
        public int EnemiesLeaked { get; private set; }
        public bool IsGameOver { get; private set; }
        public bool IsVictory { get; private set; }

        // -- Wave countdown timer ------------------------------------------------
        public bool CountdownActive { get; private set; }
        public float CountdownSeconds { get; private set; }

        public GameState(int totalWaves, int startGold, int castleHp)
        {
            TotalWaves = totalWaves;
            Gold = startGold;
            CastleHealth = castleHp;
            MaxCastleHealth = castleHp;
            CurrentWave = 0;
        }

        // -- Gold ----------------------------------------------------------------
        public void AddGold(int amount) => Gold += amount;

        public bool SpendGold(int amount)
        {
            if (Gold < amount) return false;
            Gold -= amount;
            return true;
        }

        // -- Score / kills -------------------------------------------------------
        public void AddScore(int amount) => Score += amount;
        public void RegisterKill() => EnemiesKilled++;
        public void RegisterLeak() => EnemiesLeaked++;

        // -- Castle --------------------------------------------------------------
        public void DamageCastle(int amount)
        {
            CastleHealth -= amount;
            if (CastleHealth <= 0)
            {
                CastleHealth = 0;
                IsGameOver = true;
            }
        }

        // -- Wave ----------------------------------------------------------------
        public void NextWave() => CurrentWave++;

        public void SetVictory()
        {
            IsVictory = true;
            IsGameOver = true;
        }

        // -- Countdown -----------------------------------------------------------
        /// <summary>Starts the between-wave countdown timer.</summary>
        public void StartCountdown(float seconds)
        {
            CountdownSeconds = seconds;
            CountdownActive = true;
        }

        /// <summary>
        /// Ticks the countdown. Returns true when it reaches zero.
        /// </summary>
        public bool TickCountdown(float deltaTime)
        {
            if (!CountdownActive) return false;

            CountdownSeconds -= deltaTime;
            if (CountdownSeconds <= 0f)
            {
                CountdownSeconds = 0f;
                CountdownActive = false;
                return true;
            }
            return false;
        }

        public void StopCountdown() => CountdownActive = false;
    }
}
