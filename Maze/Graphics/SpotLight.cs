using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Maze.Graphics.Shaders;
using Maze.Engine;

namespace Maze.Graphics
{
    public class SpotLight : Light
    {
        private RenderTarget2D _shadowMap;
        private Texture2D _bakedShadow;
        private DefferedShaderState _bakedState;

        [Setting("Direction")]
        public Vector3 Direction { get; set; }

        public float DiversionAngle { get; set; }

        public float DiffusePower { get; set; } = 1f;

        public float Hardness { get; set; } = 1f;

        public float SpecularHardness { get; set; } = 1f;

        public float SpecularPower { get; set; } = 1f;

        public SpotLight(Vector3 direction, float reach, float diversionAngle) : base()
        {
            (Direction, Radius, DiversionAngle) = (direction, reach, diversionAngle);

            _shadowMap = new RenderTarget2D(Maze.Instance.GraphicsDevice, 1024, 1024, false, SurfaceFormat.Single, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.PreserveContents);
        }

        public override void LoadStaticState(EnumerableLevelObjects staticObjects)
        {
            _bakedShadow = GetShadows(staticObjects, out _);
            _bakedState = new DefferedShaderState { Color = _bakedShadow };

            _shadowMap = new RenderTarget2D(Maze.Instance.GraphicsDevice, 1024, 1024, false, SurfaceFormat.Single, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.PreserveContents);
        }

        public override Texture2D GetShadows(EnumerableLevelObjects levelObjects, out Matrix[] lightViewMatrices)
        {
            var up = Vector3.Transform(Vector3.Up, Extensions.GetAlignmentMatrix(Vector3.Forward, Direction));

            var matrix = Matrix.CreateWorld(-Position, Vector3.Forward, Vector3.Up) *
                Matrix.CreateLookAt(Vector3.Zero, Direction, up) *
                Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(90f), 1f, 0.01f, Radius);

            lightViewMatrices = new Matrix[] { matrix };

            var objects = levelObjects.Intersect(new BoundingFrustum(matrix)).Evaluate();
            if (objects.Count == 0)
                if (IsStatic)
                    return _bakedShadow;

            objects.SetShaderState(new WriteDepthShaderState() { LightPosition = Position, WorldViewProjection = matrix });
            objects.Level.Mesh.ShaderState = new WriteDepthInstancedShaderState() { LightPosition = Position, WorldViewProjection = matrix };

            var gd = Maze.Instance.GraphicsDevice;

            gd.SetRenderTarget(_shadowMap);
            gd.Clear(ClearOptions.DepthBuffer | ClearOptions.Target, Color.Black, 1f, 0);

            if (_bakedShadow != null)
            {
                Maze.Instance.GraphicsDevice.DepthStencilState = DepthStencilState.None;
                Maze.Instance.DrawQuad(_bakedState);
                Maze.Instance.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            }
            objects.Draw();
            objects.Level.Mesh.Draw();

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