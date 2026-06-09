using System;
using System.Collections.Generic;
using TowerDefense.Models;

namespace TowerDefense.Controllers
{
    /// <summary>
    /// Builds wave definitions for both Normal (10 waves) and
    /// Endless modes. Endless waves scale in difficulty every wave.
    /// </summary>
    public static class WaveManager
    {
        // -- Normal mode: 10 fixed waves + boss on wave 10 ----------------------
        public static List<WaveDefinition> BuildNormalWaves()
        {
            return new List<WaveDefinition>
            {
                // Wave 1
                new WaveDefinition { Groups = { (EnemyType.Orc, 5, 1.2f) }},
                // Wave 2
                new WaveDefinition { Groups = { (EnemyType.Orc, 8, 0.9f) }},
                // Wave 3
                new WaveDefinition { Groups = { (EnemyType.Orc, 6, 0.8f), (EnemyType.Troll, 2, 2.0f) }},
                // Wave 4
                new WaveDefinition { Groups = { (EnemyType.Orc, 10, 0.7f), (EnemyType.Troll, 3, 1.8f) }},
                // Wave 5
                new WaveDefinition { Groups = { (EnemyType.Orc, 8, 0.6f), (EnemyType.Troll, 2, 1.5f), (EnemyType.Dragon, 1, 3.0f) }},
                // Wave 6
                new WaveDefinition { Groups = { (EnemyType.Orc, 12, 0.5f), (EnemyType.Troll, 4, 1.3f), (EnemyType.Dragon, 2, 2.5f) }},
                // Wave 7
                new WaveDefinition { Groups = { (EnemyType.Troll, 6, 1.2f), (EnemyType.Dragon, 3, 2.0f) }},
                // Wave 8
                new WaveDefinition { Groups = { (EnemyType.Orc, 15, 0.4f), (EnemyType.Troll, 5, 1.0f), (EnemyType.Dragon, 4, 1.8f) }},
                // Wave 9
                new WaveDefinition { Groups = { (EnemyType.Troll, 8, 0.9f), (EnemyType.Dragon, 5, 1.5f) }},
                // Wave 10 — boss wave (boss spawned separately by GameController)
                new WaveDefinition { Groups = { (EnemyType.Orc, 10, 0.4f), (EnemyType.Dragon, 4, 1.2f) }},
            };
        }

        // -- Endless mode: generate wave N on demand ----------------------------
        /// <summary>
        /// Generates a wave definition for the given wave number.
        /// Scales enemy count, speed interval and type mix with wave number.
        /// Every 10th wave is a boss wave (boss spawned by GameController).
        /// </summary>
        public static WaveDefinition BuildEndlessWave(int waveNumber)
        {
            var def = new WaveDefinition();

            float scale = 1f + (waveNumber - 1) * 0.15f;
            float interval = Math.Max(0.25f, 1.2f - waveNumber * 0.05f);

            int orcs = (int)(4 * scale);
            int trolls = (int)(2 * scale);
            int dragons = (int)(1 * scale);

            if (waveNumber <= 3)
            {
                // Early waves: orcs only
                def.Groups.Add((EnemyType.Orc, orcs, interval));
            }
            else if (waveNumber <= 6)
            {
                def.Groups.Add((EnemyType.Orc, orcs, interval));
                def.Groups.Add((EnemyType.Troll, trolls, interval * 1.5f));
            }
            else
            {
                def.Groups.Add((EnemyType.Orc, orcs, interval));
                def.Groups.Add((EnemyType.Troll, trolls, interval * 1.4f));
                def.Groups.Add((EnemyType.Dragon, dragons, interval * 2.0f));
            }

            return def;
        }

        // -- Boss HP multiplier grows every 10 waves ----------------------------
        public static float BossHpMultiplier(int waveNumber)
        {
            int bossCount = waveNumber / 10;
            return 1f + (bossCount - 1) * 0.5f;
        }
    }
}
