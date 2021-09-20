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

        public abstract Shaders.ShadowMapShaderState GetShadows(EnumerableLevelObjects objects);

        public abstract void Dispose();

        public abstract void LoadStaticState(EnumerableLevelObjects staticObjects);

        protected static readonly RasterizerState shadowRasterizer = new()
        {
            DepthBias = 0.002f,
            SlopeScaleDepthBias = 0.007f,
            MultiSampleAntiAlias = false,
            CullMode = CullMode.None
        };
    }
}
