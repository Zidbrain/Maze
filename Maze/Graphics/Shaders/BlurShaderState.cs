using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Maze.Graphics;

namespace Maze.Graphics.Shaders
{
    public class BlurShaderState : DefferedShaderState
    {
        public BlurShaderState(Texture2D color) : base(color) { }

        public bool IsVertical { get; set; }

        public override void Apply(EffectParameterCollection parameters)
        {
            base.Apply(parameters);
            parameters["_vertical"].SetValue(IsVertical);
        }

        public override EffectTechnique GetTechnique(EffectTechniqueCollection techniques) =>
            techniques["Blur"];
    }
}
