using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Maze.Graphics.Shaders
{
    public interface ILightShaderState<out TLight> : IShaderState where TLight : Light
    {
        TLight[] LightingData { get; }

        Vector3 CameraPosition { get; set; }

        Texture2D ShadowMaps { get; set; }
    }

    public abstract class LightShaderState<TLight> : DefferedShaderState, ILightShaderState<TLight> where TLight : Light
    {
        protected LightShaderState(Texture2D colors, Texture2D normals, Texture2D positions) : base(colors) =>
            (Normal, Position) = (normals, positions);

        public override abstract EffectTechnique GetTechnique(EffectTechniqueCollection techniques);

        public override void Apply(EffectParameterCollection parameters)
        {
            base.Apply(parameters);

            var count = LightingData.Length;
            for (int i = 0; i < LightingData.Length; i++)
                if (LightingData[i] is null)
                {
                    count = i;
                    break;
                }

            parameters["_lightsCount"].SetValue(count);
            parameters["_cameraPosition"].SetValue(CameraPosition);
            parameters["_shadowMaps"].SetValue(ShadowMaps);

            var positions = new Vector3[count];
            var shadowsEnabled = new int[count];
            var radii = new float[count];

            for (int i = 0; i < count; i++)
            {
                radii[i] = LightingData[i].Radius;
                positions[i] = LightingData[i].Position;
                shadowsEnabled[i] = System.Convert.ToInt32(LightingData[i].ShadowsEnabled);
            }

            parameters["_lightingRadius"].SetValue(radii);
            parameters["_lightingPosition"].SetValue(positions);
            parameters["_shadowsEnabled"].SetValue(shadowsEnabled);

            SetParameters(parameters, count);
        }

        public TLight[] LightingData { get; } = new TLight[LightEngine.LightBatchCount];

        public Vector3 CameraPosition { get; set; }

        public Texture2D ShadowMaps { get; set; }

        /// <summary>
        /// Set the parameters of a <see cref="Light"/> to a shader
        /// </summary>
        /// <remarks><see cref="LightingData"/> stores <see cref="Light"/>s which parameters need to be passed down to a shader</remarks>
        /// <param name="parameters">Shader parameters.</param>
        /// <param name="count">Number of non null elements in <see cref="LightingData"/></param>
        protected abstract void SetParameters(EffectParameterCollection parameters, int count);
    }

    public class PointLightShaderState : LightShaderState<PointLight>
    {
        public PointLightShaderState(Texture2D colors, Texture2D normals, Texture2D positions) : base(colors, normals, positions) { }

        protected override void SetParameters(EffectParameterCollection parameters, int count)
        {
            var colors = new Vector4[count];
            var powers = new float[count];
            var hardnesses = new float[count];
            var specularHardnesses = new float[count];
            var specularPowers = new float[count];

            for (int i = 0; i < count; i++)
            {
                colors[i] = LightingData[i].Color.ToVector4();
                powers[i] = LightingData[i].DiffusePower;
                hardnesses[i] = LightingData[i].Hardness;
                specularHardnesses[i] = LightingData[i].SpecularHardness;
                specularPowers[i] = LightingData[i].SpecularPower;
            }

            parameters["_lightingColor"].SetValue(colors);
            parameters["_diffusePower"].SetValue(powers);
            parameters["_hardness"].SetValue(hardnesses);
            parameters["_specularHardness"].SetValue(specularHardnesses);
            parameters["_specularPower"].SetValue(specularPowers);
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


        public override void Apply(EffectParameterCollection parameters)
        {
            base.Apply(parameters);
            parameters["_lightPosition"].SetValue(LightPosition);
        }

        public override EffectTechnique GetTechnique(EffectTechniqueCollection techniques) =>
            techniques["WriteDepthInstanced"];
    }
}
