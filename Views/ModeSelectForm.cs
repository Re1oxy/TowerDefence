using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using TowerDefense.Models;
using TowerDefense.Utils;

namespace TowerDefense.Views
{
    /// <summary>
    /// Screen for selecting game mode (Normal / Endless) and
    /// difficulty (Easy / Hard) before starting a session.
    /// </summary>
    public class ModeSelectForm : Form
    {
        public GameConfig SelectedConfig { get; private set; }

        private GameMode _mode = GameMode.Normal;
        private Difficulty _difficulty = Difficulty.Easy;

        private Button _btnNormal, _btnEndless;
        private Button _btnEasy, _btnHard;
        private Button _btnStart, _btnBack;
        private Label _lblDesc;

        public ModeSelectForm()
        {
            InitializeForm();
            InitializeControls();
            RefreshSelection();
        }

        private void InitializeForm()
        {
            Text = "Select Mode";
            ClientSize = new Size(480, 420);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(22, 32, 22);
            DoubleBuffered = true;
        }

        private void InitializeControls()
        {
            // -- Title -----------------------------------------------------------
            var title = new Label
            {
                Text = "SELECT MODE & DIFFICULTY",
                Location = new Point(0, 28),
                Size = new Size(480, 30),
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(180, 230, 160),
                Font = new Font(Constants.FontName, 14, FontStyle.Bold),
                BackColor = Color.Transparent
            };
            Controls.Add(title);

            // -- Mode buttons ----------------------------------------------------
            var modeLabel = MakeLabel("GAME MODE", 30, 80);
            Controls.Add(modeLabel);

            _btnNormal = MakeToggleBtn("Normal\n10 Waves + Boss", 30, 110);
            _btnEndless = MakeToggleBtn("Endless\nInfinite + Bosses every 10", 250, 110);
            _btnNormal.Click += (s, e) => { _mode = GameMode.Normal; RefreshSelection(); };
            _btnEndless.Click += (s, e) => { _mode = GameMode.Endless; RefreshSelection(); };
            Controls.Add(_btnNormal);
            Controls.Add(_btnEndless);

            // -- Difficulty buttons ----------------------------------------------
            var diffLabel = MakeLabel("DIFFICULTY", 30, 190);
            Controls.Add(diffLabel);

            _btnEasy = MakeToggleBtn("Easy\n200g start, 30 HP", 30, 220);
            _btnHard = MakeToggleBtn("Hard\n120g start, 15 HP\n+50% enemy stats", 250, 220);
            _btnEasy.Click += (s, e) => { _difficulty = Difficulty.Easy; RefreshSelection(); };
            _btnHard.Click += (s, e) => { _difficulty = Difficulty.Hard; RefreshSelection(); };
            Controls.Add(_btnEasy);
            Controls.Add(_btnHard);

            // -- Description label -----------------------------------------------
            _lblDesc = new Label
            {
                Location = new Point(30, 310),
                Size = new Size(420, 50),
                ForeColor = Color.FromArgb(160, 190, 160),
                Font = new Font(Constants.FontName, 9),
                BackColor = Color.Transparent,
                AutoSize = false
            };
            Controls.Add(_lblDesc);

            // -- Start button ----------------------------------------------------
            _btnStart = new Button
            {
                Text = "START GAME",
                Location = new Point(120, 365),
                Size = new Size(240, 42),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(50, 120, 50),
                ForeColor = Color.White,
                Font = new Font(Constants.FontName, 12, FontStyle.Bold),
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 0 }
            };
            _btnStart.Click += BtnStart_Click;
            Controls.Add(_btnStart);
        }

        private Label MakeLabel(string text, int x, int y)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(420, 22),
                ForeColor = Color.FromArgb(140, 160, 140),
                Font = new Font(Constants.FontName, 9, FontStyle.Bold),
                BackColor = Color.Transparent
            };
        }

        private Button MakeToggleBtn(string text, int x, int y)
        {
            return new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(200, 65),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                Font = new Font(Constants.FontName, 9),
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 2 }
            };
        }

        private void RefreshSelection()
        {
            Color activeColor = Color.FromArgb(50, 100, 50);
            Color inactiveColor = Color.FromArgb(35, 50, 35);
            Color activeBorder = Color.FromArgb(120, 200, 120);
            Color inactiveBorder = Color.FromArgb(60, 80, 60);

            _btnNormal.BackColor = _mode == GameMode.Normal ? activeColor : inactiveColor;
            _btnEndless.BackColor = _mode == GameMode.Endless ? activeColor : inactiveColor;
            _btnNormal.FlatAppearance.BorderColor = _mode == GameMode.Normal ? activeBorder : inactiveBorder;
            _btnEndless.FlatAppearance.BorderColor = _mode == GameMode.Endless ? activeBorder : inactiveBorder;

            _btnEasy.BackColor = _difficulty == Difficulty.Easy ? Color.FromArgb(40, 90, 40) : inactiveColor;
            _btnHard.BackColor = _difficulty == Difficulty.Hard ? Color.FromArgb(100, 40, 40) : inactiveColor;
            _btnEasy.FlatAppearance.BorderColor = _difficulty == Difficulty.Easy ? activeBorder : inactiveBorder;
            _btnHard.FlatAppearance.BorderColor = _difficulty == Difficulty.Hard ? Color.FromArgb(200, 80, 80) : inactiveBorder;

            // -- Upgrade note in description ------------------------------------
            var cfg = new GameConfig { GameMode = _mode, Difficulty = _difficulty };
            string upgNote = cfg.MaxUpgradeLevel == 1 ? "No tower upgrades"
                           : cfg.MaxUpgradeLevel == 2 ? "Tower upgrades up to level 2"
                           : "Tower upgrades up to level 3";

            _lblDesc.Text = $"{upgNote}  |  {(_mode == GameMode.Endless ? "Bosses every 10 waves, infinite scaling" : "10 waves, final boss on wave 10")}";

            Invalidate();
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            SelectedConfig = new GameConfig
            {
                GameMode = _mode,
                Difficulty = _difficulty
            };
            DialogResult = DialogResult.OK;
            Close();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            using var bg = new LinearGradientBrush(ClientRectangle,
                Color.FromArgb(28, 40, 28), Color.FromArgb(16, 22, 16),
                LinearGradientMode.Vertical);
            g.FillRectangle(bg, ClientRectangle);
        }
    }
}
