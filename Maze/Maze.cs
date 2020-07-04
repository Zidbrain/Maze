using Maze.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using static Microsoft.Xna.Framework.MathHelper;

namespace Maze
{
    public class Maze : Game
    {
        private readonly GraphicsDeviceManager _graphics;

        public InputController Input { get; private set; }
        public Shader Shader { get; private set; }

        public Queue<IDrawable> DrawableQueue { get; private set; }
        public BoundingFrustum Frustum { get; private set; }

        public SpriteFont Font { get; private set; }

        private SpriteBatch _spriteBatch;

        private VertexBuffer _vertexBuffer;

        public Level Level { get; private set; }

        public RenderTargets RenderTargets { get; private set; }

        public Vector2 ScreenSize =>
            new Vector2(Window.ClientBounds.Width, Window.ClientBounds.Height);

        public Maze()
        {
            _graphics = new GraphicsDeviceManager(this)
            {
                HardwareModeSwitch = false,
                PreferMultiSampling = true,
                SynchronizeWithVerticalRetrace = false,
                GraphicsProfile = GraphicsProfile.HiDef
            };
            IsFixedTimeStep = false;
        }

        protected override void LoadContent()
        {
            base.LoadContent();

            Content.RootDirectory = "Content";
            Shader = new Shader();

            DrawableQueue = new Queue<IDrawable>();

            Input = new InputController();

            Font = Content.Load<SpriteFont>("Font");
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            Mouse.SetPosition(Window.ClientBounds.Width / 2, Window.ClientBounds.Height / 2);

            Level = new Level();

            RenderTargets = new RenderTargets();
            _vertexBuffer = new VertexBuffer(GraphicsDevice, typeof(VertexPositionTexture), 6, BufferUsage.WriteOnly);
            _vertexBuffer.SetData(new[]
            {
                new VertexPositionTexture(new Vector3(-1,1,1), Vector2.Zero),
                new VertexPositionTexture(new Vector3(-1,-1,1), new Vector2(0f, 1f)),
                new VertexPositionTexture(new Vector3(1,-1, 1), Vector2.One),
                new VertexPositionTexture(new Vector3(1,-1, 1), Vector2.One),
                new VertexPositionTexture(new Vector3(-1,1,1), Vector2.Zero),
                new VertexPositionTexture(new Vector3(1,1,1), new Vector2(1f,0f))
            });
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            GraphicsDevice.BlendState = BlendState.AlphaBlend;
            GraphicsDevice.RasterizerState = new RasterizerState()
            {
                CullMode = CullMode.None,
                MultiSampleAntiAlias = true,
            };
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.SamplerStates[0] = SamplerState.AnisotropicClamp;
            GraphicsDevice.PresentationParameters.MultiSampleCount = 0;

            if (Input.Pressed(Keys.Escape))
                Exit();

            Level.Update(gameTime);

            if (Input.PressedOnce(Keys.F))
                _graphics.ToggleFullScreen();

            Input.Update();

            Shader.StandartState.Matrix = Matrix.CreateLookAt(Level.CameraPosition, Level.CameraPosition + Level.CameraDirection, Level.CameraUp) *
                    Matrix.CreatePerspectiveFieldOfView(ToRadians(60f), ScreenSize.X / ScreenSize.Y, 0.01f, 7.3f);
            Frustum = new BoundingFrustum(Shader.StandartState.Matrix);
        }


        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.SetRenderTargets(RenderTargets);

            GraphicsDevice.Clear(Color.CornflowerBlue);

            DrawableQueue.Enqueue(Level);

            while (DrawableQueue.Count > 0)
                DrawableQueue.Dequeue().Draw();

            GraphicsDevice.SetRenderTarget(RenderTargets.United);

            DrawVertexes(_vertexBuffer, Matrix.Identity, shaderState: new RasterizeShaderState(RenderTargets));

            _spriteBatch.Begin(blendState: BlendState.AlphaBlend);
            var text = $"Position X:{Level.CameraPosition.X} Y:{Level.CameraPosition.Y} Z:{Level.CameraPosition.Z}\n" +
            $"Camera Direction X:{Level.CameraDirection.X} Y:{Level.CameraDirection.Y} Z:{Level.CameraDirection.Z}";
            _spriteBatch.DrawString(Font, text, new Vector2(0f, 1080) - new Vector2(0f, Font.MeasureString(text).Y), Color.White);
            _spriteBatch.DrawString(Font, (1000f / gameTime.ElapsedGameTime.TotalMilliseconds).ToString(), Vector2.Zero, Color.White);
            _spriteBatch.End();

            GraphicsDevice.SetRenderTarget(null);

            _spriteBatch.Begin();
            _spriteBatch.Draw(RenderTargets.United, new Rectangle(0, 0, Window.ClientBounds.Width, Window.ClientBounds.Height), Color.White);
            _spriteBatch.End();

            base.Draw(gameTime);
        }

        public void DrawVertexes(VertexBuffer buffer, in Matrix transform, int start = 0, int primitiveCount = 2, ShaderState shaderState = null)
        {
            if (shaderState is null)
            {
                Shader.StandartState.Transform = transform;
                Shader.State = Shader.StandartState;
            }
            else
            {
                if (shaderState is StandartShaderState standartState)
                {
                    standartState.Matrix = Shader.StandartState.Matrix;
                    standartState.Transform = transform;
                }
                Shader.State = shaderState;
            }

            GraphicsDevice.SetVertexBuffer(buffer);
            Shader.Apply();
            GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, start, primitiveCount);
        }

        public static Maze Game { get; } = new Maze();

        public static void Main() =>
            Game.Run();
    }
}
