using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace TowerDefense.Models
{
    public enum TowerType { Archer, Mage, Catapult }

    /// <summary>
    /// Base tower class. Attacks the furthest enemy in range.
    /// </summary>
    public abstract class Tower : Entity
    {
        public TowerType TowerType { get; protected set; }
        public int Cost { get; protected set; }
        public float Range { get; protected set; }
        public int Damage { get; protected set; }
        public float FireRate { get; protected set; }
        public string Name { get; protected set; }
        public string Description { get; protected set; }

        protected float FireCooldown { get; private set; } = 0f;
        protected ProjectileType ProjectileType { get; set; }

        public List<Projectile> Projectiles { get; } = new List<Projectile>();

        protected Color TowerColor { get; set; }
        protected Color AccentColor { get; set; }

        protected Tower(float x, float y) : base(x, y, GameMap.CellSize, GameMap.CellSize) { }

        public override void Update(float deltaTime)
        {
            Projectiles.RemoveAll(p => !p.IsAlive);
            foreach (var p in Projectiles) p.Update(deltaTime);

            if (FireCooldown > 0) { FireCooldown -= deltaTime; return; }

            var target = FindTarget(GetEnemies());
            if (target != null)
            {
                Shoot(target);
                FireCooldown = 1f / FireRate;
            }
        }

        protected abstract List<Enemy> GetEnemies();

        protected virtual Enemy FindTarget(List<Enemy> enemies)
        {
            return enemies
                .Where(e => e.IsAlive && DistanceTo(e) <= Range)
                .OrderByDescending(e => e.DistanceTraveled)
                .FirstOrDefault();
        }

        protected virtual void Shoot(Enemy target)
        {
            float cx = X + Width / 2f;
            float cy = Y + Height / 2f;
            Projectiles.Add(new Projectile(cx, cy, target, Damage, ProjectileType));
        }

        protected float DistanceTo(Enemy e)
        {
            float dx = (e.X + e.Width / 2f) - (X + Width / 2f);
            float dy = (e.Y + e.Height / 2f) - (Y + Height / 2f);
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        public override void Draw(Graphics g)
        {
            DrawBase(g);
            DrawTowerTop(g);
        }

        public void DrawWithRange(Graphics g)
        {
            DrawBase(g);
            DrawTowerTop(g);
            DrawRange(g);
        }

        private void DrawBase(Graphics g)
        {
            // Stone base
            using var baseBrush = new SolidBrush(Color.FromArgb(90, 90, 90));
            g.FillRectangle(baseBrush, X + 2, Y + 2, Width - 4, Height - 4);

            // Tower body
            using var brush = new SolidBrush(TowerColor);
            g.FillRectangle(brush, X + 6, Y + 6, Width - 12, Height - 12);

            // Border
            using var pen = new Pen(AccentColor, 2);
            g.DrawRectangle(pen, X + 6, Y + 6, Width - 12, Height - 12);

            // Corner stones
            using var stoneBrush = new SolidBrush(Color.FromArgb(110, 100, 90));
            g.FillRectangle(stoneBrush, X + 2, Y + 2, 6, 6);
            g.FillRectangle(stoneBrush, X + Width - 8, Y + 2, 6, 6);
            g.FillRectangle(stoneBrush, X + 2, Y + Height - 8, 6, 6);
            g.FillRectangle(stoneBrush, X + Width - 8, Y + Height - 8, 6, 6);
        }

        private void DrawRange(Graphics g)
        {
            using var pen = new Pen(Color.FromArgb(60, AccentColor), 1);
            float cx = X + Width / 2f;
            float cy = Y + Height / 2f;
            g.DrawEllipse(pen, cx - Range, cy - Range, Range * 2, Range * 2);
            using var brush = new SolidBrush(Color.FromArgb(20, AccentColor));
            g.FillEllipse(brush, cx - Range, cy - Range, Range * 2, Range * 2);
        }

        protected abstract void DrawTowerTop(Graphics g);
    }

    // ── Archer Tower ─────────────────────────────────────────────────────────
    /// <summary>Archer Tower — cheap, fast, low damage.</summary>
    public class ArcherTower : Tower
    {
        private List<Enemy> _enemies;
        private float _aimAngle = 0f; // radians, 0 = right

        public ArcherTower(float x, float y, List<Enemy> enemies) : base(x, y)
        {
            _enemies = enemies;
            TowerType = TowerType.Archer;
            Name = "Archer";
            Description = "Fast attack, low damage";
            Cost = 50;
            Range = 120f;
            Damage = 15;
            FireRate = 2f;
            ProjectileType = ProjectileType.Arrow;
            TowerColor = Color.FromArgb(139, 90, 43);
            AccentColor = Color.SandyBrown;
        }

        protected override List<Enemy> GetEnemies() => _enemies;

        public override void Update(float deltaTime)
        {
            // Track aim angle toward closest enemy in range
            var target = FindTarget(_enemies);
            if (target != null)
            {
                float cx = X + Width / 2f;
                float cy = Y + Height / 2f;
                float dx = target.Center.X - cx;
                float dy = target.Center.Y - cy;
                _aimAngle = (float)Math.Atan2(dy, dx);
            }
            base.Update(deltaTime);
        }

        protected override void DrawTowerTop(Graphics g)
        {
            float cx = X + Width / 2f;
            float cy = Y + Height / 2f;

            var state = g.Save();
            g.TranslateTransform(cx, cy);
            g.RotateTransform(_aimAngle * 180f / (float)Math.PI);

            // Arrow shaft full width
            using var arrowPen = new Pen(Color.FromArgb(180, 140, 60), 2);
            g.DrawLine(arrowPen, -14f, 0f, 14f, 0f);

            // Bow: LEFT-opening arc centered exactly at origin
            // DrawArc rect is centered so arc midpoint = (0,0)
            using var bowPen = new Pen(Color.SaddleBrown, 2);
            // Left-opening: start=90, sweep=180 draws right half → use start=-90, sweep=-180 for left half
            g.DrawArc(bowPen, -12f, -10f, 14f, 20f, -90, 180);

            // Bowstring: vertical line at x=0 (right side of bow, left of arrowhead)
            using var strPen = new Pen(Color.FromArgb(230, Color.Ivory), 1);
            g.DrawLine(strPen, -5f, -10f, -5f, 10f);

            // Arrowhead: right tip at +14
            PointF[] head = {
                new PointF(14f,  0f),
                new PointF( 9f, -3f),
                new PointF( 9f,  3f)
            };
            using var headBrush = new SolidBrush(Color.Silver);
            g.FillPolygon(headBrush, head);

            // Feathers: spread outward at tail (-14), like a V opening to the LEFT
            using var fp = new Pen(Color.FromArgb(230, 230, 230, 230), 1);
            g.DrawLine(fp, -11f, 0f, -14f, -7f);  // upper feather out
            g.DrawLine(fp, -11f, 0f, -14f, 7f);  // lower feather out

            g.Restore(state);
        }
    }

    // ── Mage Tower ───────────────────────────────────────────────────────────
    /// <summary>Mage Tower — medium speed, high damage, wide range.</summary>
    public class MageTower : Tower
    {
        private List<Enemy> _enemies;

        public MageTower(float x, float y, List<Enemy> enemies) : base(x, y)
        {
            _enemies = enemies;
            TowerType = TowerType.Mage;
            Name = "Mage";
            Description = "Powerful spells, large range";
            Cost = 100;
            Range = 160f;
            Damage = 35;
            FireRate = 1f;
            ProjectileType = ProjectileType.MagicBolt;
            TowerColor = Color.FromArgb(70, 40, 120);
            AccentColor = Color.MediumPurple;
        }

        protected override List<Enemy> GetEnemies() => _enemies;

        protected override void DrawTowerTop(Graphics g)
        {
            float cx = X + Width / 2f;
            float cy = Y + Height / 2f;

            // Star shape (magic symbol)
            using var starPen = new Pen(Color.MediumPurple, 1);
            using var starBrush = new SolidBrush(Color.FromArgb(180, 100, 220));

            // Orb
            g.FillEllipse(starBrush, cx - 6, cy - 6, 12, 12);
            using var glowBrush = new SolidBrush(Color.FromArgb(80, 200, 150, 255));
            g.FillEllipse(glowBrush, cx - 9, cy - 9, 18, 18);

            // Cross lines (magic cross)
            using var linePen = new Pen(Color.FromArgb(200, 180, 255), 1);
            g.DrawLine(linePen, cx, cy - 12, cx, cy + 12);
            g.DrawLine(linePen, cx - 12, cy, cx + 12, cy);

            // Label
            using var font = new Font("Arial", 6, FontStyle.Bold);
            using var brush = new SolidBrush(Color.MediumPurple);
            g.DrawString("MAG", font, brush, X + 12, Y + 34);
        }
    }

    // ── Catapult Tower ───────────────────────────────────────────────────────
    /// <summary>Catapult Tower — very slow, massive damage.</summary>
    public class CatapultTower : Tower
    {
        private List<Enemy> _enemies;
        private float _aimAngle = 0f;

        public CatapultTower(float x, float y, List<Enemy> enemies) : base(x, y)
        {
            _enemies = enemies;
            TowerType = TowerType.Catapult;
            Name = "Catapult";
            Description = "Very slow, massive damage";
            Cost = 150;
            Range = 200f;
            Damage = 80;
            FireRate = 0.5f;
            ProjectileType = ProjectileType.Boulder;
            TowerColor = Color.FromArgb(80, 80, 80);
            AccentColor = Color.LightGray;
        }

        protected override List<Enemy> GetEnemies() => _enemies;

        public override void Update(float deltaTime)
        {
            var target = FindTarget(_enemies);
            if (target != null)
            {
                float cx = X + Width / 2f;
                float cy = Y + Height / 2f;
                float dx = target.Center.X - cx;
                float dy = target.Center.Y - cy;
                _aimAngle = (float)Math.Atan2(dy, dx) + (float)Math.PI;
            }
            base.Update(deltaTime);
        }

        protected override void DrawTowerTop(Graphics g)
        {
            float cx = X + Width / 2f;
            float cy = Y + Height / 2f;

            var state = g.Save();
            g.TranslateTransform(cx, cy);
            g.RotateTransform(_aimAngle * 180f / (float)Math.PI);

            // Base platform (top-down view)
            using var baseBrush = new SolidBrush(Color.FromArgb(110, 85, 55));
            g.FillEllipse(baseBrush, -13f, -13f, 26f, 26f);
            using var basePen = new Pen(Color.FromArgb(80, 60, 35), 1);
            g.DrawEllipse(basePen, -13f, -13f, 26f, 26f);

            // Wheels on sides (top-down: two circles)
            using var wheelBrush = new SolidBrush(Color.FromArgb(60, 45, 25));
            using var wheelPen = new Pen(Color.FromArgb(40, 30, 15), 1);
            g.FillEllipse(wheelBrush, -5f, -16f, 10f, 7f);
            g.DrawEllipse(wheelPen, -5f, -16f, 10f, 7f);
            g.FillEllipse(wheelBrush, -5f, 9f, 10f, 7f);
            g.DrawEllipse(wheelPen, -5f, 9f, 10f, 7f);

            // Arm (beam pointing forward = right in local space)
            using var armPen = new Pen(Color.SaddleBrown, 3);
            g.DrawLine(armPen, -10f, 0f, 14f, 0f);

            // Bucket at tip
            using var bucketBrush = new SolidBrush(Color.FromArgb(90, 70, 50));
            g.FillEllipse(bucketBrush, 11f, -4f, 8f, 8f);
            using var bucketPen = new Pen(Color.FromArgb(60, 45, 30), 1);
            g.DrawEllipse(bucketPen, 11f, -4f, 8f, 8f);

            // Boulder in bucket
            using var boulderBrush = new SolidBrush(Color.FromArgb(150, 140, 130));
            g.FillEllipse(boulderBrush, 13f, -2f, 5f, 5f);

            // Pivot pin at center
            using var pivotBrush = new SolidBrush(Color.DarkGray);
            g.FillEllipse(pivotBrush, -3f, -3f, 6f, 6f);

            g.Restore(state);
        }
    }
}