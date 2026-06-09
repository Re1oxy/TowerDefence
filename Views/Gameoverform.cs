using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace TowerDefense.Views
{
    /// <summary>
    /// Shown when the player wins or loses.
    /// </summary>
    public class GameOverForm : Form
    {
        private readonly bool _isVictory;
        private readonly int _score;
        private readonly int _wave;

        public GameOverForm(bool isVictory, int score, int wave)
        {
            _isVictory = isVictory;
            _score = score;
            _wave = wave;

            InitializeForm();
            InitializeControls();
        }

        private void InitializeForm()
        {
            Text = _isVictory ? "Victory!" : "Game Over";
            ClientSize = new Size(480, 360);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = _isVictory
                ? Color.FromArgb(20, 40, 20)
                : Color.FromArgb(40, 15, 15);
            DoubleBuffered = true;
        }

        private void InitializeControls()
        {
            // ── Main menu button ─────────────────────────────────────────────
            var btnMenu = new Button
            {
                Text = "Main Menu",
                Location = new Point(80, 270),
                Size = new Size(140, 48),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(50, 80, 50),
                ForeColor = Color.White,
                Font = new Font(Utils.Constants.FontName, 11, FontStyle.Bold),
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 0 }
            };
            btnMenu.Click += (s, e) =>
            {
                // Close both this form and GameForm — MainMenu is still alive
                Owner?.Close();
                Close();
            };

            // ── Play again button ────────────────────────────────────────────
            var btnAgain = new Button
            {
                Text = "Play Again",
                Location = new Point(260, 270),
                Size = new Size(140, 48),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(60, 100, 180),
                ForeColor = Color.White,
                Font = new Font(Utils.Constants.FontName, 11, FontStyle.Bold),
                Cursor = Cursors.Hand,
                FlatAppearance = { BorderSize = 0 }
            };
            btnAgain.Click += (s, e) =>
            {
                var modeForm = new ModeSelectForm();
                if (modeForm.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var gameForm = new GameForm(modeForm.SelectedConfig);
                    gameForm.Show();
                    Owner?.Hide();
                }
                Close();
            };

            Controls.Add(btnMenu);
            Controls.Add(btnAgain);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Background gradient
            using var bg = new LinearGradientBrush(
                ClientRectangle,
                _isVictory ? Color.FromArgb(30, 60, 30) : Color.FromArgb(60, 20, 20),
                _isVictory ? Color.FromArgb(10, 20, 10) : Color.FromArgb(20, 5, 5),
                LinearGradientMode.Vertical);
            g.FillRectangle(bg, ClientRectangle);

            // Header
            string header = _isVictory ? "VICTORY!" : "GAME OVER";
            using var headerFont = new Font(Utils.Constants.FontName, 36, FontStyle.Bold);
            using var headerBrush = new SolidBrush(_isVictory
                ? Color.FromArgb(220, 200, 60)
                : Color.FromArgb(220, 80, 60));
            var hSize = g.MeasureString(header, headerFont);
            g.DrawString(header, headerFont, headerBrush,
                (ClientSize.Width - hSize.Width) / 2f, 50);

            // Stats
            using var statFont = new Font(Utils.Constants.FontName, 14);
            using var statBrush = new SolidBrush(Color.FromArgb(200, 220, 200));

            string waveLine = _isVictory
                ? $"All {_wave} waves survived!"
                : $"Survived {_wave - 1} waves";
            string scoreLine = $"Final score:  {_score}";

            var w1 = g.MeasureString(waveLine, statFont);
            var w2 = g.MeasureString(scoreLine, statFont);

            g.DrawString(waveLine, statFont, statBrush, (ClientSize.Width - w1.Width) / 2f, 165);
            g.DrawString(scoreLine, statFont, statBrush, (ClientSize.Width - w2.Width) / 2f, 200);

            // Divider line
            using var linePen = new Pen(Color.FromArgb(80, 200, 200, 200), 1);
            g.DrawLine(linePen, 60, 250, ClientSize.Width - 60, 250);
        }
    }
}
