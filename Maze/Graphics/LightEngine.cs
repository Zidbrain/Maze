using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Maze.Graphics.Shaders;
using static Maze.Maze;
using Maze.Engine;

namespace Maze.Graphics
{
    public abstract class Light
    {
        public virtual Color Color { get; set; }

        public virtual Vector3 Position { get; set; }

        public bool ShadowsEnabled { get; set; } = true;

        public abstract PointLightShaderData GetData();

        public abstract Texture2D GetShadows(out Matrix[] lightViewMatrices);
    }

    public class LightEngine : IDrawable
    {
        private readonly GammaShaderState _gamma;
        private readonly PointLightShaderState _lighting;
        private readonly ShadowMapShaderState _shadowState;
        private readonly RenderTarget2D _shadowMaps;

        private readonly Tile _box;

        public List<Light> Lights { get; } = new List<Light>();

        public const int LightBatchCount = 5;

        public Color AmbientColor
        {
            get => _gamma.MaskColor;
            set => _gamma.MaskColor = value;
        }

        public LightEngine(Level level)
        {
            _shadowMaps = new RenderTarget2D(Instance.GraphicsDevice, 1920, 1080, false, SurfaceFormat.Single, DepthFormat.None, 0, RenderTargetUsage.PreserveContents, false, LightBatchCount);

            _gamma = new GammaShaderState(Instance.RenderTargets.Color);
            _lighting = new PointLightShaderState(Instance.RenderTargets.United, Instance.RenderTargets.Normal, Instance.RenderTargets.Position) { ShadowMaps = _shadowMaps };
            _shadowState = new ShadowMapShaderState(Instance.RenderTargets.Position);

            _box = new Tile(level, 0.1f, Direction.None)
            {
                LightEnabled = false,
                DrawToMesh = false,
            };
        }

        public void Draw()
        {
            _lighting.CameraPosition = Instance.Level.CameraPosition;

            Instance.GraphicsDevice.SetRenderTarget(Instance.RenderTargets.United);
            Instance.GraphicsDevice.DepthStencilState = DepthStencilState.None;
            Instance.DrawQuad(_gamma);
            Instance.GraphicsDevice.DepthStencilState = DepthStencilState.Default;

            _box.Position = Instance.Level.CameraPosition;
            Instance.Level.Objects.Add(_box);

            var depthMaps = new Texture2D[Lights.Count];
            var matrices = new Matrix[Lights.Count][];
            for (int i = 0; i < Lights.Count; i++)
                if (Lights[i].ShadowsEnabled)
                    depthMaps[i] = Lights[i].GetShadows(out matrices[i]);

            Instance.GraphicsDevice.DepthStencilState = DepthStencilState.None;

            _shadowState.Position = Instance.RenderTargets.Position;

            for (var i = 0; i <= Lights.Count / LightBatchCount; i++)
            {
                _lighting.LightingData.Clear();
                for (var j = i * LightBatchCount; j < i * LightBatchCount + 5 && j < Lights.Count; j++)
                {
                    var data = Lights[j].GetData();
                    _lighting.LightingData.Add(data);

                    _shadowState.LightPosition = data.LightPosition;
                    _shadowState.DepthMap = depthMaps[j];
                    _shadowState.LightViewMatrices = matrices[j];

                    Instance.GraphicsDevice.SetRenderTarget(_shadowMaps, j - i * LightBatchCount);

                    if (Lights[j].ShadowsEnabled)
                        Instance.DrawQuad(_shadowState);
                }

                Instance.GraphicsDevice.SetRenderTarget(Instance.RenderTargets.United);
                Instance.DrawQuad(_lighting);
            }

            Instance.Level.Objects.Remove(_box);
        }
    }

    public class PointLight : Light
    {
        private readonly PointLightShaderData _data = new PointLightShaderData();
        private readonly RenderTarget2D _shadowMap = new RenderTarget2D(Instance.GraphicsDevice, 1024, 1024, false, SurfaceFormat.Single, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.PreserveContents, false, 6);

        private static readonly Matrix[] s_views = new[]
        {
            Matrix.CreateLookAt(Vector3.Zero, Vector3.Right, Vector3.Up), // +X
            Matrix.CreateLookAt(Vector3.Zero, Vector3.Left, Vector3.Up), // -X
            Matrix.CreateScale(-1f, 1f, 1f) * Matrix.CreateLookAt(Vector3.Zero, Vector3.Up, Vector3.Forward), // +Y
            Matrix.CreateScale(-1f, 1f, 1f) * Matrix.CreateLookAt(Vector3.Zero, Vector3.Down, Vector3.Backward), // -Y
            Matrix.CreateLookAt(Vector3.Zero, Vector3.Backward, Vector3.Up), // +Z
            Matrix.CreateLookAt(Vector3.Zero, Vector3.Forward, Vector3.Up), // -Z
        };

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

        public override PointLightShaderData GetData() =>
            _data;

        public override Texture2D GetShadows(out Matrix[] lightViewMatrices)
        {
            var gd = Instance.GraphicsDevice;

            lightViewMatrices = new Matrix[6];

            var world = Matrix.CreateWorld(-Position, Vector3.Forward, Vector3.Up);
            var proj = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(91f), 1f, 0.01f, Radius);

            var objects = Instance.Level.Objects.Intersect(new BoundingSphere(Position, Radius));

            for (int i = 0; i < 6; i++)
            {
                var wvp = world * s_views[i] * proj;
                lightViewMatrices[i] = wvp;

                var sideObjects = objects.Intersect(new BoundingFrustum(wvp));

                sideObjects.SetShaderState(new WriteDepthShaderState() { WorldViewProjection = wvp, LightPosition = Position });
                objects.Level.Mesh.ShaderState = new WriteDepthInstancedShaderState() { WorldViewProjection = wvp , LightPosition = Position };

                gd.SetRenderTarget(_shadowMap, i);
                gd.Clear(ClearOptions.DepthBuffer | ClearOptions.Target, Color.Black, 1f, 0);

                sideObjects.Draw();
                objects.Level.Mesh.Draw();
            }

            objects.SetShaderState(null);
            objects.Level.Mesh.ShaderState = null;

            return _shadowMap;
        }
    }
}
