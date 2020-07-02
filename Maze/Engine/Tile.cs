using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace Maze.Engine
{
    [Flags]
    public enum Direction
    {
        None = -1,
        Up = 1 << 0,
        Right = 1 << 1,
        Down = 1 << 2,
        Left = 1 << 3
    }

    public class Tile : IDrawable, ICollidable
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

        public Tile(float size, Direction excludedDirections)
        {
            var wall = Maze.Game.Content.Load<Texture2D>("Wall");
            var floor = Maze.Game.Content.Load<Texture2D>("Floor");
            var ceiling = Maze.Game.Content.Load<Texture2D>("Ceiling");

            _sides = new List<Square>()
            {
                new Square(Matrix.Identity, floor) { Position = new Vector3(0f, -size / 2f, 0f), Size = new Vector2(size) },
                new Square(Matrix.Identity, ceiling) { Position = new Vector3(0f, size / 2f, 0f), Size = new Vector2(size) },
            };

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

            Size = size;
        }

        public void Draw()
        {
            foreach (var side in _sides)
                side.Draw();
        }
    }
}
