using System;
using System.Collections.Generic;
using System.Drawing;

namespace TowerDefense.Models
{
    public enum EnemyType { Orc, Troll, Dragon }

    public abstract class Enemy : Entity
    {
        public int MaxHealth { get; protected set; }
        public int CurrentHealth { get; protected set; }
        public float Speed { get; protected set; }
        public int Reward { get; protected set; }
        public int Damage { get; protected set; }
        public EnemyType EnemyType { get; protected set; }

        protected List<Point> Path { get; private set; }
        protected int PathIndex { get; private set; } = 0;

        public bool ReachedEnd { get; private set; } = false;

        protected Color PrimaryColor { get; set; }
        protected Color SecondaryColor { get; set; }

        protected Enemy(float x, float y, int w, int h, List<Point> path)
            : base(x, y, w, h)
        {
            Path = path;
        }

        protected void InitHealth(int hp)
        {
            MaxHealth = hp;
            CurrentHealth = hp;
        }

        public void TakeDamage(int amount)
        {
            CurrentHealth -= amount;
            if (CurrentHealth <= 0)
            {
                CurrentHealth = 0;
                IsAlive = false;
            }
        }

        public override void Update(float deltaTime)
        {
            if (!IsAlive || ReachedEnd) return;
            if (PathIndex >= Path.Count)
            {
                ReachedEnd = true;
                IsAlive = false;
                return;
            }

            Point target = Path[PathIndex];
            float tx = target.X * GameMap.CellSize + GameMap.CellSize / 2f - Width / 2f;
            float ty = target.Y * GameMap.CellSize + GameMap.CellSize / 2f - Height / 2f;

            float dx = tx - X;
            float dy = ty - Y;
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);

            if (dist < Speed * deltaTime)
            {
                X = tx; Y = ty;
                PathIndex++;
            }
            else
            {
                X += dx / dist * Speed * deltaTime;
                Y += dy / dist * Speed * deltaTime;
            }
        }

        public override void Draw(Graphics g)
        {
            if (!IsAlive) return;

            // Body
            using (var brush = new SolidBrush(PrimaryColor))
                g.FillEllipse(brush, X, Y, Width, Height);

            using (var pen = new Pen(SecondaryColor, 2))
                g.DrawEllipse(pen, X, Y, Width, Height);

            DrawSymbol(g);
            DrawHealthBar(g);
        }

        protected abstract void DrawSymbol(Graphics g);

        private void DrawHealthBar(Graphics g)
        {
            float barW = Width;
            float barH = 5;
            float barX = X;
            float barY = Y - 8;

            g.FillRectangle(Brushes.DarkRed, barX, barY, barW, barH);
            float hpRatio = (float)CurrentHealth / MaxHealth;
            Color hpColor = hpRatio > 0.5f ? Color.LimeGreen
                          : hpRatio > 0.25f ? Color.Yellow
                          : Color.Red;
            using (var hpBrush = new SolidBrush(hpColor))
                g.FillRectangle(hpBrush, barX, barY, barW * hpRatio, barH);
            g.DrawRectangle(Pens.Black, barX, barY, barW, barH);
        }

        public float DistanceTraveled =>
            PathIndex * 1000f + (PathIndex < Path.Count
                ? 1000f - DistanceTo(Path[PathIndex])
                : 0f);

        private float DistanceTo(Point p)
        {
            float tx = p.X * GameMap.CellSize + GameMap.CellSize / 2f;
            float ty = p.Y * GameMap.CellSize + GameMap.CellSize / 2f;
            float dx = tx - (X + Width / 2f);
            float dy = ty - (Y + Height / 2f);
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        public PointF Center => new PointF(X + Width / 2f, Y + Height / 2f);
    }

