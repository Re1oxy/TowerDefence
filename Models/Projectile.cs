using System;
using System.Drawing;

namespace TowerDefense.Models
{
    public enum ProjectileType { Arrow, MagicBolt, Boulder }

    /// <summary>
    /// A projectile flying from a tower to an enemy.
    /// </summary>
    public class Projectile : Entity
    {
        // ── Properties ────────────────────────────────────────────────────────
        public int Damage { get; private set; }
        public Enemy Target { get; private set; }
        public ProjectileType ProjectileType { get; private set; }
        private float Speed { get; set; }
        private Color Color { get; set; }
        private int Radius { get; set; }

        public Projectile(float x, float y, Enemy target, int damage, ProjectileType type)
            : base(x, y, 8, 8)
        {
            Target = target;
            Damage = damage;
            ProjectileType = type;

            // ── Per-type stats ────────────────────────────────────────────────
            switch (type)
            {
                case ProjectileType.Arrow:
                    Speed = 300f; Color = Color.Yellow; Radius = 4; break;
                case ProjectileType.MagicBolt:
                    Speed = 250f; Color = Color.Cyan; Radius = 6; break;
                case ProjectileType.Boulder:
                    Speed = 180f; Color = Color.Gray; Radius = 7; break;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Update

        public override void Update(float deltaTime)
        {
            if (!IsAlive) return;

            // ── Target lost or dead ───────────────────────────────────────────
            if (Target == null || !Target.IsAlive) { IsAlive = false; return; }

            PointF tc = Target.Center;
            float dx = tc.X - X;
            float dy = tc.Y - Y;
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);

            // ── Hit target ────────────────────────────────────────────────────
            if (dist < Speed * deltaTime)
            {
                Target.TakeDamage(Damage);
                IsAlive = false;
            }
            else
            {
                X += dx / dist * Speed * deltaTime;
                Y += dy / dist * Speed * deltaTime;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Draw

        public override void Draw(Graphics g)
        {
            if (!IsAlive) return;

            using var brush = new SolidBrush(Color);
            g.FillEllipse(brush, X - Radius, Y - Radius, Radius * 2, Radius * 2);

            // ── Glow effect for magic bolt ────────────────────────────────────
            if (ProjectileType == ProjectileType.MagicBolt)
            {
                using var glow = new SolidBrush(Color.FromArgb(60, Color));
                g.FillEllipse(glow, X - Radius * 2, Y - Radius * 2, Radius * 4, Radius * 4);
            }
        }
    }
}