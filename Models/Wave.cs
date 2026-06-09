using System.Collections.Generic;

namespace TowerDefense.Models
{
    /// <summary>
    /// Defines a single enemy wave.
    /// </summary>
    public class WaveDefinition
    {
        public List<(EnemyType Type, int Count, float Interval)> Groups { get; set; }
            = new List<(EnemyType, int, float)>();
    }

    /// <summary>
    /// Manages the current wave: spawns enemies on a schedule.
    /// </summary>
    public class Wave
    {
        // ── State ─────────────────────────────────────────────────────────────
        public int WaveNumber { get; private set; }
        public bool IsFinished { get; private set; } = false;
        public bool AllSpawned { get; private set; } = false;

        // ── Spawn queue ───────────────────────────────────────────────────────
        private WaveDefinition _definition;
        private List<(EnemyType Type, int Count, float Interval)> _spawnQueue;
        private int _groupIndex = 0;
        private int _spawnedInGroup = 0;
        private float _timer = 0f;

        public Wave(int number, WaveDefinition definition)
        {
            WaveNumber = number;
            _definition = definition;
            _spawnQueue = new List<(EnemyType, int, float)>(definition.Groups);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Update

        /// <summary>
        /// Advances the timer and returns the enemy type to spawn (or null).
        /// </summary>
        public EnemyType? Update(float deltaTime)
        {
            if (AllSpawned) return null;

            // ── All groups exhausted ──────────────────────────────────────────
            if (_groupIndex >= _spawnQueue.Count) { AllSpawned = true; return null; }

            _timer += deltaTime;
            var (type, count, interval) = _spawnQueue[_groupIndex];

            if (_timer >= interval)
            {
                _timer = 0f;
                _spawnedInGroup++;

                // ── Advance to next group when current is done ────────────────
                if (_spawnedInGroup >= count)
                {
                    _spawnedInGroup = 0;
                    _groupIndex++;
                }
                return type;
            }
            return null;
        }

        public void MarkFinished() => IsFinished = true;
    }
}