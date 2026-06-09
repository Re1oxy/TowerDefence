using System;
using System.Collections.Generic;
using System.Drawing;

namespace TowerDefense.Models
{
    /// <summary>
    /// Boss enemy. Appears on wave 10 in Normal mode and every
    /// 10 waves in Endless mode. Has a unique top-down sprite,
    /// very high HP and deals massive castle damage.
    /// </summary>
    public class Boss : Enemy
    {
        private float _pulseTimer = 0f;
        private float _rotAngle = 0f;

        public Boss(float x, float y, List<Point> path, float hpMult = 1f)
            : base(x, y, 44, 44, path)
        {
            EnemyType = EnemyType.Dragon;
            Speed = 35f;
            Reward = 200;
            Damage = 8;
            PrimaryColor = Color.FromArgb(120, 0, 160);
            SecondaryColor = Color.FromArgb(60, 0, 100);
            InitHealth((int)(800 * hpMult));
        }

        public override void Update(float deltaTime)
        {
            _pulseTimer += deltaTime;
            _rotAngle += deltaTime * 90f;
            if (_rotAngle >= 360f) _rotAngle -= 360f;
            base.Update(deltaTime);
        }

        public override void Draw(Graphics g)
        {
            if (!IsAlive) return;

            float cx = X + Width / 2f;
            float cy = Y + Height / 2f;

            // -- Outer glow ------------------------------------------------------
            float pulse = (float)(Math.Sin(_pulseTimer * 3f) * 0.5f + 0.5f);
            int alpha = (int)(40 + pulse * 40);
            using var glowBrush = new SolidBrush(Color.FromArgb(alpha, 180, 0, 255));
            g.FillEllipse(glowBrush, cx - 28, cy - 28, 56, 56);

            // -- Body ------------------------------------------------------------
            using var bodyBrush = new SolidBrush(PrimaryColor);
            g.FillEllipse(bodyBrush, X, Y, Width, Height);
            using var bodyPen = new Pen(SecondaryColor, 2);
            g.DrawEllipse(bodyPen, X, Y, Width, Height);

            // -- Rotating crown (4 orbs) -----------------------------------------
            var state = g.Save();
            g.TranslateTransform(cx, cy);
            g.RotateTransform(_rotAngle);

            using var orbBrush = new SolidBrush(Color.FromArgb(220, 180, 255));
            float[] angles = { 0f, 90f, 180f, 270f };
            foreach (var a in angles)
            {
                double rad = a * Math.PI / 180.0;
                float sx = (float)Math.Cos(rad) * 18f;
                float sy = (float)Math.Sin(rad) * 18f;
                g.FillEllipse(orbBrush, sx - 4, sy - 4, 8, 8);
            }

            // -- Inner eye -------------------------------------------------------
            using var eyeBrush = new SolidBrush(Color.FromArgb(255, 220, 0));
            using var pupilBrush = new SolidBrush(Color.Black);
            g.FillEllipse(eyeBrush, -6f, -6f, 12f, 12f);
            g.FillEllipse(pupilBrush, -3f, -3f, 6f, 6f);

            g.Restore(state);

            // -- Health bar + label ----------------------------------------------
            DrawBossHealthBar(g);
            DrawBossLabel(g);
        }

        private void DrawBossHealthBar(Graphics g)
        {
            float barW = Width + 10;
            float barH = 7;
            float barX = X - 5;
            float barY = Y - 12;
            float hpRatio = (float)CurrentHealth / MaxHealth;

            g.FillRectangle(Brushes.DarkRed, barX, barY, barW, barH);

            Color hpColor = hpRatio > 0.5f ? Color.MediumPurple
                          : hpRatio > 0.25f ? Color.Orange
                          : Color.Red;

            using var hpBrush = new SolidBrush(hpColor);
            g.FillRectangle(hpBrush, barX, barY, barW * hpRatio, barH);
            g.DrawRectangle(Pens.Black, barX, barY, barW, barH);
        }

        private void DrawBossLabel(Graphics g)
        {
            using var font = new Font("Arial", 7, FontStyle.Bold);
            using var brush = new SolidBrush(Color.FromArgb(220, 180, 255));
            g.DrawString("BOSS", font, brush, X + 10, Y - 24);
        }

        protected override void DrawSymbol(Graphics g) { /* handled in Draw() */ }
    }
}
