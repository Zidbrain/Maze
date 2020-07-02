using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using static Microsoft.Xna.Framework.MathHelper;
using System.Collections.Generic;

namespace Maze.Engine
{
    public class Level : IDrawable
    {
        private readonly Tile[] _tiles;
        private Vector2 _yawpitch;
        private readonly BSPTree _tree;

        public Vector3 CameraDirection { get; private set; }
        public Vector3 CameraPosition { get; private set; }
        public Vector3 CameraUp { get; private set; }

        private void UpdateCameraDirection()
        {
            if (Maze.Game.IsActive)
            {
                var dif = (Mouse.GetState().Position - new Point(Maze.Game.Window.ClientBounds.Width / 2, Maze.Game.Window.ClientBounds.Height / 2)).ToVector2() / 750f;
                _yawpitch -= dif;

                var transform = Matrix.CreateFromYawPitchRoll(_yawpitch.X, _yawpitch.Y, 0f);
                CameraDirection = Vector3.Transform(Vector3.Forward, transform);
                CameraUp = Vector3.Transform(Vector3.Up, transform);

                if (_yawpitch.Y >= PiOver2)
                    _yawpitch.Y = PiOver2;
                else if (_yawpitch.Y <= -PiOver2)
                    _yawpitch.Y = -PiOver2;

                Mouse.SetPosition(Maze.Game.Window.ClientBounds.Width / 2, Maze.Game.Window.ClientBounds.Height / 2);
            }
        }

        private void UpdateCameraPosition(GameTime gameTime)
        {
            var boost = Keyboard.GetState().IsKeyDown(Keys.LeftShift);

            var speed = 0.05f * (float)gameTime.ElapsedGameTime.TotalMilliseconds / (1000f / 60f);

            var forward = CameraDirection.XZ();
            var right = Vector3.Transform(Vector3.Forward, Matrix.CreateFromYawPitchRoll(_yawpitch.X - PiOver2, 0f, 0f)).XZ();

            var vector = Vector3.Zero;
            foreach (var key in Keyboard.GetState().GetPressedKeys())
            {
                switch (key)
                {
                    case Keys.W:
                        vector += forward;
                        break;
                    case Keys.S:
                        vector += -forward;
                        break;
                    case Keys.D:
                        vector += right;
                        break;
                    case Keys.A:
                        vector += -right;
                        break;

                }
            }

            if (vector != Vector3.Zero)
            {
                var mult = speed * (boost ? 2f : 1f);
                vector.Normalize();
                vector *= mult;

                var polygones = _tree.Collides(new BoundingSphere(CameraPosition, mult + Polygon.Range));

                var skip = -1;
                for (var i = 0; i < polygones.Count;i++)
                {
                    if (i == skip)
                        continue;

                    var vectorLength = vector.Length();
                    var polygon = polygones[i];
                    var collision = Extensions.IntersectionDistance(new Ray(CameraPosition, vector), polygon.A, polygon.Plane.Normal);

                    if (collision != null)
                    {
                        var rangePoint = CameraPosition + vector * collision.Value - Vector3.Normalize(vector) * Polygon.Range / Math.Abs(Vector3.Dot(polygon.Plane.Normal, vector) / vectorLength);

                        if (vectorLength > (rangePoint - CameraPosition).Length() &&
                           (Extensions.IsInsideTriangle(polygon[0], polygon[1], polygon[2], rangePoint) || Extensions.Distance(polygon, rangePoint) <= Polygon.Range * MathF.Sqrt(2f)))
                        {
                            var endPoint = CameraPosition + vector + Extensions.SignedDistance(CameraPosition + vector, rangePoint, polygon.Plane.Normal) * polygon.Plane.Normal;

                            if (vector == endPoint - CameraPosition)
                                break;

                            vector = endPoint - CameraPosition;

                            skip = i;
                            i = -1;
                        }
                    }
                }

                CameraPosition += vector;
            }
        }

        public Level()
        {
            _tiles = new Tile[]
            {
                 new Tile(1f, Direction.Down),
                 new Tile(1f, Direction.Up | Direction.Down | Direction.Right),
                 new Tile(1f, Direction.Up),
                 new Tile(1f, Direction.Left)
            };

            for (var i = 0; i < _tiles.Length - 1; i++)
                _tiles[i].Position = new Vector3(0f, 0f, -1f + 1f * i);
            _tiles[3].Position = new Vector3(1f, 0f, 0f);

            _tree = new BSPTree(_tiles);

            CameraPosition = new Vector3(0f, -0f, 0f);
        }

        public void Draw()
        {
            foreach (var tile in _tiles)
                tile.Draw();
        }

        public void Update(GameTime gameTime)
        {
            UpdateCameraDirection();
            UpdateCameraPosition(gameTime);
        }
    }
}
