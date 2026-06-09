using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using TowerDefense.Controllers;
using TowerDefense.Models;
using TowerDefense.Utils;

namespace TowerDefense.Views
{
    /// <summary>
    /// Main game window. Contains the map canvas on the left
    /// and a control panel on the right.
    /// </summary>
    public class GameForm : Form
    {
        // ── Core ─────────────────────────────────────────────────────────────
        private GameController _controller;
        private System.Windows.Forms.Timer _gameTimer;
        private DateTime _lastTick;

        // ── Render ───────────────────────────────────────────────────────────
        private PictureBox _canvas;
        private Bitmap _buffer;

        // ── Side panel controls ──────────────────────────────────────────────
        private Panel _panel;
        private Button _btnArcher;
        private Button _btnMage;
        private Button _btnCatapult;
        private Button _btnNextWave;
        private Label _lblGold;
        private Label _lblHealth;
        private Label _lblWave;
        private Label _lblScore;
        private Label _lblMessage;
        private Label _lblTowerDesc;

        // ── Preview ──────────────────────────────────────────────────────────
        private Point _mouseCell = new Point(-1, -1);   // cell under cursor
        private bool _mouseOnMap = false;

        public GameForm()
        {
            _controller = new GameController();
            _controller.OnMessage += ShowMessage;
            _controller.OnGameOver += HandleGameOver;
            _controller.OnVictory += HandleVictory;

            InitializeForm();
            InitializeCanvas();
            InitializePanel();
            InitializeTimer();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Form / Canvas / Panel setup

        private void InitializeForm()
        {
            Text = "Tower Defense";
            ClientSize = new Size(Constants.WindowWidth, Constants.WindowHeight);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(30, 30, 30);
        }

        private void InitializeCanvas()
        {
            _buffer = new Bitmap(Constants.MapWidth, Constants.MapHeight);

            _canvas = new PictureBox
            {
                Location = new Point(0, 0),
                Size = new Size(Constants.MapWidth, Constants.MapHeight),
                Image = _buffer
            };

            _canvas.MouseClick += Canvas_MouseClick;
            _canvas.MouseMove += Canvas_MouseMove;
            _canvas.MouseLeave += (s, e) => { _mouseOnMap = false; };

            Controls.Add(_canvas);
        }

        private void InitializePanel()
        {
            _panel = new Panel
            {
                Location = new Point(Constants.MapWidth, 0),
                Size = new Size(Constants.PanelWidth, Constants.WindowHeight),
                BackColor = Color.FromArgb(28, 35, 28)
            };
            Controls.Add(_panel);

            int y = 10;

            // ── Title ────────────────────────────────────────────────────────
            AddLabel("TOWER DEFENSE", 10, ref y, 14, FontStyle.Bold,
                Color.FromArgb(180, 230, 160));
            y += 4;
            AddSeparator(ref y);

            // ── Stats ────────────────────────────────────────────────────────
            _lblGold = AddLabel("💰 Gold:   150", 10, ref y, 11, FontStyle.Regular,
                Color.FromArgb(240, 210, 80));
            _lblHealth = AddLabel("❤️ Castle: 20/20", 10, ref y, 11, FontStyle.Regular,
                Color.FromArgb(220, 80, 80));
            _lblWave = AddLabel("🌊 Wave:  0 / 10", 10, ref y, 11, FontStyle.Regular,
                Color.FromArgb(80, 160, 240));
            _lblScore = AddLabel("⭐ Score:  0", 10, ref y, 11, FontStyle.Regular,
                Color.FromArgb(200, 200, 100));
            y += 4;
            AddSeparator(ref y);

            // ── Tower selection ───────────────────────────────────────────────
            AddLabel("SELECT TOWER", 10, ref y, 10, FontStyle.Bold,
                Color.FromArgb(160, 180, 160));
            y += 2;

            _btnArcher = AddTowerButton("🏹 Archer   50g", ref y, TowerType.Archer);
            _btnMage = AddTowerButton("🧙 Mage    100g", ref y, TowerType.Mage);
            _btnCatapult = AddTowerButton("⚙ Catapult 150g", ref y, TowerType.Catapult);

            // ── Tower description ─────────────────────────────────────────────
            _lblTowerDesc = new Label
            {
                Text = "Fast attack, low damage",
                Location = new Point(8, y),
                Size = new Size(Constants.PanelWidth - 16, 40),
                ForeColor = Color.FromArgb(150, 170, 150),
                Font = new Font(Constants.FontName, 8, FontStyle.Italic),
                AutoSize = false
            };
            _panel.Controls.Add(_lblTowerDesc);
            y += 44;
            AddSeparator(ref y);

            // ── Next wave button ──────────────────────────────────────────────
            _btnNextWave = new Button
            {
                Text = "▶  Next Wave",
                Location = new Point(10, y),
                Size = new Size(Constants.PanelWidth - 20, 44),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(50, 110, 50),
                ForeColor = Color.White,
                Font = new Font(Constants.FontName, 11, FontStyle.Bold),
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 0 }
            };
            _btnNextWave.Click += (s, e) => _controller.StartNextWave();
            _panel.Controls.Add(_btnNextWave);
            y += 52;
            AddSeparator(ref y);

            // ── Message label ─────────────────────────────────────────────────
            _lblMessage = new Label
            {
                Text = "Place towers, then start the wave!",
                Location = new Point(8, y),
                Size = new Size(Constants.PanelWidth - 16, 80),
                ForeColor = Color.FromArgb(180, 200, 180),
                Font = new Font(Constants.FontName, 8.5f),
                AutoSize = false
            };
            _panel.Controls.Add(_lblMessage);

            // ── How to play (bottom) ──────────────────────────────────────────
            var lblHelp = new Label
            {
                Text = "Left-click map to place tower\nHover tower to see range\nEarn gold by killing enemies",
                Location = new Point(8, Constants.WindowHeight - 90),
                Size = new Size(Constants.PanelWidth - 16, 80),
                ForeColor = Color.FromArgb(100, 130, 100),
                Font = new Font(Constants.FontName, 8),
                AutoSize = false
            };
            _panel.Controls.Add(lblHelp);

            // Select Archer by default
            SelectTower(TowerType.Archer);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Panel helpers

        private Label AddLabel(string text, int x, ref int y,
            float fontSize, FontStyle style, Color color)
        {
            var lbl = new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(Constants.PanelWidth - x * 2, 24),
                ForeColor = color,
                Font = new Font(Constants.FontName, fontSize, style),
                AutoSize = false
            };
            _panel.Controls.Add(lbl);
            y += 26;
            return lbl;
        }

