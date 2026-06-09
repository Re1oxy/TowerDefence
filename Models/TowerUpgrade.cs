namespace TowerDefense.Models
{
    /// <summary>
    /// Tower targeting priority modes selectable per tower.
    /// </summary>
    public enum TargetPriority
    {
        First,   // Enemy furthest along the path
        Weakest, // Enemy with lowest current HP
        Strongest // Enemy with highest current HP
    }

    /// <summary>
    /// Holds stat multipliers for a single upgrade level.
    /// Level 1 = base, Level 2 = first upgrade, Level 3 = second upgrade.
    /// </summary>
    public class UpgradeLevel
    {
        public int GoldCost { get; set; }
        public float DamageMult { get; set; }
        public float RangeMult { get; set; }
        public float FireRateMult { get; set; }
        public string Label { get; set; }
    }

    /// <summary>
    /// Defines upgrade levels for a tower type.
    /// Index 0 = upgrade to lvl 2, Index 1 = upgrade to lvl 3.
    /// </summary>
    public static class TowerUpgradeData
    {
        public static UpgradeLevel[] GetUpgrades(TowerType type)
        {
            switch (type)
            {
                case TowerType.Archer:
                    return new[]
                    {
                        new UpgradeLevel { GoldCost = 40,  DamageMult = 1.5f, RangeMult = 1.1f, FireRateMult = 1.3f, Label = "Lv2: Sharp Arrows" },
                        new UpgradeLevel { GoldCost = 80,  DamageMult = 2.5f, RangeMult = 1.2f, FireRateMult = 1.6f, Label = "Lv3: Master Archer" }
                    };

                case TowerType.Mage:
                    return new[]
                    {
                        new UpgradeLevel { GoldCost = 75,  DamageMult = 1.6f, RangeMult = 1.2f, FireRateMult = 1.2f, Label = "Lv2: Arcane Focus" },
                        new UpgradeLevel { GoldCost = 150, DamageMult = 2.8f, RangeMult = 1.4f, FireRateMult = 1.5f, Label = "Lv3: Archmage" }
                    };

                case TowerType.Catapult:
                    return new[]
                    {
                        new UpgradeLevel { GoldCost = 100, DamageMult = 1.7f, RangeMult = 1.15f, FireRateMult = 1.2f, Label = "Lv2: Heavy Boulder" },
                        new UpgradeLevel { GoldCost = 200, DamageMult = 3.0f, RangeMult = 1.3f,  FireRateMult = 1.4f, Label = "Lv3: Siege Engine" }
                    };

                default:
                    return new UpgradeLevel[0];
            }
        }
    }
}
