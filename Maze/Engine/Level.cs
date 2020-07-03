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
        private readonly Tile[,] _tiles;
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
                var times = 0;
                for (var i = 0; i < polygones.Count && times < 3; i++)
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

                            vector = endPoint - CameraPosition;

                            skip = i;
                            i = -1;
                            times++;
                        }
                    }
                }

                CameraPosition += vector;
            }
        }

        public Tile[,] GenerateMaze(int size)
        {
            var cells = new (bool visited, Direction removedWall)[size, size];

            (int x, int y)? CheckForFree(int x, int y, Direction direction)
            {
                switch (direction)
                {
                    case Direction.Right:
                        if (x + 1 < size && !cells[x + 1, y].visited)
                            return (x + 1, y);
                        break;
                    case Direction.Left:
                        if (x - 1 >= 0 && !cells[x - 1, y].visited)
                            return (x - 1, y);
                        break;
                    case Direction.Up:
                        if (y + 1 < size && !cells[x, y + 1].visited)
                            return (x, y + 1);
                        break;
                    case Direction.Down:
                        if (y - 1 >= 0 && !cells[x, y - 1].visited)
                            return (x, y - 1);
                        break;
                }
                return null;
            }

            var stack = new Stack<(int x, int y, bool visited)>();
            stack.Push((0, 0, true));

            while (stack.Count != 0)
            {
                var (x, y, visited) = stack.Pop();

                var directions = new int[] { 0, 1, 2, 3 };
                directions.Shuffle();

                foreach (var dirInt in directions)
                {
                    var direction = (Direction)(1 << dirInt);

                    var free = CheckForFree(x, y, direction);
                    if (free != null)
                    {
                        cells[free.Value.x, free.Value.y] = (true, cells[free.Value.x, free.Value.y].removedWall | (Direction)(1 << ((dirInt + 2) % 4)));
                        cells[x, y] = (visited, direction | cells[x, y].removedWall);

                        stack.Push((x, y, true));
                        stack.Push((free.Value.x, free.Value.y, true));
                        break;
                    }
                }
            }

            var tileGrid = new Tile[size, size];
            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                {
                    Direction direction;
                    if (x == 0 && y == 0)
                        direction = Direction.None;
                    else if (y == 0)
                        direction = Direction.Left;
                    else if (x == 0)
                        direction = Direction.Down;
                    else direction = Direction.Left | Direction.Down;

                    direction |= cells[x, y].removedWall;

                    tileGrid[x, y] = new Tile(1f, direction) { Position = new Vector3(x, 0f, -y) };
                }

            return tileGrid;
        }

        public Level()
        {
            _tiles = GenerateMaze(10);

            _tree = new BSPTree(_tiles.ToIEnumerable<ICollidable>());

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
