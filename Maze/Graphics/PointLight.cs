using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Maze.Graphics.Shaders;
using static Maze.Maze;
using System;

namespace Maze.Graphics
{
    public class PointLight : Light, IDisposable
    {
        private readonly RenderTarget2D _shadowMap = new(Instance.GraphicsDevice, 1024, 1024, false, SurfaceFormat.Single, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.PreserveContents, false, 6);

        private static readonly Matrix[] s_views = new[]
        {
            Matrix.CreateLookAt(Vector3.Zero, Vector3.Right, Vector3.Up), // +X
            Matrix.CreateLookAt(Vector3.Zero, Vector3.Left, Vector3.Up), // -X
            Matrix.CreateScale(-1f, 1f, 1f) * Matrix.CreateLookAt(Vector3.Zero, Vector3.Up, Vector3.Forward), // +Y
            Matrix.CreateScale(-1f, 1f, 1f) * Matrix.CreateLookAt(Vector3.Zero, Vector3.Down, Vector3.Backward), // -Y
            Matrix.CreateLookAt(Vector3.Zero, Vector3.Backward, Vector3.Up), // +Z
            Matrix.CreateLookAt(Vector3.Zero, Vector3.Forward, Vector3.Up), // -Z
        };

        public float Radius { get; set; }

        public float DiffusePower { get; set; } = 1f;

        public float Hardness { get; set; } = 1f;

        public float SpecularHardness { get; set; } = 1f;

        public float SpecularPower { get; set; } = 1f;

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

            objects.SetShaderState(null as IShaderState);
            objects.Level.Mesh.ShaderState = null;

            return _shadowMap;
        }

        public void Dispose()
        {
            _shadowMap.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
