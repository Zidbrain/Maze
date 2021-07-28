using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Maze.Graphics.Shaders
{
    public class GammaShaderState : DefferedShaderState
    {
        public Color MaskColor { get; set; } = Microsoft.Xna.Framework.Color.White;

        public float Gamma { get; set; } = 1f;

        public Texture2D Mask { get; set; }

        public GammaShaderState(Texture2D source) : base(source) { }

        public override void Apply(EffectParameterCollection parameters)
        {
            base.Apply(parameters);

            parameters["_color"].SetValue(MaskColor.ToVector4());
            parameters["_gamma"].SetValue(Gamma);
            parameters["_mask"].SetValue(Mask);
        }

        public override EffectTechnique GetTechnique(EffectTechniqueCollection techniques) =>
            techniques["Gamma"];
    }
}