        private void AddSeparator(ref int y)
        {
            var sep = new Panel
            {
                Location = new Point(8, y),
                Size = new Size(Constants.PanelWidth - 16, 1),
                BackColor = Color.FromArgb(60, 80, 60)
            };
            _panel.Controls.Add(sep);
            y += 8;
        }

        private Button AddTowerButton(string text, ref int y, TowerType type)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(10, y),
                Size = new Size(Constants.PanelWidth - 20, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(45, 65, 45),
                ForeColor = Color.White,
                Font = new Font(Constants.FontName, 10),
                Cursor = Cursors.Hand,
                Tag = type,
                FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(70, 90, 70) }
            };
            btn.Click += (s, e) => SelectTower((TowerType)((Button)s).Tag);
            _panel.Controls.Add(btn);
            y += 40;
            return btn;
        }

        private void SelectTower(TowerType type)
        {
            _controller.SelectedTowerType = type;

            // Reset all button colours
            foreach (var btn in new[] { _btnArcher, _btnMage, _btnCatapult })
            {
                btn.BackColor = Color.FromArgb(45, 65, 45);
                btn.FlatAppearance.BorderColor = Color.FromArgb(70, 90, 70);
            }

            // Highlight selected
            Button selected = type switch
            {
                TowerType.Archer => _btnArcher,
                TowerType.Mage => _btnMage,
                TowerType.Catapult => _btnCatapult,
                _ => _btnArcher
            };
            selected.BackColor = Color.FromArgb(60, 110, 60);
            selected.FlatAppearance.BorderColor = Color.FromArgb(120, 200, 120);

            // Update description
            _lblTowerDesc.Text = type switch
            {
                TowerType.Archer => "Fast attack • Low damage\nRange: 120  Rate: 2/s  DMG: 15",
                TowerType.Mage => "Slow attack • High damage\nRange: 160  Rate: 1/s  DMG: 35",
                TowerType.Catapult => "Very slow • Massive damage\nRange: 200  Rate: 0.5/s  DMG: 80",
                _ => ""
            };
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Game loop

        private void InitializeTimer()
        {
            _lastTick = DateTime.Now;
            _gameTimer = new System.Windows.Forms.Timer
            {
                Interval = Constants.TimerIntervalMs
            };
            _gameTimer.Tick += GameLoop;
            _gameTimer.Start();
        }

        private void GameLoop(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;
            float deltaTime = (float)(now - _lastTick).TotalSeconds;
            _lastTick = now;

            // Cap delta to avoid spiral of death on lag spikes
            if (deltaTime > 0.05f) deltaTime = 0.05f;

            _controller.Update(deltaTime);
            UpdatePanel();
            RenderFrame();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Rendering

        private void RenderFrame()
        {
            using var g = Graphics.FromImage(_buffer);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.FromArgb(30, 30, 30));

            _controller.Draw(g);
            DrawPlacementPreview(g);

            _canvas.Refresh();
        }

        private void DrawPlacementPreview(Graphics g)
        {
            if (!_mouseOnMap || _mouseCell.X < 0) return;

            int px = _mouseCell.X * GameMap.CellSize;
            int py = _mouseCell.Y * GameMap.CellSize;
            bool canPlace = _controller.Map.CanPlaceTower(_mouseCell.X, _mouseCell.Y);
            int cost = _controller.GetTowerCost(_controller.SelectedTowerType);
            bool canAfford = _controller.State.Gold >= cost;

            Color overlayColor = (canPlace && canAfford)
                ? Color.FromArgb(60, 100, 255, 100)
                : Color.FromArgb(60, 255, 80, 80);

            Color borderColor = (canPlace && canAfford)
                ? Color.FromArgb(180, 100, 255, 100)
                : Color.FromArgb(180, 255, 80, 80);

            using var brush = new SolidBrush(overlayColor);
            g.FillRectangle(brush, px, py, GameMap.CellSize, GameMap.CellSize);

            using var pen = new Pen(borderColor, 2);
            g.DrawRectangle(pen, px, py, GameMap.CellSize, GameMap.CellSize);

            // Draw range preview
            if (canPlace && canAfford)
            {
                float range = _controller.SelectedTowerType switch
                {
                    TowerType.Archer => 120f,
                    TowerType.Mage => 160f,
                    TowerType.Catapult => 200f,
                    _ => 120f
                };
                float cx = px + GameMap.CellSize / 2f;
                float cy = py + GameMap.CellSize / 2f;

                using var rangeBrush = new SolidBrush(Color.FromArgb(15, 100, 200, 100));
                g.FillEllipse(rangeBrush, cx - range, cy - range, range * 2, range * 2);

                using var rangePen = new Pen(Color.FromArgb(50, 100, 200, 100), 1);
                g.DrawEllipse(rangePen, cx - range, cy - range, range * 2, range * 2);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Panel update

        private void UpdatePanel()
        {
            var s = _controller.State;

            _lblGold.Text = $"💰 Gold:   {s.Gold}";
            _lblHealth.Text = $"❤️ Castle: {s.CastleHealth}/{s.MaxCastleHealth}";
            _lblWave.Text = $"🌊 Wave:   {s.CurrentWave} / {s.TotalWaves}";
            _lblScore.Text = $"⭐ Score:  {s.Score}";

            _lblHealth.ForeColor = s.CastleHealth > s.MaxCastleHealth / 2
                ? Color.FromArgb(220, 80, 80)
                : Color.FromArgb(255, 60, 60);

            _btnNextWave.Enabled = _controller.CanStartNextWave;
            _btnNextWave.BackColor = _controller.CanStartNextWave
                ? Color.FromArgb(50, 130, 50)
                : Color.FromArgb(50, 60, 50);
        }

        private void ShowMessage(string msg)
        {
            if (_lblMessage.InvokeRequired)
                _lblMessage.Invoke(new Action(() => _lblMessage.Text = msg));
            else
                _lblMessage.Text = msg;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Input

        private void Canvas_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                _controller.TryPlaceTower(e.X, e.Y);
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            _mouseOnMap = true;
            _mouseCell = new Point(
                e.X / GameMap.CellSize,
                e.Y / GameMap.CellSize);
            _controller.UpdateHover(e.X, e.Y);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Game over / Victory

        private void HandleGameOver()
        {
            _gameTimer.Stop();
            Invoke(new Action(() =>
            {
                var form = new GameOverForm(false,
                    _controller.State.Score,
                    _controller.State.CurrentWave);
                form.Owner = this;
                form.ShowDialog();
            }));
        }

        private void HandleVictory()
        {
            _gameTimer.Stop();
            Invoke(new Action(() =>
            {
                var form = new GameOverForm(true,
                    _controller.State.Score,
                    _controller.State.CurrentWave);
                form.Owner = this;
                form.ShowDialog();
            }));
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _gameTimer?.Stop();
            _buffer?.Dispose();
            base.OnFormClosed(e);
        }
    }
}

