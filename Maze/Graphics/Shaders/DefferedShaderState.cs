using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace Maze.Graphics.Shaders
{
    public class DefferedShaderState : IShaderState
    {
        public virtual void Apply(EffectParameterCollection parameters)
        {
            parameters["_texture"].SetValue(Color);
           // parameters["_depthBuffer"].SetValue(Depth);
            parameters["_normalBuffer"].SetValue(Normal);
            parameters["_positionBuffer"].SetValue(Position);
        }

        public Texture2D Color { get; set; }
        public Texture2D Depth { get; set; }
        public Texture2D Normal { get; set; }
        public Texture2D Position { get; set; }

        public DefferedShaderState(RenderTargets renderTargets)
        {
            Color = renderTargets.Color;
            Depth = renderTargets.Depth;
            Normal = renderTargets.Normal;
            Position = renderTargets.Position;
        }

        public DefferedShaderState(Texture2D color) =>
            Color = color;

        public DefferedShaderState() { }

        public virtual EffectTechnique GetTechnique(EffectTechniqueCollection techniques) =>
            techniques["Rasterize"];
    }
}
