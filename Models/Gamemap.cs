using System.Collections.Generic;
using System.Drawing;

namespace TowerDefense.Models
{
    public enum CellType { Grass, Path, Castle, Spawn }

    /// <summary>
    /// Game map: grid of cells and enemy path.
    /// </summary>
    public class GameMap
    {
        public const int CellSize = 48;
        public const int Cols = 18;
        public const int Rows = 13;

        public CellType[,] Grid { get; private set; }
        public List<Point> EnemyPath { get; private set; }

        public Point SpawnPoint => EnemyPath[0];
        public Point CastlePoint => EnemyPath[EnemyPath.Count - 1];

        private static readonly Color GrassLight = Color.FromArgb(80, 150, 60);
        private static readonly Color GrassDark = Color.FromArgb(65, 130, 50);
        private static readonly Color PathColor = Color.FromArgb(200, 170, 110);
        private static readonly Color PathEdge = Color.FromArgb(170, 140, 80);

        public GameMap()
        {
            Grid = new CellType[Cols, Rows];
            EnemyPath = new List<Point>();
            BuildMap();
        }

        private void BuildMap()
        {
            for (int x = 0; x < Cols; x++)
                for (int y = 0; y < Rows; y++)
                    Grid[x, y] = CellType.Grass;

            var path = new List<Point>
            {
                new Point(0, 2),
                new Point(1, 2), new Point(2, 2), new Point(3, 2), new Point(4, 2),
                new Point(4, 3), new Point(4, 4), new Point(4, 5), new Point(4, 6),
                new Point(5, 6), new Point(6, 6), new Point(7, 6), new Point(8, 6),
                new Point(8, 5), new Point(8, 4), new Point(8, 3), new Point(8, 2),
                new Point(9, 2), new Point(10, 2), new Point(11, 2), new Point(12, 2),
                new Point(12, 3), new Point(12, 4), new Point(12, 5), new Point(12, 6),
                new Point(12, 7), new Point(12, 8), new Point(12, 9),
                new Point(11, 9), new Point(10, 9), new Point(9, 9), new Point(8, 9),
                new Point(7, 9), new Point(6, 9), new Point(5, 9), new Point(4, 9),
                new Point(4, 10), new Point(4, 11),
                new Point(5, 11), new Point(6, 11), new Point(7, 11), new Point(8, 11),
                new Point(9, 11), new Point(10, 11), new Point(11, 11), new Point(12, 11),
                new Point(13, 11), new Point(14, 11), new Point(15, 11), new Point(16, 11),
                new Point(16, 10), new Point(16, 9), new Point(16, 8), new Point(16, 7),
                new Point(16, 6), new Point(16, 5), new Point(16, 4), new Point(16, 3),
                new Point(17, 3)
            };

            EnemyPath = path;

            foreach (var p in path)
                if (p.X >= 0 && p.X < Cols && p.Y >= 0 && p.Y < Rows)
                    Grid[p.X, p.Y] = CellType.Path;

            Grid[path[0].X, path[0].Y] = CellType.Spawn;
            Grid[path[path.Count - 1].X, path[path.Count - 1].Y] = CellType.Castle;
        }

        public bool CanPlaceTower(int col, int row)
        {
            if (col < 0 || col >= Cols || row < 0 || row >= Rows) return false;
            return Grid[col, row] == CellType.Grass;
        }

        public void Draw(Graphics g)
        {
            for (int x = 0; x < Cols; x++)
            {
                for (int y = 0; y < Rows; y++)
                {
                    int px = x * CellSize;
                    int py = y * CellSize;
                    var rect = new Rectangle(px, py, CellSize, CellSize);

                    switch (Grid[x, y])
                    {
                        case CellType.Grass:
                            bool checker = (x + y) % 2 == 0;
                            using (var b = new SolidBrush(checker ? GrassLight : GrassDark))
                                g.FillRectangle(b, rect);
                            // Grass details
                            if (checker)
                            {
                                using var detailPen = new Pen(Color.FromArgb(40, 0, 80, 0), 1);
                                g.DrawLine(detailPen, px + 10, py + 12, px + 10, py + 20);
                                g.DrawLine(detailPen, px + 28, py + 28, px + 28, py + 36);
                                g.DrawLine(detailPen, px + 36, py + 10, px + 36, py + 18);
                            }
                            break;

                        case CellType.Path:
                            g.FillRectangle(new SolidBrush(PathColor), rect);
                            g.DrawRectangle(new Pen(PathEdge, 1), rect);
                            break;

                        case CellType.Spawn:
                            g.FillRectangle(new SolidBrush(PathColor), rect);
                            DrawSpawn(g, px, py);
                            break;

                        case CellType.Castle:
                            g.FillRectangle(new SolidBrush(Color.FromArgb(150, 130, 100)), rect);
                            DrawCastle(g, px, py);
                            break;
                    }
                }
            }
        }

        private void DrawSpawn(Graphics g, int px, int py)
        {
            // Green portal circle
            using var outerBrush = new SolidBrush(Color.FromArgb(60, 200, 60));
            g.FillEllipse(outerBrush, px + 8, py + 8, 32, 32);

            using var innerBrush = new SolidBrush(Color.FromArgb(30, 255, 30));
            g.FillEllipse(innerBrush, px + 14, py + 14, 20, 20);

            using var borderPen = new Pen(Color.LimeGreen, 2);
            g.DrawEllipse(borderPen, px + 8, py + 8, 32, 32);

            // Arrow pointing right
            using var arrowPen = new Pen(Color.White, 2);
            g.DrawLine(arrowPen, px + 16, py + 24, px + 32, py + 24);
            g.DrawLine(arrowPen, px + 27, py + 19, px + 32, py + 24);
            g.DrawLine(arrowPen, px + 27, py + 29, px + 32, py + 24);

            // Label
            using var font = new Font("Arial", 6, FontStyle.Bold);
            g.DrawString("START", font, Brushes.White, px + 8, py + 2);
        }

        private void DrawCastle(Graphics g, int px, int py)
        {
            // Castle walls
            using var wallBrush = new SolidBrush(Color.FromArgb(160, 140, 110));
            g.FillRectangle(wallBrush, px + 8, py + 16, 32, 28);

            // Battlements (3 merlons)
            g.FillRectangle(wallBrush, px + 8, py + 8, 8, 10);
            g.FillRectangle(wallBrush, px + 20, py + 8, 8, 10);
            g.FillRectangle(wallBrush, px + 32, py + 8, 8, 10);

            // Gate
            using var gateBrush = new SolidBrush(Color.FromArgb(60, 45, 30));
            g.FillRectangle(gateBrush, px + 18, py + 30, 12, 14);

            // Gate arch
            g.FillEllipse(gateBrush, px + 18, py + 26, 12, 8);

            // Wall outline
            using var wallPen = new Pen(Color.FromArgb(100, 85, 60), 1);
            g.DrawRectangle(wallPen, px + 8, py + 16, 32, 28);

            // Label
            using var font = new Font("Arial", 6, FontStyle.Bold);
            g.DrawString("CASTLE", font, Brushes.White, px + 4, py + 2);
        }
    }
}