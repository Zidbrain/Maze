using System;
using Microsoft.Xna.Framework;
using Maze.Graphics;
using Microsoft.Xna.Framework.Graphics;

namespace Maze.Graphics.Shaders
{
    public class SSAOShaderState : DefferedShaderState
    {
        private static readonly Vector3[] s_kernels;
        private const int s_kernelsCount = 16;
        private static readonly Texture2D s_noise;

        static SSAOShaderState()
        {
            var random = new Random();

            s_kernels = new Vector3[s_kernelsCount];
            for (int i = 0; i < s_kernelsCount; i++)
            {
                s_kernels[i] = new(
                    (float)random.NextDouble() * 2f - 1f,
                    (float)random.NextDouble() * 2f - 1f,
                    (float)random.NextDouble());
                s_kernels[i].Normalize();
                s_kernels[i] *= (float)random.NextDouble();
                s_kernels[i] *= MathHelper.Lerp(0.1f, 1f, i * i / s_kernelsCount / s_kernelsCount);
            }

            s_noise = new Texture2D(Maze.Instance.GraphicsDevice, 4, 4, false, SurfaceFormat.Vector4);
            var noise = new Vector4[16];
            for (int i = 0; i < 16; i++)
                noise[i] = new Vector4(
                    (float)random.NextDouble() * 2f - 1f,
                    (float)random.NextDouble() * 2f - 1f,
                    0f, 1f);
            s_noise.SetData(noise);
        }

        public SSAOShaderState(Texture2D depth, Texture2D normal, Texture2D position) : base()
        {
            Depth = depth;
            Normal = normal;
            Position = position;
        }

        public float Radius { get; set; } = 0.025f;

        public override void Apply(EffectParameterCollection parameters)
        {
            base.Apply(parameters);

            parameters["_kernels"].SetValue(s_kernels);
            parameters["_SSAORadius"].SetValue(Radius);
            parameters["_noiseTexture"].SetValue(s_noise);
        }

        public override EffectTechnique GetTechnique(EffectTechniqueCollection techniques) =>
            techniques["SSAO"];
    }
}
