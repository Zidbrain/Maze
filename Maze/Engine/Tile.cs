using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

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

        public Direction ExcludedDirections { get; }

        public Tile(float size, Direction excludedDirections, bool hasCeiling = true)
        {
            var wall = Maze.Game.Content.Load<Texture2D>("Textures/Wall");
            var floor = Maze.Game.Content.Load<Texture2D>("Textures/Floor");
            var ceiling = Maze.Game.Content.Load<Texture2D>("Textures/Ceiling");

            _sides = hasCeiling ? new List<Square>()
            {
                new Square(Matrix.Identity, floor) { Position = new Vector3(0f, -size / 2f, 0f), Size = new Vector2(size) },
                new Square(Matrix.Identity, ceiling) { Position = new Vector3(0f, size / 2f, 0f), Size = new Vector2(size) },
            } : new List<Square>();

            var add = new[]
            {
                new Square(Matrix.CreateRotationX(MathHelper.PiOver2), wall) { Position = new Vector3(0f, 0f, -size / 2f), Size = new Vector2(size) },
                new Square(Matrix.CreateRotationY(MathHelper.PiOver2) * Matrix.CreateRotationZ(MathHelper.PiOver2), wall) { Position = new Vector3(size / 2f, 0f, 0f), Size = new Vector2(size) },
                new Square(Matrix.CreateRotationX(MathHelper.PiOver2), wall) { Position = new Vector3(0f, 0f, size / 2f), Size = new Vector2(size) },
                new Square(Matrix.CreateRotationY(MathHelper.PiOver2) * Matrix.CreateRotationZ(MathHelper.PiOver2), wall) { Position = new Vector3(-size / 2f, 0f, 0f), Size = new Vector2(size) },
            };

            for (var i = 0; i < 4; i++)
                if (!excludedDirections.HasFlag((Direction)(1 << i)))
                    _sides.Add(add[i]);

            ExcludedDirections = excludedDirections;

            Size = size;
        }

        public void Draw()
        {
            var intersection = Maze.Game.Frustum.Contains(new BoundingBox(Position - new Vector3(Size / 2f), Position + new Vector3(Size / 2f)));

            if (intersection == ContainmentType.Contains || intersection == ContainmentType.Intersects)
                foreach (var side in _sides)
                    side.Draw();
        }

        public void Dispose()
        {
            foreach (var side in _sides)
                side.Dispose();
        }
    }
}
