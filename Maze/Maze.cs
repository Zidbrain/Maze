﻿using Maze.Engine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Maze.Graphics;
using System.Collections.Generic;
using static Microsoft.Xna.Framework.MathHelper;
using Maze.Graphics.Shaders;
using System;

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

        private double _hookedFade;
        private bool _fadeOut;
        public float FadeAlpha { get; set; }

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
            Level.OutOfBounds += GenerateNewLevel;

            void GenerateNewLevel(object sender, EventArgs e)
            {
                UpdateableManager.Remove(Level);
                Level.Dispose();

                _showMessage = true;
                Level = new Level() { LockMovement = true, };
                Level.OutOfBounds += GenerateNewLevel;

                UpdateableManager.Add(Level);

                _fadeOut = true;
                _hookedFade = GameTime.TotalGameTime.TotalMilliseconds;
            }
            UpdateableManager.Add(Level);

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

            _fogState = new FogShaderState(RenderTargets.United, RenderTargets.Position)
            {
                FogStart = 2.5f,
                FogEnd = 5f,
                FogColor = Color.CornflowerBlue,
            };
        }

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
            GraphicsDevice.SamplerStates[0] = SamplerState.AnisotropicClamp;

            if (Input.Pressed(Keys.Escape))
                Exit();

            if (_showMessage && Keyboard.GetState().GetPressedKeyCount() != 0)
            {
                Level.LockMovement = false;
                _showMessage = false;
            }

            UpdateableManager.Update(gameTime);

            // LightEngine.Lights[0].Position = Level.CameraPosition;

            if (Input.PressedOnce(Keys.F))
                _graphics.ToggleFullScreen();

            Input.Update();

            Shader.StandartState.WorldViewProjection = Matrix.CreateLookAt(Level.CameraPosition, Level.CameraPosition + Level.CameraDirection, Level.CameraUp) *
                    Matrix.CreatePerspectiveFieldOfView(ToRadians(60f), ScreenSize.X / ScreenSize.Y, 0.01f, 5f);
            Frustum = new BoundingFrustum(Shader.StandartState.WorldViewProjection);

            _fogState.CameraPosition = Level.CameraPosition;

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

            GraphicsDevice.Clear(Color.Transparent);

            DrawableQueue.Enqueue(Level);

            while (DrawableQueue.Count > 0)
                DrawableQueue.Dequeue().Draw();

            DrawQuad(_fogState);
            DrawQuad(new GammaShaderState(RenderTargets.United) { Gamma = 1.6f });

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

        public void DrawVertexes(VertexBuffer buffer, IShaderState shaderState, int start = 0, int primitiveCount = 2)
        {
            if (shaderState is null)
                Shader.State = Shader.StandartState;
            else
                Shader.State = shaderState;

            GraphicsDevice.SetVertexBuffer(buffer);
            Shader.Apply();
            GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, start, primitiveCount);
        }

        public void DrawQuad(IShaderState state)
        {
            DrawVertexes(_vertexBuffer, state);

            var targets = GraphicsDevice.GetRenderTargets();
            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.SetRenderTargets(targets);
        }

        public static Maze Instance { get; } = new Maze();

        public static void Main() =>
            Instance.Run();
    }
}
