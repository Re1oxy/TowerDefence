using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using TowerDefense.Controllers;
using TowerDefense.Models;
using TowerDefense.Utils;

namespace TowerDefense.Views
{
    /// <summary>
    /// Main game window. Renders the map canvas and side panel.
    /// Handles all user input including tower popup (RMB).
    /// </summary>
    public class GameForm : Form
    {
        // -- Core ----------------------------------------------------------------
        private GameController _ctrl;
        private GameConfig _config;
        private System.Windows.Forms.Timer _gameTimer;
        private DateTime _lastTick;

        // -- Render --------------------------------------------------------------
        private PictureBox _canvas;
        private Bitmap _buffer;

        // -- Side panel ----------------------------------------------------------
        private Panel _panel;
        private Button _btnArcher, _btnMage, _btnCatapult;
        private Button _btnNextWave;
        private Label _lblGold, _lblHealth, _lblWave, _lblScore, _lblMessage;
        private Label _lblTowerDesc, _lblCountdown, _lblMode;
        private Panel _previewPanel;

        // -- Tower popup ---------------------------------------------------------
        private Panel _popup;
        private Tower _popupTower;

        // -- Placement preview ---------------------------------------------------
        private Point _mouseCell = new Point(-1, -1);
        private bool _mouseOnMap = false;

        public GameForm(GameConfig config)
        {
            _config = config;
            _ctrl = new GameController(config);
            _ctrl.OnMessage += ShowMessage;
            _ctrl.OnGameOver += HandleGameOver;
            _ctrl.OnVictory += HandleVictory;

            InitializeForm();
            InitializeCanvas();
            InitializePanel();
            InitializePopup();
            InitializeTimer();
        }

        // ── Form / Canvas setup ─────────────────────────────────────────────────
        private void InitializeForm()
        {
            Text = $"Tower Defense — {_config.GameMode} / {_config.Difficulty}";
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

        // ── Side panel ──────────────────────────────────────────────────────────
        private void InitializePanel()
        {
            _panel = new Panel
            {
                Location = new Point(Constants.MapWidth, 0),
                Size = new Size(Constants.PanelWidth, Constants.WindowHeight),
                BackColor = Color.FromArgb(28, 35, 28)
            };
            Controls.Add(_panel);

            int y = 8;

            // Mode label
            _lblMode = AddLabel($"{_config.GameMode} | {_config.Difficulty}", 8, ref y, 8,
                FontStyle.Italic, Color.FromArgb(120, 150, 120));
            AddSeparator(ref y);

            // Stats
            _lblGold = AddLabel("Gold:   150", 8, ref y, 11, FontStyle.Regular, Color.FromArgb(240, 210, 80));
            _lblHealth = AddLabel("Castle: 20/20", 8, ref y, 11, FontStyle.Regular, Color.FromArgb(220, 80, 80));
            _lblWave = AddLabel("Wave:   0 / 10", 8, ref y, 11, FontStyle.Regular, Color.FromArgb(80, 160, 240));
            _lblScore = AddLabel("Score:  0", 8, ref y, 11, FontStyle.Regular, Color.FromArgb(200, 200, 100));
            y += 2;
            AddSeparator(ref y);

            // Tower select
            AddLabel("SELECT TOWER", 8, ref y, 9, FontStyle.Bold, Color.FromArgb(160, 180, 160));
            _btnArcher = AddTowerButton("Archer   50g", ref y, TowerType.Archer);
            _btnMage = AddTowerButton("Mage    100g", ref y, TowerType.Mage);
            _btnCatapult = AddTowerButton("Catapult 150g", ref y, TowerType.Catapult);

            _lblTowerDesc = new Label
            {
                Text = "Fast attack, low damage",
                Location = new Point(8, y),
                Size = new Size(Constants.PanelWidth - 16, 34),
                ForeColor = Color.FromArgb(140, 160, 140),
                Font = new Font(Constants.FontName, 8, FontStyle.Italic),
                AutoSize = false
            };
            _panel.Controls.Add(_lblTowerDesc);
            y += 36;
            AddSeparator(ref y);

            // Countdown
            _lblCountdown = new Label
            {
                Text = "",
                Location = new Point(8, y),
                Size = new Size(Constants.PanelWidth - 16, 24),
                ForeColor = Color.FromArgb(255, 180, 60),
                Font = new Font(Constants.FontName, 11, FontStyle.Bold),
                AutoSize = false,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter
            };
            _panel.Controls.Add(_lblCountdown);
            y += 26;

            // Next wave button
            _btnNextWave = new Button
            {
                Text = "Next Wave",
                Location = new Point(8, y),
                Size = new Size(Constants.PanelWidth - 16, 38),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(50, 110, 50),
                ForeColor = Color.White,
                Font = new Font(Constants.FontName, 10, FontStyle.Bold),
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 0 }
            };
            _btnNextWave.Click += (s, e) => { _ctrl.StartNextWave(); _ctrl.State.StopCountdown(); };
            _panel.Controls.Add(_btnNextWave);
            y += 44;
            AddSeparator(ref y);

            // Wave preview panel
            _previewPanel = new Panel
            {
                Location = new Point(8, y),
                Size = new Size(Constants.PanelWidth - 16, 80),
                BackColor = Color.FromArgb(22, 30, 22)
            };
            _previewPanel.Paint += PreviewPanel_Paint;
            _panel.Controls.Add(_previewPanel);
            y += 84;
            AddSeparator(ref y);

            // Message
            _lblMessage = new Label
            {
                Text = "Place towers, then start!",
                Location = new Point(8, y),
                Size = new Size(Constants.PanelWidth - 16, 60),
                ForeColor = Color.FromArgb(170, 190, 170),
                Font = new Font(Constants.FontName, 8),
                AutoSize = false
            };
            _panel.Controls.Add(_lblMessage);

            // Help hint
            var lblHelp = new Label
            {
                Text = "LMB: place tower\nRMB: tower options\nHover: show range",
                Location = new Point(8, Constants.WindowHeight - 62),
                Size = new Size(Constants.PanelWidth - 16, 58),
                ForeColor = Color.FromArgb(90, 110, 90),
                Font = new Font(Constants.FontName, 8),
                AutoSize = false
            };
            _panel.Controls.Add(lblHelp);

            SelectTowerType(TowerType.Archer);
        }

