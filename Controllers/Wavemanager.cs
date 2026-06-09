using System.Collections.Generic;
using TowerDefense.Models;

namespace TowerDefense.Controllers
{
    /// <summary>
    /// Wave factory — builds all 10 wave definitions with increasing difficulty.
    /// </summary>
    public static class WaveManager
    {
        public static List<WaveDefinition> BuildWaves()
        {
            return new List<WaveDefinition>
            {
                // Wave 1 — orcs only
                new WaveDefinition { Groups = {
                    (EnemyType.Orc, 5, 1.2f)
                }},
                // Wave 2
                new WaveDefinition { Groups = {
                    (EnemyType.Orc, 8, 0.9f)
                }},
                // Wave 3 — orcs + troll
                new WaveDefinition { Groups = {
                    (EnemyType.Orc, 6, 0.8f),
                    (EnemyType.Troll, 2, 2.0f)
                }},
                // Wave 4
                new WaveDefinition { Groups = {
                    (EnemyType.Orc, 10, 0.7f),
                    (EnemyType.Troll, 3, 1.8f)
                }},
                // Wave 5 — first dragon!
                new WaveDefinition { Groups = {
                    (EnemyType.Orc, 8, 0.6f),
                    (EnemyType.Troll, 2, 1.5f),
                    (EnemyType.Dragon, 1, 3.0f)
                }},
                // Wave 6
                new WaveDefinition { Groups = {
                    (EnemyType.Orc, 12, 0.5f),
                    (EnemyType.Troll, 4, 1.3f),
                    (EnemyType.Dragon, 2, 2.5f)
                }},
                // Wave 7
                new WaveDefinition { Groups = {
                    (EnemyType.Troll, 6, 1.2f),
                    (EnemyType.Dragon, 3, 2.0f)
                }},
                // Wave 8
                new WaveDefinition { Groups = {
                    (EnemyType.Orc, 15, 0.4f),
                    (EnemyType.Troll, 5, 1.0f),
                    (EnemyType.Dragon, 4, 1.8f)
                }},
                // Wave 9
                new WaveDefinition { Groups = {
                    (EnemyType.Troll, 8, 0.9f),
                    (EnemyType.Dragon, 5, 1.5f)
                }},
                // Wave 10 — final!
                new WaveDefinition { Groups = {
                    (EnemyType.Orc, 20, 0.3f),
                    (EnemyType.Troll, 10, 0.8f),
                    (EnemyType.Dragon, 8, 1.2f)
                }},
            };
        }
    }
}