using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Maze.Graphics.Shaders;

namespace Maze.Graphics
{
    public class SpotLight : Light, IDisposable
    {
        private readonly RenderTarget2D _shadowMap;

        public Vector3 Direction { get; set; }

        public float Reach { get; set; }

        public float DiversionAngle { get; set; }

        public float DiffusePower { get; set; } = 1f;

        public float Hardness { get; set; } = 1f;

        public float SpecularHardness { get; set; } = 1f;

        public float SpecularPower { get; set; } = 1f;


        public SpotLight(Vector3 direction, float reach, float diversionAngle) : base() 
        {
            (Direction, Reach, DiversionAngle) = (direction, reach, diversionAngle);

            _shadowMap = new RenderTarget2D(Maze.Instance.GraphicsDevice, 1024, 1024, false, SurfaceFormat.Single, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.PreserveContents);
        }

        public override Texture2D GetShadows(out Matrix[] lightViewMatrices)
        {
            var up = Vector3.Transform(Vector3.Up, Extensions.GetAlignmentMatrix(Vector3.Forward, Direction));

            var matrix = Matrix.CreateWorld(-Position, Vector3.Forward, Vector3.Up) *
                Matrix.CreateLookAt(Vector3.Zero, Direction, up) *
                Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(90f), 1f, 0.01f, Reach);

            lightViewMatrices = new Matrix[] { matrix };

            var objects = Maze.Instance.Level.Objects.Intersect(new BoundingFrustum(matrix));

            objects.SetShaderState(new WriteDepthShaderState() { LightPosition = Position, WorldViewProjection = matrix });
            objects.Level.Mesh.ShaderState = new WriteDepthInstancedShaderState() { LightPosition = Position, WorldViewProjection = matrix };

            var gd = Maze.Instance.GraphicsDevice;

            gd.SetRenderTarget(_shadowMap);
            gd.Clear(ClearOptions.DepthBuffer | ClearOptions.Target, Color.Black, 1f, 0);

            objects.Draw();
            objects.Level.Mesh.Draw();

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