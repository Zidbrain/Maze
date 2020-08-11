using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Maze.Graphics.Shaders
{
    public class PointLightShaderState : DefferedShaderState
    {
        public PointLightShaderState(Texture2D colors, Texture2D normals, Texture2D positions) : base(colors) =>
            (Normal, Position) = (normals, positions);

        public List<PointLightShaderData> LightingData { get; set; } = new List<PointLightShaderData>(LightEngine.LightBatchCount);

        public Vector3 CameraPosition { get; set; }

        public Texture2D ShadowMaps { get; set; }

        public override void Apply(EffectParameterCollection parameters)
        {
            base.Apply(parameters);

            parameters["_lightsCount"].SetValue(LightingData.Count);
            parameters["_cameraPosition"].SetValue(CameraPosition);
            parameters["_shadowMaps"].SetValue(ShadowMaps);

            var positions = new Vector3[LightingData.Count];
            var colors = new Vector4[LightingData.Count];
            var radiuses = new float[LightingData.Count];
            var powers = new float[LightingData.Count];
            var hardnesses = new float[LightingData.Count];
            var specularHardnesses = new float[LightingData.Count];
            var specularPowers = new float[LightingData.Count];
            var shadowsEnabled = new int[LightingData.Count];

            for (int i = 0; i < LightingData.Count; i++)
            {
                positions[i] = LightingData[i].LightPosition;
                colors[i] = LightingData[i].LightColor.ToVector4();
                radiuses[i] = LightingData[i].Radius;
                powers[i] = LightingData[i].DiffusePower;
                hardnesses[i] = LightingData[i].Hardness;
                specularHardnesses[i] = LightingData[i].SpecularHardness;
                specularPowers[i] = LightingData[i].SpecularPower;
                shadowsEnabled[i] = System.Convert.ToInt32(LightingData[i].ShadowsEnabled);
            }

            parameters["_lightingPosition"].SetValue(positions);
            parameters["_lightingColor"].SetValue(colors);
            parameters["_lightingRadius"].SetValue(radiuses);
            parameters["_diffusePower"].SetValue(powers);
            parameters["_hardness"].SetValue(hardnesses);
            parameters["_specularHardness"].SetValue(specularHardnesses);
            parameters["_specularPower"].SetValue(specularPowers);
            parameters["_shadowsEnabled"].SetValue(shadowsEnabled);
        }

        public override EffectTechnique GetTechnique(EffectTechniqueCollection techniques) =>
            techniques["Lighting"];
    }

    public class WriteDepthShaderState : TransformShaderState
    {
        public Vector3 LightPosition { get; set; }

        public override void Apply(EffectParameterCollection parameters)
        {
            base.Apply(parameters);
            parameters["_lightPosition"].SetValue(LightPosition);
        }

        public override EffectTechnique GetTechnique(EffectTechniqueCollection techniques) =>
            techniques["WriteDepth"];
    }

    public class WriteDepthInstancedShaderState : InstancedShaderState
    {
        public Vector3 LightPosition { get; set; }

        public Texture2D Texture { get; set; }

        public override void Apply(EffectParameterCollection parameters)
        {
            base.Apply(parameters);
            parameters["_texture"].SetValue(Texture);
            parameters["_lightPosition"].SetValue(LightPosition);
        }

        public override EffectTechnique GetTechnique(EffectTechniqueCollection techniques) =>
            techniques["WriteDepthInstanced"];
    }

    public class PointLightShaderData
    {
        public float Radius { get; set; }

        public Vector3 LightPosition { get; set; }

        public Color LightColor { get; set; } = Color.White;

        public float DiffusePower { get; set; } = 1f;

        public float Hardness { get; set; } = 1f;

        public float SpecularHardness { get; set; } = 1f;

        public float SpecularPower { get; set; } = 1f;

        public bool ShadowsEnabled { get; set; } = true;
    }
}
