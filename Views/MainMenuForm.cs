using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace TowerDefense.Views
{
    public class MainMenuForm : Form
    {
        private Button _btnPlay;
        private Button _btnQuit;
        private System.Windows.Forms.Timer _animTimer;
        private float _animOffset = 0f;

        public MainMenuForm()
        {
            InitializeForm();
            InitializeControls();
            InitializeAnimation();
        }

        private void InitializeForm()
        {
            Text = "Tower Defense";
            ClientSize = new Size(Utils.Constants.WindowWidth, Utils.Constants.WindowHeight);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(20, 30, 20);
            DoubleBuffered = true;
        }

        private void InitializeControls()
        {
            _btnPlay = CreateButton("[ PLAY ]", new Point(ClientSize.Width / 2 - 120, 340));
            _btnPlay.BackColor = Color.FromArgb(60, 140, 60);
            _btnPlay.Click += (s, e) =>
            {
                var modeForm = new ModeSelectForm();
                if (modeForm.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var gameForm = new GameForm(modeForm.SelectedConfig);
                    gameForm.FormClosed += (gs, ge) => Show();
                    Hide();
                    gameForm.Show();
                }
            };

            _btnQuit = CreateButton("[ QUIT ]", new Point(ClientSize.Width / 2 - 120, 420));
            _btnQuit.BackColor = Color.FromArgb(140, 50, 50);
            _btnQuit.Click += (s, e) => Application.Exit();

            Controls.Add(_btnPlay);
            Controls.Add(_btnQuit);
        }

        private Button CreateButton(string text, Point location)
        {
            return new Button
            {
                Text = text,
                Location = location,
                Size = new Size(240, 55),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                Font = new Font(Utils.Constants.FontName, 14, FontStyle.Bold),
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 0 }
            };
        }

        private void InitializeAnimation()
        {
            _animTimer = new System.Windows.Forms.Timer { Interval = 16 };
            _animTimer.Tick += (s, e) => { _animOffset += 0.02f; Invalidate(); };
            _animTimer.Start();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            DrawBackground(g);
            DrawTitle(g);
            DrawSubtitle(g);
            DrawTowerPreview(g);
            DrawHint(g);
        }

        private void DrawBackground(Graphics g)
        {
            using var darkBrush = new SolidBrush(Color.FromArgb(25, 40, 25));
            using var lightBrush = new SolidBrush(Color.FromArgb(35, 55, 35));
            int cell = 48;
            for (int x = 0; x < ClientSize.Width; x += cell)
                for (int y = 0; y < ClientSize.Height; y += cell)
                {
                    bool checker = ((x / cell) + (y / cell)) % 2 == 0;
                    g.FillRectangle(checker ? darkBrush : lightBrush, x, y, cell, cell);
                }
            using var vignette = new LinearGradientBrush(
                new Rectangle(0, 0, ClientSize.Width, ClientSize.Height),
                Color.FromArgb(160, 0, 0, 0), Color.FromArgb(60, 0, 0, 0),
                LinearGradientMode.Vertical);
            g.FillRectangle(vignette, 0, 0, ClientSize.Width, ClientSize.Height);
        }

        private void DrawTitle(Graphics g)
        {
            string title = "TOWER DEFENSE";
            using var font = new Font(Utils.Constants.FontName, 52, FontStyle.Bold);
            using var shadow = new SolidBrush(Color.FromArgb(120, 0, 0, 0));
            g.DrawString(title, font, shadow, ClientSize.Width / 2f - 248 + 4, 124);
            using var mainBrush = new SolidBrush(Color.FromArgb(180, 230, 160));
            g.DrawString(title, font, mainBrush, ClientSize.Width / 2f - 248, 120);
        }

        private void DrawSubtitle(Graphics g)
        {
            string sub = "Defend your castle against 10 waves of enemies!";
            using var font = new Font(Utils.Constants.FontName, 13, FontStyle.Italic);
            using var brush = new SolidBrush(Color.FromArgb(180, 200, 220, 200));
            var size = g.MeasureString(sub, font);
            g.DrawString(sub, font, brush, (ClientSize.Width - size.Width) / 2f, 240);
        }

        private void DrawTowerPreview(Graphics g)
        {
            int cx = ClientSize.Width / 2;
            int cy = 290;
            using var wallBrush = new SolidBrush(Color.FromArgb(140, 120, 90));
            g.FillRectangle(wallBrush, cx - 18, cy, 36, 30);
            g.FillRectangle(wallBrush, cx - 18, cy - 10, 10, 12);
            g.FillRectangle(wallBrush, cx - 4, cy - 10, 10, 12);
            g.FillRectangle(wallBrush, cx + 10, cy - 10, 10, 12);
            using var gateBrush = new SolidBrush(Color.FromArgb(60, 45, 30));
            g.FillRectangle(gateBrush, cx - 6, cy + 14, 12, 16);
            g.FillEllipse(gateBrush, cx - 6, cy + 10, 12, 8);
            using var pen = new Pen(Color.FromArgb(180, 230, 160), 1);
            g.DrawRectangle(pen, cx - 18, cy, 36, 30);
        }

        private void DrawHint(Graphics g)
        {
            string[] lines =
            {
                "[ ARC ]  Archer    50g  |  fast attack,  low damage",
                "[ MAG ]  Mage     100g  |  slow attack,  high damage",
                "[ CAT ]  Catapult 150g  |  very slow,  massive damage",
            };
            using var font = new Font(Utils.Constants.FontName, 10);
            using var brush = new SolidBrush(Color.FromArgb(140, 200, 200, 200));
            int y = ClientSize.Height - 100;
            foreach (var line in lines)
            {
                var size = g.MeasureString(line, font);
                g.DrawString(line, font, brush, (ClientSize.Width - size.Width) / 2f, y);
                y += 22;
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _animTimer?.Stop();
            base.OnFormClosed(e);
        }
    }
}
