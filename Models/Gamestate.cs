namespace TowerDefense.Models
{
    /// <summary>
    /// Game state: castle health, gold, score, wave number.
    /// </summary>
    public class GameState
    {
        // ── Properties ────────────────────────────────────────────────────────
        public int CastleHealth { get; private set; }
        public int MaxCastleHealth { get; private set; }
        public int Gold { get; private set; }
        public int Score { get; private set; }
        public int CurrentWave { get; private set; }
        public int TotalWaves { get; private set; }
        public bool IsGameOver { get; private set; }
        public bool IsVictory { get; private set; }

        public GameState(int totalWaves = 10, int startGold = 150, int castleHp = 20)
        {
            TotalWaves = totalWaves;
            Gold = startGold;
            CastleHealth = castleHp;
            MaxCastleHealth = castleHp;
            CurrentWave = 0;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Gold

        public void AddGold(int amount) => Gold += amount;

        public bool SpendGold(int amount)
        {
            if (Gold < amount) return false;
            Gold -= amount;
            return true;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Score / Castle / Wave

        public void AddScore(int amount) => Score += amount;

        public void DamageCastle(int amount)
        {
            CastleHealth -= amount;

            // ── Castle destroyed ──────────────────────────────────────────────
            if (CastleHealth <= 0)
            {
                CastleHealth = 0;
                IsGameOver = true;
            }
        }

        public void NextWave() => CurrentWave++;

        public void SetVictory()
        {
            IsVictory = true;
            IsGameOver = true;
        }
    }
}