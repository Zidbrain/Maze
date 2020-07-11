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

        public GameTime GameTime { get; private set; }

        private double _hookedFade;
        private bool _fadeOut;
        public float FadeAlpha { get; set; }

        public Vector2 ScreenSize =>
            new Vector2(Window.ClientBounds.Width, Window.ClientBounds.Height);

        public Maze()
        {
            _graphics = new GraphicsDeviceManager(this)
            {
                HardwareModeSwitch = false,
                PreferMultiSampling = true,
                SynchronizeWithVerticalRetrace = false,
                GraphicsProfile = GraphicsProfile.HiDef,
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

            Level = new Level() { LockMovement = true };
            Level.OutOfBounds += GenerateNewLevel;
            
            void GenerateNewLevel(object sender, System.EventArgs e)
            {
                _showMessage = true;
                Level.Dispose();
                Level = new Level() { LockMovement = true };
                Level.OutOfBounds += GenerateNewLevel;

                _fadeOut = true;
                _hookedFade = GameTime.TotalGameTime.TotalMilliseconds;
            }

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

        private bool _showMessage = true;

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            GameTime = gameTime;

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

            if (_showMessage && Keyboard.GetState().GetPressedKeyCount() != 0)
            {
                Level.LockMovement = false;
                _showMessage = false;
            }

            Level.Update(gameTime);

            if (Input.PressedOnce(Keys.F))
                _graphics.ToggleFullScreen();

            Input.Update();

            Shader.StandartState.Matrix = Matrix.CreateLookAt(Level.CameraPosition, Level.CameraPosition + Level.CameraDirection, Level.CameraUp) *
                    Matrix.CreatePerspectiveFieldOfView(ToRadians(60f), ScreenSize.X / ScreenSize.Y, 0.01f, 7.3f);
            Frustum = new BoundingFrustum(Shader.StandartState.Matrix);

            if (_fadeOut)
            {
                var time = (float)(gameTime.TotalGameTime.TotalMilliseconds - _hookedFade) / 500f;
                if (time > 1)
                {
                    time = 1;
                    _fadeOut = false;
                }

                FadeAlpha = Lerp(1f, 0f, time);
            }
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

            if (_showMessage)
            {
                text = "Press any key to start";
                _spriteBatch.DrawString(Font, text, new Vector2(1920f / 2f - Font.MeasureString(text).X * 1.5f, 1080f / 2f - Font.MeasureString(text).Y * 1.5f),
                    Color.White, 0f, Vector2.Zero, 3f, SpriteEffects.None, 0f);
            }
            
            _spriteBatch.End();

            GraphicsDevice.SetRenderTarget(null);

            _spriteBatch.Begin(blendState: BlendState.NonPremultiplied);
            _spriteBatch.Draw(RenderTargets.United, new Rectangle(0, 0, Window.ClientBounds.Width, Window.ClientBounds.Height), Color.White);
            _spriteBatch.Draw(Extensions.Sample, new Rectangle(0, 0, Window.ClientBounds.Width, Window.ClientBounds.Height), new Color(new Vector4(1f, 1f, 1f, FadeAlpha))); 
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
