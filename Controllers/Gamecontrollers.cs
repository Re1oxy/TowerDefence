using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using TowerDefense.Models;

namespace TowerDefense.Controllers
{
    /// <summary>
    /// Main game controller. Manages all game objects,
    /// wave logic, tower placement and scoring.
    /// </summary>
    public class GameController
    {
        public GameMap Map { get; private set; }
        public GameState State { get; private set; }

        public List<Tower> Towers { get; private set; } = new List<Tower>();
        public List<Enemy> Enemies { get; private set; } = new List<Enemy>();

        private List<WaveDefinition> _waveDefs;
        private Wave _currentWave;
        private bool _waveActive = false;
        private bool _waitingForNextWave = true;

        public TowerType SelectedTowerType { get; set; } = TowerType.Archer;
        public Tower HoveredTower { get; private set; }

        public event Action<string> OnMessage;
        public event Action OnGameOver;
        public event Action OnVictory;

        public GameController()
        {
            Map = new GameMap();
            State = new GameState(totalWaves: 10, startGold: 150, castleHp: 20);
            _waveDefs = WaveManager.BuildWaves();
        }

        // ── Game Update ──────────────────────────────────────────────────────
        public void Update(float deltaTime)
        {
            if (State.IsGameOver) return;

            UpdateWave(deltaTime);
            UpdateEnemies(deltaTime);

            foreach (var t in Towers)
                t.Update(deltaTime);
        }

        private void UpdateWave(float deltaTime)
        {
            if (!_waveActive || _currentWave == null) return;

            var toSpawn = _currentWave.Update(deltaTime);
            if (toSpawn.HasValue)
                SpawnEnemy(toSpawn.Value);

            // Wave ends when all spawned and all dead/reached end
            if (_currentWave.AllSpawned && Enemies.Count == 0)
            {
                _currentWave.MarkFinished();
                _waveActive = false;
                _waitingForNextWave = true;

                if (State.CurrentWave >= State.TotalWaves)
                {
                    State.SetVictory();
                    OnVictory?.Invoke();
                }
                else
                {
                    OnMessage?.Invoke($"Wave {State.CurrentWave} complete! Get ready...");
                }
            }
        }

        private void UpdateEnemies(float deltaTime)
        {
            foreach (var e in Enemies)
                e.Update(deltaTime);

            // Collect rewards for killed enemies (not those who reached the end)
            var justKilled = Enemies
                .Where(e => !e.IsAlive && !e.ReachedEnd)
                .ToList();
            ProcessKills(justKilled);

            // Damage castle for enemies that reached the end
            var reachedEnd = Enemies.Where(e => e.ReachedEnd).ToList();
            foreach (var e in reachedEnd)
            {
                State.DamageCastle(e.Damage);
                if (State.IsGameOver)
                {
                    OnGameOver?.Invoke();
                    return;
                }
            }

            Enemies.RemoveAll(e => !e.IsAlive);
        }

        private void SpawnEnemy(EnemyType type)
        {
            var spawn = Map.SpawnPoint;
            float sx = spawn.X * GameMap.CellSize;
            float sy = spawn.Y * GameMap.CellSize;

            Enemy enemy = type switch
            {
                EnemyType.Orc => new Orc(sx, sy, Map.EnemyPath),
                EnemyType.Troll => new Troll(sx, sy, Map.EnemyPath),
                EnemyType.Dragon => new Dragon(sx, sy, Map.EnemyPath),
                _ => new Orc(sx, sy, Map.EnemyPath)
            };

            Enemies.Add(enemy);
        }

        // ── Waves ────────────────────────────────────────────────────────────
        public bool CanStartNextWave =>
            _waitingForNextWave &&
            !State.IsGameOver &&
            State.CurrentWave < State.TotalWaves;

        public void StartNextWave()
        {
            if (!CanStartNextWave) return;
            State.NextWave();
            _currentWave = new Wave(State.CurrentWave, _waveDefs[State.CurrentWave - 1]);
            _waveActive = true;
            _waitingForNextWave = false;
            OnMessage?.Invoke($"Wave {State.CurrentWave} of {State.TotalWaves} started!");
        }

        // ── Towers ───────────────────────────────────────────────────────────
        public bool TryPlaceTower(int pixelX, int pixelY)
        {
            int col = pixelX / GameMap.CellSize;
            int row = pixelY / GameMap.CellSize;

            if (!Map.CanPlaceTower(col, row))
                return false;

            int cost = GetTowerCost(SelectedTowerType);
            if (!State.SpendGold(cost))
            {
                OnMessage?.Invoke("Not enough gold!");
                return false;
            }

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
            // Mark cell as occupied so nothing can be placed again
            Map.Grid[col, row] = CellType.Path;
            return true;
        }

        public void UpdateHover(int pixelX, int pixelY)
        {
            HoveredTower = Towers.FirstOrDefault(t =>
                pixelX >= t.X && pixelX < t.X + t.Width &&
                pixelY >= t.Y && pixelY < t.Y + t.Height);
        }

        public int GetTowerCost(TowerType type) => type switch
        {
            TowerType.Archer => 50,
            TowerType.Mage => 100,
            TowerType.Catapult => 150,
            _ => 50
        };

        public string GetTowerDescription(TowerType type) => type switch
        {
            TowerType.Archer => "Archer  |  50g  |  Fast, low damage",
            TowerType.Mage => "Mage    |  100g |  Slow, high damage",
            TowerType.Catapult => "Catapult|  150g |  Very slow, massive damage",
            _ => ""
        };

        // ── Kill Rewards ─────────────────────────────────────────────────────
        public void ProcessKills(List<Enemy> justKilled)
        {
            foreach (var e in justKilled)
            {
                State.AddGold(e.Reward);
                State.AddScore(e.Reward * 10);
            }
        }

        // ── Drawing ──────────────────────────────────────────────────────────
        public void Draw(Graphics g)
        {
            Map.Draw(g);

            foreach (var t in Towers)
            {
                if (t == HoveredTower) t.DrawWithRange(g);
                else t.Draw(g);

                foreach (var p in t.Projectiles)
                    p.Draw(g);
            }

            foreach (var e in Enemies)
                e.Draw(g);
        }
    }
}
