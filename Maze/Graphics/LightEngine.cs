using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Maze.Graphics.Shaders;
using static Maze.Maze;

namespace Maze.Graphics
{
    public abstract class Light
    {
        public virtual Color Color { get; set; }

        public virtual Vector3 Position { get; set; }

        public abstract LightingShaderData GetData();
    }

    public class LightEngine : IDrawable
    {
        private readonly GammaShaderState _gamma;
        private readonly LightingShaderState _lighting;

        public List<Light> Lights { get; } = new List<Light>();

        public const int LightBatchCount = 5;

        public Color AmbientColor
        {
            get => _gamma.MaskColor;
            set => _gamma.MaskColor = value;
        }

        public LightEngine()
        {
            _gamma = new GammaShaderState(Instance.RenderTargets.Color);
            _lighting = new LightingShaderState(Instance.RenderTargets.United, Instance.RenderTargets.Normal, Instance.RenderTargets.Position);
        }

        public void Draw()
        {
            Instance.GraphicsDevice.SetRenderTarget(Instance.RenderTargets.United);
            Instance.GraphicsDevice.DepthStencilState = DepthStencilState.None;
            Instance.DrawQuad(_gamma);

            for (var i = 0; i <= Lights.Count / LightBatchCount; i++)
            {
                _lighting.LightingData.Clear();
                for (var j = i * LightBatchCount; j < i * LightBatchCount + 5 && j < Lights.Count; j++)
                        _lighting.LightingData.Add(Lights[j].GetData());

                Instance.DrawQuad(_lighting);
            }
        }
    }

    public class PointLight : Light
    {
        private readonly LightingShaderData _data = new LightingShaderData();

        public float Radius
        {
            get => _data.Radius;
            set => _data.Radius = value;
        }

        public float DiffusePower
        {
            get => _data.DiffusePower;
            set => _data.DiffusePower = value;
        }

        public float Hardness
        {
            get => _data.Hardness;
            set => _data.Hardness = value;
        }

        public float SpecularHardness
        {
            get => _data.SpecularHardness;
            set => _data.SpecularHardness = value;
        }

        public float SpecularPower
        {
            get => _data.SpecularPower;
            set => _data.SpecularPower = value;
        }

        public override Color Color
        {
            get => _data.LightColor;
            set => _data.LightColor = value;
        }

        public override Vector3 Position
        {
            get => _data.LightPosition;
            set => _data.LightPosition = value;
        }

        public override LightingShaderData GetData() =>
            _data;
    }
}
