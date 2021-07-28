using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Maze.Graphics.Shaders
{
    public class SpotLightShaderState : LightShaderState<SpotLight>
    {
        public SpotLightShaderState(Texture2D colors, Texture2D normals, Texture2D positions) : base(colors, normals, positions) { }

        protected override void SetParameters(EffectParameterCollection parameters, int count)
        {
            var colors = new Vector4[count];
            var powers = new float[count];
            var hardnesses = new float[count];
            var specularHardnesses = new float[count];
            var specularPowers = new float[count];

            var angles = new float[count];
            var directions = new Vector3[count];
            var matrices = new Matrix[count];

            for (int i = 0; i < count; i++)
            {
                var data = LightingData[i];

                colors[i] = data.Color.ToVector4();
                powers[i] = data.DiffusePower;
                hardnesses[i] = data.Hardness;
                specularHardnesses[i] = data.SpecularHardness;
                specularPowers[i] = data.SpecularPower;

                angles[i] = data.DiversionAngle;
                data.Direction.Normalize();
                directions[i] = data.Direction;

                matrices[i] = Matrix.Invert(Extensions.GetAlignmentMatrix(Vector3.Forward, data.Direction));
            }

            parameters["_lightingColor"].SetValue(colors);
            parameters["_diffusePower"].SetValue(powers);
            parameters["_hardness"].SetValue(hardnesses);
            parameters["_specularHardness"].SetValue(specularHardnesses);
            parameters["_specularPower"].SetValue(specularPowers);

            parameters["_diversionAngle"].SetValue(angles);
            parameters["_direction"].SetValue(directions);
            parameters["_directionMatrix"].SetValue(matrices);
        }

        public override EffectTechnique GetTechnique(EffectTechniqueCollection techniques) =>
            techniques["Spotlight"];
    }
}