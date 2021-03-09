using System;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace Maze.Graphics.Shaders
{
    public class ShadowMapShaderState : DefferedShaderState
    {
        public Texture2D DepthMap { get; set; }

        public Vector3 LightPosition { get; set; }

        public Matrix[] LightViewMatrices { get; set; }

        public ShadowMapShaderState(Texture2D positions) : base() =>
            Position = positions;

        public override void Apply(EffectParameterCollection parameters)
        {
            base.Apply(parameters);

            parameters["_lightShadows"].SetValue(DepthMap);
            parameters["_lightPosition"].SetValue(LightPosition);
            parameters["_lightViewMatrices"].SetValue(LightViewMatrices);
            parameters["_lightViewLength"].SetValue(LightViewMatrices.Length);
        }

        public override EffectTechnique GetTechnique(EffectTechniqueCollection techniques) =>
            techniques["GenerateShadowMap"];
    }
}
