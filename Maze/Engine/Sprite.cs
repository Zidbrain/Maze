using Maze.Graphics;
using Maze.Graphics.Shaders;
using Microsoft.Xna.Framework;
using System;

namespace Maze.Engine
{
    public class Sprite : LevelObject
    {
        private readonly Square _square;

        public override ShaderState ShaderState { get => _square.ShaderState; set => _square.ShaderState = value; }

        public Vector2 Size { get => _square.Size; set => _square.Size = value; }


        public Sprite(Level level, Vector2 size) : base(level)
        {
            _square = new Square(Matrix.Identity, level.Textures.Ceiling, null);

            Size = size;
        }

        public override void Draw() =>
            _square.Draw();

        public override void Draw(AutoMesh mesh) => _square.Draw(mesh);

        public override bool Intersects(BoundingFrustum frustum)
        {
            var step = _square.Transform.Left * Size.X / 2f + _square.Transform.Forward * Size.Y / 2f;
            return frustum.Intersects(BoundingBox.CreateFromPoints(new Vector3[] { Position + step, Position, Position - step }));
        }
        public override bool Intersects(in BoundingSphere sphere)
        {
            var step = _square.Transform.Left * Size.X / 2f+ _square.Transform.Forward * Size.Y / 2f;
            return sphere.Intersects(BoundingBox.CreateFromPoints(new Vector3[] { Position + step, Position, Position - step }));
        }
        public override void Update(GameTime time) => 
            _square.Transform = Matrix.CreateWorld(Position, Level.CameraUp, Level.CameraDirection);
    }
}