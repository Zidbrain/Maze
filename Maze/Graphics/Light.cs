using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Maze.Graphics
{
    public abstract class Light
    {
        public virtual Color Color { get; set; } = Color.White;

        public virtual Vector3 Position { get; set; }

        public virtual bool ShadowsEnabled { get; set; } = true;

        public abstract Texture2D GetShadows(out Matrix[] lightViewMatrices);
    }
}
