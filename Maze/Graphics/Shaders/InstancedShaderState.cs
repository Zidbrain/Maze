﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Maze.Graphics.Shaders
{
    public abstract class InstancedShaderState : ShaderState
    {
        public Matrix WorldViewProjection { get; set; }

        public Matrix[] Matrices { get; set; }

        public override void Apply(EffectParameterCollection parameters)
        {
            parameters["_matrix"].SetValue(WorldViewProjection);
            parameters["_matrices"].SetValue(Matrices);
        }
    }

    public class LevelMeshShaderState : InstancedShaderState
    {
        public Color Color { get; set; } = Color.White;

        public Texture2D Texture { get; set; }

        public Texture2D NormalTexture { get; set; }

        public bool OnlyColor { get; set; }

        public override void Apply(EffectParameterCollection parameters)
        {
            base.Apply(parameters);
            parameters["_color"].SetValue(Color.ToVector4());
            parameters["_texture"].SetValue(Texture);
            parameters["_onlyColor"].SetValue(OnlyColor);

            if (NormalTexture is null)
                parameters["_normalEnabled"].SetValue(false);
            else
            {
                parameters["_normalEnabled"].SetValue(true);
                parameters["_normalTexture"].SetValue(NormalTexture);
            }
        }

        public override EffectTechnique GetTechnique(EffectTechniqueCollection techniques) =>
            techniques["Instanced"];
    }
}