        private Label AddLabel(string text, int x, ref int y, float size,
            FontStyle style, Color color)
        {
            var lbl = new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(Constants.PanelWidth - x * 2, 22),
                ForeColor = color,
                Font = new Font(Constants.FontName, size, style),
                AutoSize = false,
                BackColor = Color.Transparent
            };
            _panel.Controls.Add(lbl);
            y += 24;
            return lbl;
        }

        private void AddSeparator(ref int y)
        {
            var sep = new Panel
            {
                Location = new Point(8, y),
                Size = new Size(Constants.PanelWidth - 16, 1),
                BackColor = Color.FromArgb(55, 75, 55)
            };
            _panel.Controls.Add(sep);
            y += 6;
        }

        private Button AddTowerButton(string text, ref int y, TowerType type)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(8, y),
                Size = new Size(Constants.PanelWidth - 16, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(40, 58, 40),
                ForeColor = Color.White,
                Font = new Font(Constants.FontName, 9),
                Cursor = Cursors.Hand,
                Tag = type,
                FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(65, 85, 65) }
            };
            btn.Click += (s, e) => SelectTowerType((TowerType)((Button)s).Tag);
            _panel.Controls.Add(btn);
            y += 36;
            return btn;
        }

        private void SelectTowerType(TowerType type)
        {
            _ctrl.SelectedTowerType = type;
            _ctrl.DeselectTower();
            HidePopup();

            foreach (var btn in new[] { _btnArcher, _btnMage, _btnCatapult })
            {
                btn.BackColor = Color.FromArgb(40, 58, 40);
                btn.FlatAppearance.BorderColor = Color.FromArgb(65, 85, 65);
            }

            var sel = type switch
            {
                TowerType.Archer => _btnArcher,
                TowerType.Mage => _btnMage,
                TowerType.Catapult => _btnCatapult,
                _ => _btnArcher
            };
            sel.BackColor = Color.FromArgb(55, 105, 55);
            sel.FlatAppearance.BorderColor = Color.FromArgb(120, 200, 120);

            _lblTowerDesc.Text = type switch
            {
                TowerType.Archer => "Fast attack\nRange:120  Rate:2/s  DMG:15",
                TowerType.Mage => "High damage, large range\nRange:160  Rate:1/s  DMG:35",
                TowerType.Catapult => "Massive damage, slow\nRange:200  Rate:0.5/s DMG:80",
                _ => ""
            };
        }

        // ── Tower popup (RMB) ───────────────────────────────────────────────────
        private void InitializePopup()
        {
            _popup = new Panel
            {
                Size = new Size(160, 160),
                BackColor = Color.FromArgb(240, 28, 38, 28),
                Visible = false,
                BorderStyle = BorderStyle.FixedSingle
            };
            Controls.Add(_popup);
            _popup.BringToFront();
        }

        private void ShowPopup(Tower tower, Point screenPos)
        {
            _popupTower = tower;
            _ctrl.SelectTower(tower);
            _popup.Controls.Clear();

            int y = 6;

            // -- Title -----------------------------------------------------------
            var title = new Label
            {
                Text = $"{tower.Name}  Lv{tower.Level}",
                Location = new Point(6, y),
                Size = new Size(148, 20),
                ForeColor = Color.FromArgb(200, 230, 180),
                Font = new Font(Constants.FontName, 9, FontStyle.Bold),
                AutoSize = false
            };
            _popup.Controls.Add(title);
            y += 22;

            // -- Stats -----------------------------------------------------------
            var stats = new Label
            {
                Text = $"DMG:{tower.Damage}  RNG:{(int)tower.Range}  {tower.FireRate:F1}/s",
                Location = new Point(6, y),
                Size = new Size(148, 16),
                ForeColor = Color.FromArgb(160, 180, 160),
                Font = new Font(Constants.FontName, 7.5f),
                AutoSize = false
            };
            _popup.Controls.Add(stats);
            y += 20;

            // -- Priority buttons ------------------------------------------------
            var priorityLabel = new Label
            {
                Text = "TARGET:",
                Location = new Point(6, y),
                Size = new Size(60, 15),
                ForeColor = Color.FromArgb(140, 160, 140),
                Font = new Font(Constants.FontName, 7, FontStyle.Bold),
                AutoSize = false
            };
            _popup.Controls.Add(priorityLabel);
            y += 17;

            string[] pLabels = { "First", "Weakest", "Strongest" };
            TargetPriority[] pValues = { TargetPriority.First, TargetPriority.Weakest, TargetPriority.Strongest };
            for (int i = 0; i < 3; i++)
            {
                var pBtn = MakePopupButton(pLabels[i], 6, y, 146, 22);
                var pVal = pValues[i];
                pBtn.BackColor = tower.Priority == pVal
                    ? Color.FromArgb(55, 100, 55)
                    : Color.FromArgb(35, 50, 35);
                pBtn.Click += (s, e) =>
                {
                    tower.Priority = pVal;
                    ShowPopup(tower, _popup.Location); // refresh
                };
                _popup.Controls.Add(pBtn);
                y += 24;
            }

            y += 4;

            // -- Upgrade button --------------------------------------------------
            int maxLvl = _config.MaxUpgradeLevel;
            if (maxLvl > 1)
            {
                int upgCost = tower.GetUpgradeCost(maxLvl);
                string upgLabel = upgCost > 0
                    ? $"Lv{tower.Level}→{tower.Level + 1}  {upgCost}g"
                    : "MAX LEVEL";
                var btnUpg = MakePopupButton(upgLabel, 6, y, 70, 24);
                btnUpg.BackColor = upgCost > 0 && _ctrl.State.Gold >= upgCost
                    ? Color.FromArgb(40, 80, 140)
                    : Color.FromArgb(35, 35, 55);
                btnUpg.Click += (s, e) =>
                {
                    if (_ctrl.TryUpgradeTower(_popupTower))
                        ShowPopup(_popupTower, _popup.Location);
                };
                _popup.Controls.Add(btnUpg);
            }

            // -- Sell button -----------------------------------------------------
            var btnSell = MakePopupButton($"Sell {tower.SellValue}g",
                maxLvl > 1 ? 82 : 6, y, maxLvl > 1 ? 70 : 146, 24);
            btnSell.BackColor = Color.FromArgb(100, 40, 40);
            btnSell.Click += (s, e) =>
            {
                _ctrl.TrySellTower(_popupTower);
                HidePopup();
            };
            _popup.Controls.Add(btnSell);
            y += 28;

            // -- Resize and position popup ---------------------------------------
            _popup.Size = new Size(160, y + 4);

            // Keep popup inside window
            int px = screenPos.X + 10;
            int py = screenPos.Y - _popup.Height / 2;
            px = Math.Max(0, Math.Min(px, ClientSize.Width - _popup.Width));
            py = Math.Max(0, Math.Min(py, ClientSize.Height - _popup.Height));
            _popup.Location = new Point(px, py);
            _popup.Visible = true;
            _popup.BringToFront();
        }

        private Button MakePopupButton(string text, int x, int y, int w, int h)
        {
            return new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(w, h),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                Font = new Font(Constants.FontName, 7.5f),
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 0 }
            };
        }

        private void HidePopup()
        {
            if (_popup != null)
                _popup.Visible = false;
            _popupTower = null;
            _ctrl.DeselectTower();
        }

        // ── Wave preview panel paint ────────────────────────────────────────────
        private void PreviewPanel_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Color.FromArgb(22, 30, 22));

            var preview = _ctrl.NextWavePreview;
            int nextWave = _ctrl.State.CurrentWave + 1;
            bool isBoss = _ctrl.NextWaveIsBoss;

            using var titleFont = new Font(Constants.FontName, 7, FontStyle.Bold);
            using var titleBrush = new SolidBrush(Color.FromArgb(160, 190, 160));
            g.DrawString($"NEXT: Wave {nextWave}" + (isBoss ? "  [BOSS]" : ""),
                titleFont, titleBrush, 4, 4);

            if (preview == null) return;

            int y = 20;
            foreach (var grp in preview.Groups)
            {
                Color col = grp.Type switch
                {
                    EnemyType.Orc => Color.FromArgb(80, 200, 60),
                    EnemyType.Troll => Color.FromArgb(180, 130, 80),
                    EnemyType.Dragon => Color.FromArgb(220, 80, 60),
                    _ => Color.Gray
                };
                using var brush = new SolidBrush(col);
                using var font = new Font(Constants.FontName, 8);
                string name = grp.Type.ToString().ToUpper();
                g.FillEllipse(brush, 6, y + 1, 10, 10);
                g.DrawString($"{name} x{grp.Count}", font, brush, 20, y);
                y += 16;
            }

            if (isBoss)
            {
                using var bossBrush = new SolidBrush(Color.FromArgb(200, 100, 255));
                using var bossFont = new Font(Constants.FontName, 8, FontStyle.Bold);
                g.FillEllipse(bossBrush, 6, y + 1, 10, 10);
                g.DrawString("BOSS x1", bossFont, bossBrush, 20, y);
            }
        }

        // ── Game loop ───────────────────────────────────────────────────────────
        private void InitializeTimer()
        {
            _lastTick = DateTime.Now;
            _gameTimer = new System.Windows.Forms.Timer { Interval = Constants.TimerIntervalMs };
            _gameTimer.Tick += GameLoop;
            _gameTimer.Start();
        }

        private void GameLoop(object sender, EventArgs e)
        {
            var now = DateTime.Now;
            float dt = (float)(now - _lastTick).TotalSeconds;
            _lastTick = now;
            if (dt > 0.05f) dt = 0.05f;

            _ctrl.Update(dt);
            UpdatePanel();
            RenderFrame();
        }

        // ── Rendering ───────────────────────────────────────────────────────────
        private void RenderFrame()
        {
            using var g = Graphics.FromImage(_buffer);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Color.FromArgb(25, 25, 25));

            _ctrl.Draw(g);
            DrawPlacementPreview(g);

            _canvas.Refresh();
        }

        private void DrawPlacementPreview(Graphics g)
        {
            if (!_mouseOnMap || _mouseCell.X < 0) return;

            int px = _mouseCell.X * GameMap.CellSize;
            int py = _mouseCell.Y * GameMap.CellSize;
            bool canPlace = _ctrl.Map.CanPlaceTower(_mouseCell.X, _mouseCell.Y);
            bool canAfford = _ctrl.State.Gold >= _ctrl.GetTowerCost(_ctrl.SelectedTowerType);

            Color overlay = (canPlace && canAfford)
                ? Color.FromArgb(55, 100, 255, 100)
                : Color.FromArgb(55, 255, 80, 80);
            Color border = (canPlace && canAfford)
                ? Color.FromArgb(180, 100, 255, 100)
                : Color.FromArgb(180, 255, 80, 80);

            using var brush = new SolidBrush(overlay);
            g.FillRectangle(brush, px, py, GameMap.CellSize, GameMap.CellSize);
            using var pen = new Pen(border, 2);
            g.DrawRectangle(pen, px, py, GameMap.CellSize, GameMap.CellSize);

            if (canPlace && canAfford)
            {
                float range = _ctrl.SelectedTowerType switch
                {
                    TowerType.Archer => 120f,
                    TowerType.Mage => 160f,
                    TowerType.Catapult => 200f,
                    _ => 120f
                };
                float cx = px + GameMap.CellSize / 2f;
                float cy = py + GameMap.CellSize / 2f;
                using var rb = new SolidBrush(Color.FromArgb(14, 100, 200, 100));
                g.FillEllipse(rb, cx - range, cy - range, range * 2, range * 2);
                using var rp = new Pen(Color.FromArgb(45, 100, 200, 100), 1);
                g.DrawEllipse(rp, cx - range, cy - range, range * 2, range * 2);
            }
        }

        // ── Panel update ────────────────────────────────────────────────────────
        private void UpdatePanel()
        {
            var s = _ctrl.State;
            _lblGold.Text = $"Gold:   {s.Gold}";
            _lblHealth.Text = $"Castle: {s.CastleHealth}/{s.MaxCastleHealth}";
            _lblWave.Text = _config.GameMode == GameMode.Endless
                ? $"Wave:   {s.CurrentWave}"
                : $"Wave:   {s.CurrentWave} / 10";
            _lblScore.Text = $"Score:  {s.Score}";

            // Countdown display
            if (s.CountdownActive)
                _lblCountdown.Text = $"Next wave in {s.CountdownSeconds:F1}s";
            else
                _lblCountdown.Text = "";

            _btnNextWave.Enabled = _ctrl.CanStartNextWave;
            _btnNextWave.BackColor = _ctrl.CanStartNextWave
                ? Color.FromArgb(50, 120, 50)
                : Color.FromArgb(40, 50, 40);

            _previewPanel.Invalidate();
        }

        private void ShowMessage(string msg)
        {
            if (_lblMessage.InvokeRequired)
                _lblMessage.Invoke(new Action(() => _lblMessage.Text = msg));
            else
                _lblMessage.Text = msg;
        }

        // ── Input ───────────────────────────────────────────────────────────────
        private void Canvas_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                // Close popup on LMB anywhere on map
                if (_popup.Visible) { HidePopup(); return; }
                _ctrl.TryPlaceTower(e.X, e.Y);
            }
            else if (e.Button == MouseButtons.Right)
            {
                var tower = _ctrl.GetTowerAt(e.X, e.Y);
                if (tower != null)
                {
                    var screenPt = _canvas.PointToScreen(new Point(e.X, e.Y));
                    var formPt = PointToClient(screenPt);
                    ShowPopup(tower, formPt);
                }
                else
                {
                    HidePopup();
                }
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            _mouseOnMap = true;
            _mouseCell = new Point(e.X / GameMap.CellSize, e.Y / GameMap.CellSize);
            _ctrl.UpdateHover(e.X, e.Y);
        }

        // ── Game over / Victory ─────────────────────────────────────────────────
        private void HandleGameOver()
        {
            _gameTimer.Stop();
            Invoke(new Action(() =>
            {
                var f = new GameOverForm(false, _ctrl.State.Score, _ctrl.State.CurrentWave);
                f.Owner = this;
                f.ShowDialog();
            }));
        }

        private void HandleVictory()
        {
            _gameTimer.Stop();
            Invoke(new Action(() =>
            {
                var f = new GameOverForm(true, _ctrl.State.Score, _ctrl.State.CurrentWave);
                f.Owner = this;
                f.ShowDialog();
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
