using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Maze.Engine;

namespace Maze.Graphics
{
    public abstract class Light : System.IDisposable
    {
        public virtual Color Color { get; set; } = Color.White;

        public bool IsStatic { get; init; }

        public virtual Vector3 Position { get; set; }

        public virtual float Radius { get; set; }

        public virtual bool ShadowsEnabled { get; set; } = true;

        /// <summary>
        /// Get shadow texture for input objects and view matrices of this light
        /// </summary>
        /// <param name="objects">Objects, which shadow needs to be drawn</param>
        /// <param name="lightViewMatrices">View matrices which were used to draw the shadow</param>
        /// <returns></returns>
        public abstract Texture2D GetShadows(EnumerableLevelObjects objects, out Matrix[] lightViewMatrices);

        public abstract void Dispose();

        public abstract void LoadStaticState(EnumerableLevelObjects staticObjects);
    }
}