    // ── Orc ─────────────────────────────────────────────────────────────────
    /// <summary>Orc — basic enemy: medium speed, medium HP.</summary>
    public class Orc : Enemy
    {
        public Orc(float x, float y, List<Point> path, float hpMult = 1f, float speedMult = 1f)
            : base(x, y, 28, 28, path)
        {
            EnemyType = EnemyType.Orc;
            Speed = 80f * speedMult;
            Reward = 10;
            Damage = 1;
            PrimaryColor = Color.FromArgb(80, 160, 60);
            SecondaryColor = Color.DarkGreen;
            InitHealth((int)(60 * hpMult));
        }

        protected override void DrawSymbol(Graphics g)
        {
            float cx = X + Width / 2f;
            float cy = Y + Height / 2f;

            // Eyes
            using var eyeBrush = new SolidBrush(Color.Red);
            g.FillEllipse(eyeBrush, cx - 7, cy - 5, 5, 5);
            g.FillEllipse(eyeBrush, cx + 2, cy - 5, 5, 5);

            // Tusks
            using var tuskBrush = new SolidBrush(Color.Ivory);
            g.FillRectangle(tuskBrush, cx - 6, cy + 2, 3, 5);
            g.FillRectangle(tuskBrush, cx + 3, cy + 2, 3, 5);
        }
    }

    // ── Troll ────────────────────────────────────────────────────────────────
    /// <summary>Troll — slow but very tanky. Deals 2 damage to castle.</summary>
    public class Troll : Enemy
    {
        public Troll(float x, float y, List<Point> path, float hpMult = 1f, float speedMult = 1f)
            : base(x, y, 34, 34, path)
        {
            EnemyType = EnemyType.Troll;
            Speed = 45f * speedMult;
            Reward = 20;
            Damage = 2;
            PrimaryColor = Color.FromArgb(100, 80, 50);
            SecondaryColor = Color.SaddleBrown;
            InitHealth((int)(200 * hpMult));
        }

        protected override void DrawSymbol(Graphics g)
        {
            float cx = X + Width / 2f;
            float cy = Y + Height / 2f;

            // Eyes
            using var eyeBrush = new SolidBrush(Color.OrangeRed);
            g.FillEllipse(eyeBrush, cx - 9, cy - 6, 7, 7);
            g.FillEllipse(eyeBrush, cx + 2, cy - 6, 7, 7);

            // Angry brow
            using var browPen = new Pen(Color.Black, 2);
            g.DrawLine(browPen, cx - 9, cy - 8, cx - 2, cy - 6);
            g.DrawLine(browPen, cx + 2, cy - 6, cx + 9, cy - 8);
        }
    }

    // ── Dragon ───────────────────────────────────────────────────────────────
    /// <summary>Dragon — fast, high castle damage, high reward.</summary>
    public class Dragon : Enemy
    {
        public Dragon(float x, float y, List<Point> path, float hpMult = 1f, float speedMult = 1f)
            : base(x, y, 38, 38, path)
        {
            EnemyType = EnemyType.Dragon;
            Speed = 120f * speedMult;
            Reward = 50;
            Damage = 5;
            PrimaryColor = Color.FromArgb(200, 50, 30);
            SecondaryColor = Color.DarkRed;
            InitHealth((int)(120 * hpMult));
        }

        protected override void DrawSymbol(Graphics g)
        {
            float cx = X + Width / 2f;
            float cy = Y + Height / 2f;

            // Wings (triangles)
            using var wingBrush = new SolidBrush(Color.FromArgb(160, 30, 10));
            Point[] leftWing = { new Point((int)cx - 4, (int)cy), new Point((int)X, (int)cy - 10), new Point((int)X, (int)cy + 4) };
            Point[] rightWing = { new Point((int)cx + 4, (int)cy), new Point((int)(X + Width), (int)cy - 10), new Point((int)(X + Width), (int)cy + 4) };
            g.FillPolygon(wingBrush, leftWing);
            g.FillPolygon(wingBrush, rightWing);

            // Eyes
            using var eyeBrush = new SolidBrush(Color.Yellow);
            g.FillEllipse(eyeBrush, cx - 9, cy - 7, 7, 7);
            g.FillEllipse(eyeBrush, cx + 2, cy - 7, 7, 7);
        }
    }
}
