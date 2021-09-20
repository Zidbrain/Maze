using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using static Microsoft.Xna.Framework.MathHelper;
using System.Collections.Generic;
using Maze.Graphics;
using Maze.Graphics.Shaders;

namespace Maze.Engine
{
    public sealed class LevelTextures
    {
        public Texture2D Wall { get; }

        public Texture2D Floor { get; }

        public Texture2D Ceiling { get; }

        public Texture2D WallNormal { get; }

        public Texture2D FloorNormal { get; }

        public Texture2D CeilingNormal { get; }

        public LevelTextures()
        {
            Wall = Maze.Instance.Content.Load<Texture2D>("Textures/Wall");
            Floor = Maze.Instance.Content.Load<Texture2D>("Textures/Floor");
            Ceiling = Maze.Instance.Content.Load<Texture2D>("Textures/Ceiling");

            WallNormal = Maze.Instance.Content.Load<Texture2D>("Textures/Wall-normal");
            FloorNormal = Maze.Instance.Content.Load<Texture2D>("Textures/Floor-normal");
            CeilingNormal = Maze.Instance.Content.Load<Texture2D>("Textures/Ceiling-normal");
        }

        public (Texture2D texture, Texture2D normal)[] GetArray()
        {
            var ret = new (Texture2D texture, Texture2D normal)[3];

            ret[0] = (Wall, WallNormal);
            ret[1] = (Floor, FloorNormal);
            ret[2] = (Ceiling, CeilingNormal);

            return ret;
        }
    }

    public class Level : IDrawable, IUpdateable, IDisposable
    {
        private readonly Tile[,,] _tiles;

        public BSPTree BSPTree { get; }

        public AutoMesh Mesh { get; }

        public LevelObjectCollection Objects { get; }

        public LightEngine LightEngine { get; }

        public Player Player { get; }

        public LevelTextures Textures { get; }

        private readonly SpotLight _spotlight;
        private readonly List<IUpdateable> _remove;

        public Level()
        {
            Maze.Instance.SettingsManager.Subscribe(this);
            Textures = new LevelTextures();

            Objects = new LevelObjectCollection(this);

            Mesh = new AutoMesh();

            _tiles = new Tile[3, 3, 2];
            for (int z = 0; z < 2; z++)
                for (int x = 0; x < 3; x++)
                    for (int y = 0; y < 3; y++)
                    {
                        var excluded = Direction.Down | Direction.Left | Direction.Right | Direction.Up;

                        if (x == 0)
                            excluded ^= Direction.Left;
                        if (y == 0)
                            excluded ^= Direction.Up;
                        if (x == 2)
                            excluded ^= Direction.Right;
                        if (y == 2)
                            excluded ^= Direction.Down;

                        _tiles[x, y, z] = new Tile(this, 2f, excluded, z != 0, z != 1)
                        {
                            Position = new Vector3(x * 4f, z * 4f, y * 4f),
                            LightEnabled = false,
                            IsStatic = true
                        };
                    }

            var boundary = new BoundaryBox(new Vector3(-2f), new Vector3(10f, 6f, 10f));
            var edge = new Square(this, VectorMath.Construct(new Vector3(4f, 1f, 4f), new Vector3(PiOver4, 0f, PiOver4), new Vector3(boundary.Center.X, 1f, boundary.Center.Z)),
                Textures.Floor, Textures.FloorNormal)
            { IsStatic = true };

            _remove = new List<IUpdateable>
            {
                new CollectionInterpolation<Tile>(_tiles.ToIEnumerable<Tile>(),
                (tile, value) =>
                {
                    tile.Light.DiffusePower = value * 5f;
                    tile.Light.SpecularPower = value * 12f;
                },
                tile =>
                    Vector3.Distance(Player.Position, tile.Position) < 1.5f,
                TimeSpan.FromSeconds(0.25d))
                {
                    SkipCondition = tile => !tile.LightEnabled,
                    OnEnter = tile => { if (!LightEngine.Lights.Contains(tile.Light)) LightEngine.Lights.Add(tile.Light); },
                    OnExit = tile => LightEngine.Lights.Remove(tile.Light)
                }.Start(),
            };

            foreach (var tile in _tiles)
                Objects.Add(tile);
            Objects.Add(edge);
           // Objects.Add(new ModelObject(this, @"Arm.gltf"));

            BSPTree = new BSPTree(new ICollideable[] { new CollisionObject(boundary), edge }, boundary, 6);

            LightEngine = new LightEngine(this)
            {
                AmbientColor = new Color(new Vector4(new Vector3(0.5f), 1f))
            };
            LightEngine.Lights.Add(new PointLight() { Radius = 10f, Position = boundary.Center, IsStatic = true, DiffusePower = 2f, Hardness = 2f, SpecularPower = 3, SpecularHardness = 50 });

            _spotlight = new SpotLight(Vector3.Forward, 5f, ToRadians(35f))
            {
                DiffusePower = 10,
                SpecularHardness = 200,
                SpecularPower = 6,
                Hardness = 1f,
                Color = Color.White,
                Position = new Vector3(0f, 0f, 2f),
                IsStatic = true,
            };
            LightEngine.Lights.Add(_spotlight);

            Player = new(this) { Position = new Vector3(0f, boundary.Min.Y - Player.Hitbox.Min.Y, 0f) };

            // CustomInterpolation<SpotLight>.Start(_spotlight, static (spotlight, t) => spotlight.Direction = new Vector3(MathF.Cos(t * MathF.PI * 2), 0f, MathF.Sin(t * MathF.PI * 2)), TimeSpan.FromSeconds(3d), RepeatOptions.Jump);

            //Maze.Instance.SettingsManager.Subscribe(_spotlight);
        }

        private bool _initializedLights;

        public void Draw()
        {
            if (!_initializedLights)
            {
                var obj = Objects.Static().Evaluate();
                foreach (var light in LightEngine.Lights)
                   light.LoadStaticState(obj);
                if (_spotlight.IsStatic)
                    _spotlight.LoadStaticState(obj);

                Maze.Instance.GraphicsDevice.SetRenderTargets(Maze.Instance.RenderTargets);

                _initializedLights = true;
            }

            var intersects = Objects.Intersect(Maze.Instance.Frustum).Evaluate();
            foreach (var obj in intersects)
                obj.ShaderState = new StandartShaderState() { WorldViewProjection = Maze.Instance.Shader.StandartState.WorldViewProjection };

            intersects.Draw();

            Mesh.ShaderState.WorldViewProjection = Maze.Instance.Shader.StandartState.WorldViewProjection;
            Mesh.Draw();

            LightEngine.Draw();

           // Maze.Instance.Present();
        }

        public bool LockMovement { get; set; }

        public bool Update(GameTime gameTime)
        {
            Player.Update(gameTime);
            Objects.Update(gameTime);

            if (LockMovement)
                return false;

            return false;
        }

        void IUpdateable.Begin() { }
        void IUpdateable.End()
        {
            foreach (var remove in _remove)
                Maze.Instance.UpdateableManager.Remove(remove);
        }

        public void Dispose()
        {
            foreach (var tile in _tiles)
                tile.Dispose();

            _spotlight.Dispose();

            LightEngine.Dispose();

            Maze.Instance.SettingsManager.Unsubscribe(this);

            GC.SuppressFinalize(this);
        }
    }
}
