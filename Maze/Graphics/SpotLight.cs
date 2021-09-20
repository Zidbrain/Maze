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
        private SpotLightShadowMapShaderState _shaderState;

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
            _shaderState = new(Maze.Instance.RenderTargets.Position, Maze.Instance.RenderTargets.Normal) { DepthMap = _shadowMap };
            _bakedShadow = GetShadows(staticObjects).DepthMap as Texture2D;
            _bakedState = new DefferedShaderState { Color = _bakedShadow };

            _shadowMap = new RenderTarget2D(Maze.Instance.GraphicsDevice, 1024, 1024, false, SurfaceFormat.Single, DepthFormat.Depth24Stencil8, 0, RenderTargetUsage.PreserveContents);
        }

        public override SpotLightShadowMapShaderState GetShadows(EnumerableLevelObjects levelObjects)
        {
            var up = Vector3.Transform(Vector3.Up, VectorMath.GetAlignmentMatrix(Vector3.Forward, Direction));

            var matrix = Matrix.CreateWorld(-Position, Vector3.Forward, Vector3.Up) *
                Matrix.CreateLookAt(Vector3.Zero, Direction, up) *
                Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(90f), 1f, 0.1f, Radius);

            var objects = levelObjects.Intersect(new BoundingFrustum(matrix)).Evaluate();
            if (objects.Count == 0)
                if (IsStatic)
                {
                    _shaderState.DepthMap = _bakedShadow;
                    return _shaderState;
                }

            objects.SetShaderState(new WriteDepthShaderState() { WorldViewProjection = matrix });
            objects.Level.Mesh.ShaderState = new WriteDepthInstancedShaderState() { WorldViewProjection = matrix };

            var gd = Maze.Instance.GraphicsDevice;
            var prev = gd.RasterizerState;
            gd.RasterizerState = shadowRasterizer;

            gd.SetRenderTarget(_shadowMap);
            gd.Clear(ClearOptions.DepthBuffer | ClearOptions.Target, Color.Red, 1f, 0);

            if (_bakedShadow != null)
            {
                Maze.Instance.GraphicsDevice.DepthStencilState = DepthStencilState.None;
                Maze.Instance.DrawQuad(_bakedState, false);
                Maze.Instance.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            }
            objects.Draw();
            objects.Level.Mesh.Draw();

            objects.SetShaderState(null as TransformShaderState);
            objects.Level.Mesh.ShaderState = null;

            gd.RasterizerState = prev;

            _shaderState.DepthMap = _shadowMap;
            _shaderState.LightView = matrix;
            _shaderState.SpotLight = this;
            _shaderState.CameraPosition = Maze.Instance.Level.Player.Position;

            return _shaderState;
        }

        public override void Dispose()
        {
            _shadowMap.Dispose();
            _bakedShadow.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}