using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using TowerDefense.Models;

namespace TowerDefense.Controllers
{
    /// <summary>
    /// Main game controller. Manages all game objects, wave logic,
    /// tower placement, upgrades, selling, and scoring.
    /// </summary>
    public class GameController
    {
        // ── Core ─────────────────────────────────────────────────────────────
        public GameMap Map { get; private set; }
        public GameState State { get; private set; }
        public GameConfig Config { get; private set; }

        // ── Game objects ──────────────────────────────────────────────────────
        public List<Tower> Towers { get; private set; } = new List<Tower>();
        public List<Enemy> Enemies { get; private set; } = new List<Enemy>();

        // ── Wave state ────────────────────────────────────────────────────────
        private List<WaveDefinition> _normalWaveDefs;
        private Wave _currentWave;
        private bool _waveActive = false;
        private bool _waitingForNextWave = true;
        private bool _bossSpawnedThisWave = false;

        // ── Damage indicators ─────────────────────────────────────────────────
        private List<DamageIndicator> _indicators = new List<DamageIndicator>();

        // ── Tower interaction ─────────────────────────────────────────────────
        public TowerType SelectedTowerType { get; set; } = TowerType.Archer;
        public Tower HoveredTower { get; private set; }
        public Tower SelectedTower { get; private set; }

        // ── Wave preview ──────────────────────────────────────────────────────
        public WaveDefinition NextWavePreview { get; private set; }
        public bool NextWaveIsBoss { get; private set; }

        // ── Events ────────────────────────────────────────────────────────────
        public event Action<string> OnMessage;
        public event Action OnGameOver;
        public event Action OnVictory;

