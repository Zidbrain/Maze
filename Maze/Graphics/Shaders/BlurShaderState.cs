using Microsoft.Xna.Framework.Graphics;

namespace Maze.Graphics.Shaders
{
    public class BlurShaderState : DefferedShaderState
    {
        public BlurShaderState(Texture2D color) : base(color) { }

        public override void Apply(EffectParameterCollection parameters)
        {
            base.Apply(parameters);
        }

        public override EffectTechnique GetTechnique(EffectTechniqueCollection techniques) =>
            techniques["Blur"];
    }
}
