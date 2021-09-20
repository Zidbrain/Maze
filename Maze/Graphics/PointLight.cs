using Maze.Engine;
using Maze.Graphics.Shaders;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using static Maze.Maze;

namespace Maze.Graphics
{
    public class PointLight : Light
    {
        private readonly RenderTargetCube _shadowMap;
        private readonly PointLightShadowMapShaderState _shaderState;
        private RenderTarget2D[] _bakedShadow;
        private readonly DefferedShaderState _bakedState;

        private static readonly Matrix[] s_views = new[]
        {
            Matrix.CreateLookAt(Vector3.Zero, Vector3.Right, Vector3.Up) * Matrix.CreateScale(-1f, 1f, 1f), // +X
            Matrix.CreateLookAt(Vector3.Zero, Vector3.Left, Vector3.Up) * Matrix.CreateScale(-1f, 1f, 1f), // -X
            Matrix.CreateLookAt(Vector3.Zero, Vector3.Up, Vector3.Forward) * Matrix.CreateScale(-1f, 1f, 1f), // +Y
            Matrix.CreateLookAt(Vector3.Zero, Vector3.Down, Vector3.Backward) * Matrix.CreateScale(-1f, 1f, 1f), // -Y
            Matrix.CreateLookAt(Vector3.Zero, Vector3.Backward, Vector3.Up) * Matrix.CreateScale(-1f, 1f, 1f), // +Z
            Matrix.CreateLookAt(Vector3.Zero, Vector3.Forward, Vector3.Up) * Matrix.CreateScale(-1f, 1f, 1f), // -Z
        };

        public float DiffusePower { get; set; } = 1f;

        public float Hardness { get; set; } = 1f;

        public float SpecularHardness { get; set; } = 1f;

        public float SpecularPower { get; set; } = 1f;

        public PointLight()
        {
            _shadowMap = new(Instance.GraphicsDevice, 1024, false, SurfaceFormat.Single, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.PreserveContents);
            _shaderState = new(Instance.RenderTargets.Position) { DepthMap = _shadowMap };
            _bakedState = new DefferedShaderState();
        }

        public override void LoadStaticState(EnumerableLevelObjects staticObjects)
        {
            _bakedShadow = new RenderTarget2D[6];

            var gd = Instance.GraphicsDevice;
            var world = Matrix.CreateWorld(-Position, Vector3.Forward, Vector3.Up);
            var proj = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(90f), 1f, 0.01f, Radius);

            var prev = gd.RasterizerState;
            gd.RasterizerState = shadowRasterizer;

            var objects = staticObjects.Intersect(new BoundingSphere(Position, Radius)).Evaluate();

            for (int i = 0; i < 6; i++)
            {
                _bakedShadow[i] = new RenderTarget2D(gd, 1024, 1024, false, SurfaceFormat.Single, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.PreserveContents);

                var wvp = world * s_views[i] * proj;

                var sideObjects = objects.Intersect(new BoundingFrustum(wvp)).Evaluate();

                sideObjects.SetShaderState(new WriteDepthShaderState() { WorldViewProjection = wvp });
                objects.Level.Mesh.ShaderState = new WriteDepthInstancedShaderState() { WorldViewProjection = wvp };

                gd.SetRenderTarget(_bakedShadow[i]);
                gd.Clear(ClearOptions.DepthBuffer | ClearOptions.Target, Color.Red, 1f, 0);

                sideObjects.Draw();
                objects.Level.Mesh.Draw();
            }

            objects.SetShaderState(null as TransformShaderState);
            objects.Level.Mesh.ShaderState = null;

            gd.RasterizerState = prev;
        }

        public override PointLightShadowMapShaderState GetShadows(EnumerableLevelObjects levelObjects)
        {
            var gd = Instance.GraphicsDevice;

            var world = Matrix.CreateWorld(-Position, Vector3.Forward, Vector3.Up);
            var proj = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(90f), 1f, 0.01f, Radius);

            _shaderState.NearDist = 0.01f;
            _shaderState.FarDist = Radius;
            _shaderState.LightPosition = Position;

            var prev = gd.RasterizerState;
            gd.RasterizerState = shadowRasterizer;

            var objects = levelObjects.Intersect(new BoundingSphere(Position, Radius)).Evaluate();

            for (int i = 0; i < 6; i++)
            {
                var wvp = world * s_views[i] * proj;

                var sideObjects = objects.Intersect(new BoundingFrustum(wvp)).Evaluate();

                sideObjects.SetShaderState(new WriteDepthShaderState() { WorldViewProjection = wvp });
                sideObjects.Level.Mesh.ShaderState = new WriteDepthInstancedShaderState() { WorldViewProjection = wvp };

                gd.SetRenderTarget(_shadowMap, (CubeMapFace)i);
                gd.Clear(ClearOptions.DepthBuffer | ClearOptions.Target, Color.Red, 1f, 0);

                if (IsStatic)
                {
                    gd.DepthStencilState = DepthStencilState.None;
                    _bakedState.Color = _bakedShadow[i];
                    Instance.DrawQuad(_bakedState, false);
                    gd.DepthStencilState = DepthStencilState.Default;
                }

                sideObjects.Draw();
                sideObjects.Level.Mesh.Draw();
            }

            objects.SetShaderState(null as TransformShaderState);
            objects.Level.Mesh.ShaderState = null;

            gd.RasterizerState = prev;

            _shaderState.DepthMap = _shadowMap;

            return _shaderState;
        }

        public override void Dispose()
        {
            _shadowMap.Dispose();
            if (_bakedShadow != null)
                foreach (var obj in _bakedShadow)
                    obj.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
