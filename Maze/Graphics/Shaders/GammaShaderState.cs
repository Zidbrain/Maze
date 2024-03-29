﻿using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Maze.Graphics.Shaders
{
    public class GammaShaderState : DefferedShaderState
    {
        public float Gamma { get; set; } = 1f;

        public GammaShaderState(Texture2D source) : base(source) { }

        public override void Apply(EffectParameterCollection parameters)
        {
            base.Apply(parameters);

            parameters["_gamma"].SetValue(Gamma);
        }

        public override EffectTechnique GetTechnique(EffectTechniqueCollection techniques) =>
            techniques["Gamma"];
    }

    public class MaskShaderState : DefferedShaderState
    {
        public Color MaskColor { get; set; } = Microsoft.Xna.Framework.Color.White;
        public Texture2D Mask { get; set; }

        public MaskShaderState(Texture2D source) : base(source) { }

        public override void Apply(EffectParameterCollection parameters)
        {
            base.Apply(parameters);

            parameters["_color"].SetValue(MaskColor.ToVector4());
            parameters["_mask"].SetValue(Mask);
        }

        public override EffectTechnique GetTechnique(EffectTechniqueCollection techniques) =>
            techniques["Mask"];
    }
}
