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

        public override BoundaryBox Boundary => 
            new(Position - new Vector3(Size), Position + new Vector3(Size));

        private TransformShaderState _shaderState;
        public override TransformShaderState ShaderState
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

        public Tile(Level level, float size, Direction excludedDirections, bool hasCeiling = true, bool hasFloor = true) : base(level)
        {
            _sides = new List<Square>();
            var sizemat = Matrix.CreateScale(size, 1f, size);

            if (hasFloor)
                _sides.Add(new Square(level, sizemat, level.Textures.Floor, level.Textures.FloorNormal) { Position = new Vector3(0f, -size, 0f) });
            if (hasCeiling)
                _sides.Add(new Square(level, sizemat, level.Textures.Ceiling, level.Textures.CeilingNormal) { Position = new Vector3(0f, size, 0f) });

            var add = new[]
            {
                new Square(level, sizemat * Matrix.CreateRotationX(MathHelper.PiOver2), level.Textures.Wall, level.Textures.WallNormal) { Position = new Vector3(0f, 0f, -size) },
                new Square(level, sizemat * Matrix.CreateRotationY(MathHelper.PiOver2) * Matrix.CreateRotationZ(MathHelper.PiOver2), level.Textures.Wall, level.Textures.WallNormal) { Position = new Vector3(size, 0f, 0f) },
                new Square(level, sizemat * Matrix.CreateRotationX(MathHelper.PiOver2), level.Textures.Wall, level.Textures.WallNormal) { Position = new Vector3(0f, 0f, size) },
                new Square(level, sizemat * Matrix.CreateRotationY(MathHelper.PiOver2) * Matrix.CreateRotationZ(MathHelper.PiOver2), level.Textures.Wall, level.Textures.WallNormal) { Position = new Vector3(-size, 0f, 0f) },
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
                Position = new Vector3(0f, size / 2f - 0.1f, 0f),
                IsStatic = true
            };
        }

        public override void Update(GameTime time) { }

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
