using System.Drawing;

namespace TowerDefense.Models
{
    /// <summary>
    /// Basic abstract class for all game entities.
    /// </summary>
    public abstract class Entity
    {
        public float X { get; set; }
        public float Y { get; set; }
        public int Width { get; protected set; }
        public int Height { get; protected set; }
        public bool IsAlive { get; protected set; } = true;

        protected Entity(float x, float y, int width, int height)
        {
            X = x; Y = y; Width = width; Height = height;
        }

        public RectangleF Bounds => new RectangleF(X, Y, Width, Height);

        public abstract void Update(float deltaTime);
        public abstract void Draw(Graphics g);
    }
}

