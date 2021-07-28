using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Maze.Graphics.Shaders;
using static Maze.Maze;
using System;
using Maze.Engine;

namespace Maze.Graphics
{
    public class PointLight : Light
    {
        private readonly RenderTarget2D _shadowMap = new(Instance.GraphicsDevice, 1024, 1024, false, SurfaceFormat.Single, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.PreserveContents, false, 6);
        private RenderTarget2D[] _bakedShadow;

        private static readonly Matrix[] s_views = new[]
        {
            Matrix.CreateLookAt(Vector3.Zero, Vector3.Right, Vector3.Up), // +X
            Matrix.CreateLookAt(Vector3.Zero, Vector3.Left, Vector3.Up), // -X
            Matrix.CreateScale(-1f, 1f, 1f) * Matrix.CreateLookAt(Vector3.Zero, Vector3.Up, Vector3.Forward), // +Y
            Matrix.CreateScale(-1f, 1f, 1f) * Matrix.CreateLookAt(Vector3.Zero, Vector3.Down, Vector3.Backward), // -Y
            Matrix.CreateLookAt(Vector3.Zero, Vector3.Backward, Vector3.Up), // +Z
            Matrix.CreateLookAt(Vector3.Zero, Vector3.Forward, Vector3.Up), // -Z
        };

        public float DiffusePower { get; set; } = 1f;

        public float Hardness { get; set; } = 1f;

        public float SpecularHardness { get; set; } = 1f;

        public float SpecularPower { get; set; } = 1f;

        public override void LoadStaticState(EnumerableLevelObjects staticObjects)
        {
            _bakedShadow = new RenderTarget2D[6];

            var gd = Instance.GraphicsDevice;
            var world = Matrix.CreateWorld(-Position, Vector3.Forward, Vector3.Up);
            var proj = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(91f), 1f, 0.01f, Radius);

            var objects = staticObjects.Intersect(new BoundingSphere(Position, Radius)).Evaluate();

            for (int i =0; i < 6; i++)
            {
                _bakedShadow[i] = new RenderTarget2D(gd, 1024, 1024, false, SurfaceFormat.Single, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.PreserveContents);

                var wvp = world * s_views[i] * proj;

                var sideObjects = objects.Intersect(new BoundingFrustum(wvp)).Evaluate();

                sideObjects.SetShaderState(new WriteDepthShaderState() { WorldViewProjection = wvp, LightPosition = Position });
                objects.Level.Mesh.ShaderState = new WriteDepthInstancedShaderState() { WorldViewProjection = wvp, LightPosition = Position };

                gd.SetRenderTarget(_bakedShadow[i]);
                gd.Clear(ClearOptions.DepthBuffer | ClearOptions.Target, Color.Black, 1f, 0);

                sideObjects.Draw();
                objects.Level.Mesh.Draw();
            }

            objects.SetShaderState(null as IShaderState);
            objects.Level.Mesh.ShaderState = null;
        }

        public override Texture2D GetShadows(EnumerableLevelObjects levelObjects, out Matrix[] lightViewMatrices)
        {
            var gd = Instance.GraphicsDevice;

            lightViewMatrices = new Matrix[6];

            var world = Matrix.CreateWorld(-Position, Vector3.Forward, Vector3.Up);
            var proj = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(91f), 1f, 0.01f, Radius);

            var objects = levelObjects.Intersect(new BoundingSphere(Position, Radius)).Evaluate();

            for (int i = 0; i < 6; i++)
            {
                var wvp = world * s_views[i] * proj;
                lightViewMatrices[i] = wvp;

                var sideObjects = objects.Intersect(new BoundingFrustum(wvp)).Evaluate();

                sideObjects.SetShaderState(new WriteDepthShaderState() { WorldViewProjection = wvp, LightPosition = Position });
                sideObjects.Level.Mesh.ShaderState = new WriteDepthInstancedShaderState() { WorldViewProjection = wvp , LightPosition = Position };

                gd.SetRenderTarget(_shadowMap, i);
                gd.Clear(ClearOptions.DepthBuffer | ClearOptions.Target, Color.Black, 1f, 0);

                Instance.DrawQuad(new DefferedShaderState { Color = _bakedShadow[i] });
                sideObjects.Draw();
                sideObjects.Level.Mesh.Draw();
            }

            objects.SetShaderState(null as IShaderState);
            objects.Level.Mesh.ShaderState = null;

            return _shadowMap;
        }

        public override void Dispose()
        {
            _shadowMap.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
