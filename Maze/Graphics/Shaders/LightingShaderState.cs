using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Maze.Graphics.Shaders
{
    public class LightingShaderState : DefferedShaderState
    {
        public LightingShaderState(Texture2D colors, Texture2D normals, Texture2D positions) : base(colors) =>
            (Normal, Position) = (normals, positions);

        public List<LightingShaderData> LightingData { get; set; } = new List<LightingShaderData>(LightEngine.LightBatchCount);

        public override void Apply(EffectParameterCollection parameters)
        {
            base.Apply(parameters);

            parameters["_lightsCount"].SetValue(LightingData.Count);

            var positions = new Vector3[LightingData.Count];
            var colors = new Vector4[LightingData.Count];
            var radiuses = new float[LightingData.Count];
            var powers = new float[LightingData.Count];
            var hardnesses = new float[LightingData.Count];
            var specularHardnesses = new float[LightingData.Count];
            var specularPowers = new float[LightingData.Count];

            for (int i = 0; i < LightingData.Count; i++)
            {
                positions[i] = LightingData[i].LightPosition;
                colors[i] = LightingData[i].LightColor.ToVector4();
                radiuses[i] = LightingData[i].Radius;
                powers[i] = LightingData[i].DiffusePower;
                hardnesses[i] = LightingData[i].Hardness;
                specularHardnesses[i] = LightingData[i].SpecularHardness;
                specularPowers[i] = LightingData[i].SpecularPower;
            }

            parameters["_lightingPosition"].SetValue(positions);
            parameters["_lightingColor"].SetValue(colors);
            parameters["_lightingRadius"].SetValue(radiuses);
            parameters["_diffusePower"].SetValue(powers);
            parameters["_hardness"].SetValue(hardnesses);
            parameters["_specularHardness"].SetValue(specularHardnesses);
            parameters["_specularPower"].SetValue(specularPowers);
        }

        public override EffectTechnique GetTechnique(EffectTechniqueCollection techniques) =>
            techniques["Lighting"];
    }

    public class LightingShaderData
    {
        public float Radius { get; set; }

        public Vector3 LightPosition { get; set; }

        public Color LightColor { get; set; } = Color.White;

        public float DiffusePower { get; set; } = 1f;

        public float Hardness { get; set; } = 1f;

        public float SpecularHardness { get; set; } = 1f;

        public float SpecularPower { get; set; } = 1f;
    }
}
