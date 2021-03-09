using System;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Maze.Graphics;
using Maze.Graphics.Shaders;

namespace Maze.Engine
{
    [Flags]
    public enum Direction
    {
        None = 0,
        Up = 1 << 0,
        Right = 1 << 1,
        Down = 1 << 2,
        Left = 1 << 3
    }

    public class Tile : CollideableLevelObject, IDisposable
    {
        private readonly List<Square> _sides;
        private Vector3 _position;

        public float Size { get; private set; }
        public override Vector3 Position
        {
            get => _position;
            set
            {
                var delta = value - _position;
                foreach (var side in _sides)
                    side.Position += delta;

                Light.Position += delta;

                _position = value;
            }
        }

        private Color _color;
        public Color Color
        {
            get => _color;
            set
            {
                _color = value;
                foreach (var side in _sides)
                    side.Color = _color;
            }
        }

        public bool LightEnabled { get; set; } = true;

        public override IEnumerable<Polygon> Polygones
        {
            get
            {
                var ret = new List<Polygon>();
                foreach (var side in _sides)
                    ret.AddRange(side.Polygones);
                return ret;
            }
        }

        private IShaderState _shaderState;
        public override IShaderState ShaderState
        {
            get => _shaderState;
            set
            {
                _shaderState = value;
                foreach (var side in _sides)
                    side.ShaderState = value;
            }
        }

        public PointLight Light { get; }

        public Direction ExcludedDirections { get; }

        public Tile(Level level, float size, Direction excludedDirections, bool hasCeiling = true) : base(level)
        {
            _sides = hasCeiling ? new List<Square>()
            {
                new Square(Matrix.Identity, level.Textures.Floor, level.Textures.FloorNormal) { Position = new Vector3(0f, -size / 2f, 0f), Size = new Vector2(size) },
                new Square(Matrix.Identity, level.Textures.Ceiling, level.Textures.CeilingNormal) { Position = new Vector3(0f, size / 2f, 0f), Size = new Vector2(size) },
            } : new List<Square>();

            var add = new[]
            {
                new Square(Matrix.CreateRotationX(MathHelper.PiOver2), level.Textures.Wall, level.Textures.WallNormal) { Position = new Vector3(0f, 0f, -size / 2f), Size = new Vector2(size) },
                new Square(Matrix.CreateRotationY(MathHelper.PiOver2) * Matrix.CreateRotationZ(MathHelper.PiOver2), level.Textures.Wall, level.Textures.WallNormal) { Position = new Vector3(size / 2f, 0f, 0f), Size = new Vector2(size) },
                new Square(Matrix.CreateRotationX(MathHelper.PiOver2), level.Textures.Wall, level.Textures.WallNormal) { Position = new Vector3(0f, 0f, size / 2f), Size = new Vector2(size) },
                new Square(Matrix.CreateRotationY(MathHelper.PiOver2) * Matrix.CreateRotationZ(MathHelper.PiOver2), level.Textures.Wall, level.Textures.WallNormal) { Position = new Vector3(-size / 2f, 0f, 0f), Size = new Vector2(size) },
            };

            for (var i = 0; i < 4; i++)
                if (!excludedDirections.HasFlag((Direction)(1 << i)))
                    _sides.Add(add[i]);

            ExcludedDirections = excludedDirections;

            Size = size;

            Light = new PointLight()
            {
                DiffusePower = 5,
                Radius = 1f,
                SpecularHardness = 600,
                SpecularPower = 12,
                Hardness = 1.5f,
                Position = new Vector3(0f, size / 2f - 0.1f, 0f)
            };
        }

        public override void Update(GameTime time) { }

        public override bool Intersects(BoundingFrustum frustum)
        {
            var intersection = frustum.Contains(new BoundingBox(Position - new Vector3(Size / 2f), Position + new Vector3(Size / 2f)));
            return intersection == ContainmentType.Contains || intersection == ContainmentType.Intersects;
        }

        public override bool Intersects(in BoundingSphere sphere)
        {
            var intersection = sphere.Contains(new BoundingBox(Position - new Vector3(Size / 2f), Position + new Vector3(Size / 2f)));
            return intersection == ContainmentType.Contains || intersection == ContainmentType.Intersects;
        }

        public override void Draw()
        {
            foreach (var side in _sides)
                side.Draw();
        }

        public override void Draw(AutoMesh mesh)
        {
            foreach (var side in _sides)
                side.Draw(mesh);
        }

        public void Dispose()
        {
            Light.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