        public GameController(GameConfig config)
        {
            Config = config;
            Map = new GameMap();
            State = new GameState(config.TotalWaves, config.StartGold, config.CastleHp);
            _normalWaveDefs = WaveManager.BuildNormalWaves();
            UpdateNextWavePreview();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Update

        public void Update(float deltaTime)
        {
            if (State.IsGameOver) return;

            UpdateWave(deltaTime);
            UpdateEnemies(deltaTime);

            // ── Tick all towers ───────────────────────────────────────────────
            foreach (var t in Towers)
                t.Update(deltaTime);

            // ── Auto-start next wave when countdown expires ───────────────────
            if (State.TickCountdown(deltaTime))
                StartNextWave();

            // ── Tick and clean up damage indicators ───────────────────────────
            foreach (var ind in _indicators) ind.Update(deltaTime);
            _indicators.RemoveAll(i => i.IsDone);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Wave logic

        private void UpdateWave(float deltaTime)
        {
            if (!_waveActive || _currentWave == null) return;

            // ── Spawn next enemy from schedule ────────────────────────────────
            var toSpawn = _currentWave.Update(deltaTime);
            if (toSpawn.HasValue)
                SpawnEnemy(toSpawn.Value);

            // ── Boss wave: spawn boss after all regular enemies are queued ─────
            bool isBossWave = IsBossWave(State.CurrentWave);
            if (isBossWave && _currentWave.AllSpawned && !_bossSpawnedThisWave)
            {
                SpawnBoss();
                _bossSpawnedThisWave = true;
            }

            // ── Wave ends when all enemies are spawned and cleared ────────────
            if (_currentWave.AllSpawned && _bossSpawnedOrNotNeeded() && Enemies.Count == 0)
            {
                _currentWave.MarkFinished();
                _waveActive = false;
                _waitingForNextWave = true;
                UpdateNextWavePreview();

                // ── Check for Normal mode victory (wave 10 cleared) ───────────
                bool isLastNormal = Config.GameMode == GameMode.Normal
                                    && State.CurrentWave >= 10;
                if (isLastNormal)
                {
                    State.SetVictory();
                    OnVictory?.Invoke();
                }
                else
                {
                    OnMessage?.Invoke($"Wave {State.CurrentWave} complete! Next in 5s...");
                    State.StartCountdown(5f);
                }
            }
        }

        // ── Returns true if boss is not required or already spawned ───────────
        private bool _bossSpawnedOrNotNeeded() =>
            !IsBossWave(State.CurrentWave) || _bossSpawnedThisWave;

        // ── Every 10th wave is a boss wave ────────────────────────────────────
        private bool IsBossWave(int wave) =>
            wave > 0 && wave % 10 == 0;

        private void UpdateEnemies(float deltaTime)
        {
            foreach (var e in Enemies)
                e.Update(deltaTime);

            // ── Award gold and score for each kill ────────────────────────────
            var justKilled = Enemies.Where(e => !e.IsAlive && !e.ReachedEnd).ToList();
            foreach (var e in justKilled)
            {
                State.AddGold(e.Reward);
                State.AddScore(e.Reward * 10);
                State.RegisterKill();
            }

            // ── Deal castle damage for enemies that reached the end ───────────
            foreach (var e in Enemies.Where(e => e.ReachedEnd))
            {
                State.DamageCastle(e.Damage);
                State.RegisterLeak();
                if (State.IsGameOver) { OnGameOver?.Invoke(); return; }
            }

            // ── Start 5s countdown when only the last enemy remains ───────────
            if (_waveActive && _currentWave.AllSpawned
                && Enemies.Count == 2
                && !State.CountdownActive)
            {
                // Countdown starts when only 1 enemy remains (penultimate killed)
            }
            if (_waveActive && _currentWave.AllSpawned
                && Enemies.Count(e => e.IsAlive) == 1
                && !State.CountdownActive)
            {
                // Last enemy alive — start 5s timer
                State.StartCountdown(5f);
            }

            Enemies.RemoveAll(e => !e.IsAlive);
        }

        private void SpawnEnemy(EnemyType type)
        {
            var spawn = Map.SpawnPoint;
            float sx = spawn.X * GameMap.CellSize;
            float sy = spawn.Y * GameMap.CellSize;

            // ── Apply difficulty multipliers from config ───────────────────────
            float hm = Config.EnemyHpMult;
            float sm = Config.EnemySpeedMult;

            Enemy enemy = type switch
            {
                EnemyType.Orc => new Orc(sx, sy, Map.EnemyPath, hm, sm),
                EnemyType.Troll => new Troll(sx, sy, Map.EnemyPath, hm, sm),
                EnemyType.Dragon => new Dragon(sx, sy, Map.EnemyPath, hm, sm),
                _ => new Orc(sx, sy, Map.EnemyPath, hm, sm)
            };

            Enemies.Add(enemy);
        }

        private void SpawnBoss()
        {
            var spawn = Map.SpawnPoint;
            float sx = spawn.X * GameMap.CellSize;
            float sy = spawn.Y * GameMap.CellSize;

            // ── Boss HP scales with wave number and difficulty ─────────────────
            float mult = WaveManager.BossHpMultiplier(State.CurrentWave)
                         * Config.EnemyHpMult;

            Enemies.Add(new Boss(sx, sy, Map.EnemyPath, mult));
            OnMessage?.Invoke("BOSS incoming!");
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Wave start

        // ── True only between waves and before game ends ──────────────────────
        public bool CanStartNextWave =>
            _waitingForNextWave && !State.IsGameOver &&
            (Config.GameMode == GameMode.Endless || State.CurrentWave < 10);

        public void StartNextWave()
        {
            if (!CanStartNextWave) return;

            State.StopCountdown();
            State.NextWave();
            _bossSpawnedThisWave = false;

            // ── Pick wave definition: fixed for Normal, generated for Endless ──
            WaveDefinition def = Config.GameMode == GameMode.Normal
                ? _normalWaveDefs[State.CurrentWave - 1]
                : WaveManager.BuildEndlessWave(State.CurrentWave);

            _currentWave = new Wave(State.CurrentWave, def);
            _waveActive = true;
            _waitingForNextWave = false;

            string bossNote = IsBossWave(State.CurrentWave) ? " [BOSS]" : "";
            OnMessage?.Invoke($"Wave {State.CurrentWave}{bossNote} started!");
            UpdateNextWavePreview();
        }

        private void UpdateNextWavePreview()
        {
            int next = State.CurrentWave + 1;

            // ── No preview past wave 10 in Normal mode ────────────────────────
            if (Config.GameMode == GameMode.Normal && next > 10)
            { NextWavePreview = null; NextWaveIsBoss = false; return; }

            NextWavePreview = Config.GameMode == GameMode.Normal
                ? _normalWaveDefs[Math.Min(next - 1, 9)]
                : WaveManager.BuildEndlessWave(next);

            NextWaveIsBoss = IsBossWave(next);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Tower placement

        public bool TryPlaceTower(int pixelX, int pixelY)
        {
            int col = pixelX / GameMap.CellSize;
            int row = pixelY / GameMap.CellSize;

            // ── Reject non-grass cells ────────────────────────────────────────
            if (!Map.CanPlaceTower(col, row)) return false;

            // ── Reject if player can't afford it ──────────────────────────────
            int cost = GetTowerCost(SelectedTowerType);
            if (!State.SpendGold(cost))
            { OnMessage?.Invoke("Not enough gold!"); return false; }

            float tx = col * GameMap.CellSize;
            float ty = row * GameMap.CellSize;

            Tower tower = SelectedTowerType switch
            {
                TowerType.Archer => new ArcherTower(tx, ty, Enemies),
                TowerType.Mage => new MageTower(tx, ty, Enemies),
                TowerType.Catapult => new CatapultTower(tx, ty, Enemies),
                _ => new ArcherTower(tx, ty, Enemies)
            };

            Towers.Add(tower);

            // ── Mark cell as blocked so enemies can't path through it ─────────
            Map.Grid[col, row] = CellType.Path;
            return true;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Tower selling

        public bool TrySellTower(Tower tower)
        {
            if (tower == null || !Towers.Contains(tower)) return false;

            State.AddGold(tower.SellValue);
            OnMessage?.Invoke($"Sold for {tower.SellValue}g");

            // ── Restore the cell to grass so new towers can be placed ─────────
            int col = (int)(tower.X / GameMap.CellSize);
            int row = (int)(tower.Y / GameMap.CellSize);
            Map.Grid[col, row] = CellType.Grass;

            Towers.Remove(tower);
            if (SelectedTower == tower) SelectedTower = null;
            return true;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Tower upgrading

        public bool TryUpgradeTower(Tower tower)
        {
            if (tower == null) return false;

            int maxLvl = Config.MaxUpgradeLevel;

            // ── Upgrades disabled in this game mode ───────────────────────────
            if (maxLvl <= 1) { OnMessage?.Invoke("Upgrades not available in this mode"); return false; }

            int cost = tower.GetUpgradeCost(maxLvl);

            // ── Tower already at max level ────────────────────────────────────
            if (cost == 0) { OnMessage?.Invoke("Tower is already max level"); return false; }

            if (!State.SpendGold(cost))
            { OnMessage?.Invoke("Not enough gold!"); return false; }

            tower.Upgrade(maxLvl);
            OnMessage?.Invoke($"Tower upgraded to level {tower.Level}!");
            return true;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Tower selection (click)

        // ── Returns the tower occupying the given pixel, or null ──────────────
        public Tower GetTowerAt(int pixelX, int pixelY) =>
            Towers.FirstOrDefault(t =>
                pixelX >= t.X && pixelX < t.X + t.Width &&
                pixelY >= t.Y && pixelY < t.Y + t.Height);

        public void SelectTower(Tower tower) => SelectedTower = tower;
        public void DeselectTower() => SelectedTower = null;

        public void UpdateHover(int pixelX, int pixelY)
        {
            HoveredTower = Towers.FirstOrDefault(t =>
                pixelX >= t.X && pixelX < t.X + t.Width &&
                pixelY >= t.Y && pixelY < t.Y + t.Height);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Damage indicators

        public void SpawnDamageIndicator(float x, float y, int damage)
        {
            _indicators.Add(new DamageIndicator(x, y, damage));
        }

        public IEnumerable<DamageIndicator> GetIndicators() => _indicators;

        // ─────────────────────────────────────────────────────────────────────
        //  Helpers

        public int GetTowerCost(TowerType type) => type switch
        {
            TowerType.Archer => 50,
            TowerType.Mage => 100,
            TowerType.Catapult => 150,
            _ => 50
        };

        public string GetTowerDescription(TowerType type) => type switch
        {
            TowerType.Archer => "Archer    | 50g  | Fast, low damage",
            TowerType.Mage => "Mage      | 100g | Slow, high damage",
            TowerType.Catapult => "Catapult  | 150g | Very slow, massive damage",
            _ => ""
        };

        // ─────────────────────────────────────────────────────────────────────
        //  Drawing

        public void Draw(Graphics g)
        {
            Map.Draw(g);

            foreach (var t in Towers)
            {
                bool isHovered = t == HoveredTower;
                bool isSelected = t == SelectedTower;

                // ── Show range ring on hover or selection ─────────────────────
                if (isHovered || isSelected) t.DrawWithRange(g);
                else t.Draw(g);

                // ── Gold selection outline ────────────────────────────────────
                if (isSelected)
                {
                    using var pen = new Pen(Color.FromArgb(220, 255, 200, 0), 2);
                    g.DrawRectangle(pen, t.X, t.Y, t.Width, t.Height);
                }

                foreach (var p in t.Projectiles) p.Draw(g);
            }

            foreach (var e in Enemies) e.Draw(g);

            // ── Floating damage numbers ───────────────────────────────────────
            foreach (var ind in _indicators) ind.Draw(g);
        }
    }

    // ── Damage indicator ──────────────────────────────────────────────────────
    /// <summary>
    /// Floating damage number shown above an enemy when hit.
    /// </summary>
    public class DamageIndicator
    {
        // ── State ─────────────────────────────────────────────────────────────
        private float _x, _y;
        private int _damage;
        private float _life = 0.8f;   // total display duration in seconds
        private float _elapsed;
        public bool IsDone => _elapsed >= _life;

        public DamageIndicator(float x, float y, int damage)
        { _x = x; _y = y - 10; _damage = damage; }

        public void Update(float dt) => _elapsed += dt;

        public void Draw(Graphics g)
        {
            // ── Fade out and rise over lifetime ───────────────────────────────
            float alpha = 1f - (_elapsed / _life);
            float rise = _elapsed * 30f;
            using var font = new Font("Arial", 8, FontStyle.Bold);
            using var brush = new SolidBrush(Color.FromArgb((int)(alpha * 255), 255, 80, 80));
            g.DrawString($"-{_damage}", font, brush, _x, _y - rise);
        }
    }
}