using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Maze.Engine;
using Maze.Graphics;
using Maze.Graphics.Shaders;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
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

        public UpdateableManager UpdateableManager { get; private set; }

        public GameTime GameTime { get; private set; }

        private FogShaderState _fogState;

        public Vector2 ScreenSize =>
            new(Window.ClientBounds.Width, Window.ClientBounds.Height);

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

            GameTime = new GameTime();

            UpdateableManager = new UpdateableManager();

            SettingsManager = new SettingsManager();
            UpdateableManager.Add(SettingsManager);

            GraphicsDevice.DiscardColor = Color.Transparent;

            Content.RootDirectory = "Content";
            Shader = new Shader();

            DrawableQueue = new Queue<IDrawable>();

            Input = new InputController();

            Font = Content.Load<SpriteFont>("Font");
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            Mouse.SetPosition(Window.ClientBounds.Width / 2, Window.ClientBounds.Height / 2);

            RenderTargets = new RenderTargets();

            Level = new Level() { LockMovement = true };
            UpdateableManager.Add(Level);

            _vertexBuffer = new VertexBuffer(GraphicsDevice, typeof(VertexPositionTexture), 6, BufferUsage.WriteOnly);
            _vertexBuffer.SetData(new[]
            {
                new VertexPositionTexture(new Vector3(-1,1,1), Vector2.Zero),
                new VertexPositionTexture(new Vector3(1,-1, 1), Vector2.One),
                new VertexPositionTexture(new Vector3(-1,-1,1), new Vector2(0f, 1f)),
                new VertexPositionTexture(new Vector3(1,-1, 1), Vector2.One),
                new VertexPositionTexture(new Vector3(-1,1,1), Vector2.Zero),
                new VertexPositionTexture(new Vector3(1,1,1), new Vector2(1f,0f))
            });

            _fogState = new FogShaderState(RenderTargets.Position)
            {
                FogStart = 13f,
                FogEnd = 15f,
                FogColor = Color.CornflowerBlue,
            };
        }


        public SettingsManager SettingsManager { get; private set; }

        private bool _showMessage = true;

        public void Present()
        {
            var rt = GraphicsDevice.GetRenderTargets();
            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.SetRenderTargets(rt);
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            GameTime = gameTime;

            GraphicsDevice.BlendState = BlendState.AlphaBlend;
            GraphicsDevice.RasterizerState = new RasterizerState()
            {
                CullMode = CullMode.None,
                MultiSampleAntiAlias = true,
                DepthBias = 0f,
                DepthClipEnable = false,
                SlopeScaleDepthBias = 0f
            };
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            //   GraphicsDevice.SamplerStates[0] = SamplerState.AnisotropicClamp;

            if (Input.Pressed(Keys.Escape))
                Exit();

            if (_showMessage && Keyboard.GetState().GetPressedKeyCount() != 0)
            {
                Level.LockMovement = false;
                _showMessage = false;
            }

            UpdateableManager.Update(gameTime);

            // LightEngine.Lights[0].Position = Level.CameraPosition;

            if (Input.ChangedDown(Keys.F))
                _graphics.ToggleFullScreen();

            Input.Update();

            Shader.StandartState.WorldViewProjection = Matrix.CreateLookAt(Level.Player.Position, Level.Player.Position + Level.Player.CameraDirection, Level.Player.CameraUp) *
                    Matrix.CreatePerspectiveFieldOfView(ToRadians(90f), ScreenSize.X / ScreenSize.Y, 0.01f, 15f);
            Frustum = new BoundingFrustum(Shader.StandartState.WorldViewProjection);

            _fogState.CameraPosition = Level.Player.Position;
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.SetRenderTargets(RenderTargets);
            GraphicsDevice.Clear(Color.Transparent);
            GraphicsDevice.SetRenderTarget(RenderTargets.Depth);
            GraphicsDevice.Clear(Color.Red);
            GraphicsDevice.SetRenderTargets(RenderTargets);

            DrawableQueue.Enqueue(Level);

            while (DrawableQueue.Count > 0)
                DrawableQueue.Dequeue().Draw();

            GraphicsDevice.BlendState = new BlendState() { ColorDestinationBlend = Blend.InverseSourceAlpha, ColorSourceBlend = Blend.SourceAlpha, AlphaBlendFunction = BlendFunction.Add };

            DrawQuad(_fogState, false);
            //DrawQuad(new GammaShaderState(RenderTargets.United) { Gamma = 1f }, false);

            _spriteBatch.Begin(blendState: BlendState.AlphaBlend);

            var text = $"Position {Level.Player.Position}\n" +
            $"Camera Direction {Level.Player.CameraDirection}";
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

            _spriteBatch.Begin(blendState: BlendState.Opaque);
            _spriteBatch.Draw(RenderTargets.United, new Rectangle(0, 0, Window.ClientBounds.Width, Window.ClientBounds.Height), Color.White);
            _spriteBatch.End();

            base.Draw(gameTime);
        }

        public void DrawVertexes(VertexBuffer buffer, IShaderState shaderState, int start = 0, int primitiveCount = 2)
        {
            if (shaderState is null)
                Shader.State = Shader.StandartState;
            else
                Shader.State = shaderState;

            if (shaderState is TransformShaderState transformShader)
                transformShader.Bones = null;

            GraphicsDevice.SetVertexBuffer(buffer);
            Shader.Apply();
            GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, start, primitiveCount);
        }

        public void DrawQuad(IShaderState state, bool present)
        {
            DrawVertexes(_vertexBuffer, state);

            if (present)
            {
                var targets = GraphicsDevice.GetRenderTargets();
                GraphicsDevice.SetRenderTarget(null);
                GraphicsDevice.SetRenderTargets(targets);
            }
        }

        public static Maze Instance { get; } = new Maze();

        public static void Main() => Instance.Run();
    }
}