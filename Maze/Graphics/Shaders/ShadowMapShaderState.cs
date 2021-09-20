using System;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace Maze.Graphics.Shaders
{
    public class ShadowMapShaderState : DefferedShaderState
    {
        public Texture DepthMap { get; set; }

        public ShadowMapShaderState(Texture2D positions) : base() => Position = positions;
    }

    public class SpotLightShadowMapShaderState : ShadowMapShaderState
    {
        public Matrix LightView { get; set; }

        public Vector3 CameraPosition { get; set; }

        public SpotLight SpotLight { get; set; }

        public SpotLightShadowMapShaderState(Texture2D positions, Texture2D normals) : base(positions) { Normal = normals; }

        public override void Apply(EffectParameterCollection parameters)
        {
            base.Apply(parameters);
            parameters["_matrix"].SetValue(LightView);
            parameters["_spotLightDepthMap"].SetValue(DepthMap);
            parameters["_lightPosition"].SetValue(SpotLight.Position);
            parameters["_lightDirection"].SetValue(SpotLight.Direction);
            parameters["_lightAngle"].SetValue(SpotLight.DiversionAngle);
            parameters["_lightReach"].SetValue(SpotLight.Radius);
            parameters["_cameraPosition"].SetValue(CameraPosition);
            parameters["_lightHardness"].SetValue(SpotLight.Hardness);
            parameters["_lightSpecularHardness"].SetValue(SpotLight.SpecularHardness);
            parameters["_lightSpecularPower"].SetValue(SpotLight.SpecularPower);
            parameters["_lightDiffusePower"].SetValue(SpotLight.DiffusePower);
        }

        public override EffectTechnique GetTechnique(EffectTechniqueCollection techniques) =>
            techniques["SpotLightShadowMap"];
    }

    public class PointLightShadowMapShaderState : ShadowMapShaderState
    {
        public Vector3 LightPosition { get; set; }

        public float FarDist { get; set; }

        public float NearDist { get; set; }

        public PointLightShadowMapShaderState(Texture2D positions) : base(positions) { }

        public override void Apply(EffectParameterCollection parameters)
        {
            base.Apply(parameters);

            parameters["_lightShadows"].SetValue(DepthMap);
            parameters["_lightPosition"].SetValue(LightPosition);
            parameters["_farPlane"].SetValue(FarDist);
            parameters["_nearPlane"].SetValue(NearDist);
        }

        public override EffectTechnique GetTechnique(EffectTechniqueCollection techniques) => techniques["GenerateShadowMap"];
    }
}
