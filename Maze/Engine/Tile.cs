using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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

    public class Tile : IDrawable, ICollidable, IDisposable
    {
        private readonly List<Square> _sides;
        private Vector3 _position;

        public float Size { get; private set; }
        public Vector3 Position
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

        public IEnumerable<Polygon> Polygones
        {
            get
            {
                var ret = new List<Polygon>();
                foreach (var side in _sides)
                    ret.AddRange(side.Polygones);
                return ret;
            }
        }

        private ShaderState _shaderState;
        public ShaderState ShaderState
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

        public Tile(Level level, float size, Direction excludedDirections, bool hasCeiling = true)
        {
            _sides = hasCeiling ? new List<Square>()
            {
                new Square(Matrix.Identity, level.Textures.Floor) { Position = new Vector3(0f, -size / 2f, 0f), Size = new Vector2(size), Normal = level.Textures.FloorNormal },
                new Square(Matrix.Identity, level.Textures.Ceiling) { Position = new Vector3(0f, size / 2f, 0f), Size = new Vector2(size), Normal = level.Textures.CeilingNormal },
            } : new List<Square>();

            var add = new[]
            {
                new Square(Matrix.CreateRotationX(MathHelper.PiOver2), level.Textures.Wall) { Position = new Vector3(0f, 0f, -size / 2f), Size = new Vector2(size), Normal = level.Textures.WallNormal },
                new Square(Matrix.CreateRotationY(MathHelper.PiOver2) * Matrix.CreateRotationZ(MathHelper.PiOver2), level.Textures.Wall) { Position = new Vector3(size / 2f, 0f, 0f), Size = new Vector2(size), Normal = level.Textures.WallNormal },
                new Square(Matrix.CreateRotationX(MathHelper.PiOver2), level.Textures.Wall) { Position = new Vector3(0f, 0f, size / 2f), Size = new Vector2(size), Normal = level.Textures.WallNormal },
                new Square(Matrix.CreateRotationY(MathHelper.PiOver2) * Matrix.CreateRotationZ(MathHelper.PiOver2), level.Textures.Wall) { Position = new Vector3(-size / 2f, 0f, 0f), Size = new Vector2(size), Normal = level.Textures.WallNormal },
            };

            for (var i = 0; i < 4; i++)
                if (!excludedDirections.HasFlag((Direction)(1 << i)))
                    _sides.Add(add[i]);

            ExcludedDirections = excludedDirections;

            Size = size;

            Light = new PointLight()
            {
                DiffusePower = 3,
                Radius = 1f,
                SpecularHardness = 550,
                SpecularPower = 10,
                Position = new Vector3(0f, size / 2f - 0.1f, 0f)
            };
        }

        public void Draw()
        {
            var intersection = Maze.Instance.Frustum.Contains(new BoundingBox(Position - new Vector3(Size / 2f), Position + new Vector3(Size / 2f)));

            if (intersection == ContainmentType.Contains || intersection == ContainmentType.Intersects)
                foreach (var side in _sides)
                    side.Draw();
        }

        public void Draw(LevelMesh mesh)
        {
            var intersection = Maze.Instance.Frustum.Contains(new BoundingBox(Position - new Vector3(Size / 2f), Position + new Vector3(Size / 2f)));

            if (intersection == ContainmentType.Contains || intersection == ContainmentType.Intersects)
                foreach (var side in _sides)
                    side.Draw(mesh);
        }

        public void Dispose()
        {
            foreach (var side in _sides)
                side.Dispose();
        }
    }
}
