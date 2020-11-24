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

    public class Level : IDrawable, IUpdatable, IDisposable
    {
        private Tile[,] _tiles;
        private Tile _exit;
        private Vector2 _yawpitch;
        private readonly BSPTree _tree;
        private BoundingBox _box;

        public AutoMesh Mesh { get; }

        public LevelObjectCollection Objects { get; }

        public LightEngine LightEngine { get; }

        public Vector3 CameraDirection { get; private set; }
        public Vector3 CameraPosition { get; private set; }
        public Vector3 CameraUp { get; private set; }

        private void UpdateCameraDirection()
        {
            if (Maze.Instance.IsActive)
            {
                var dif = (Mouse.GetState().Position - new Point(Maze.Instance.Window.ClientBounds.Width / 2, Maze.Instance.Window.ClientBounds.Height / 2)).ToVector2() / 750f;
                _yawpitch -= dif;

                var transform = Matrix.CreateFromYawPitchRoll(_yawpitch.X, _yawpitch.Y, 0f);
                CameraDirection = Vector3.Transform(Vector3.Forward, transform);
                CameraUp = Vector3.Transform(Vector3.Up, transform);

                if (_yawpitch.Y >= PiOver2)
                    _yawpitch.Y = PiOver2;
                else if (_yawpitch.Y <= -PiOver2)
                    _yawpitch.Y = -PiOver2;

                Mouse.SetPosition(Maze.Instance.Window.ClientBounds.Width / 2, Maze.Instance.Window.ClientBounds.Height / 2);
            }
        }

        private bool IsOutOfBounds() =>
            _box.Contains(CameraPosition) != ContainmentType.Contains;

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

        public void GenerateMaze(int size)
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

            var random = new Random();
            var side = 2;//random.Next(0, 4);
            var index = 0;//random.Next(0, size);
            var exitIndex = (x: 0, y: 0);
            switch (side)
            {
                case 0:
                    exitIndex = (index, size - 1);
                    break;
                case 1:
                    exitIndex = (size - 1, index);
                    break;
                case 2:
                    exitIndex = (index, 0);
                    break;
                case 3:
                    exitIndex = (0, index);
                    break;
            }
            cells[exitIndex.x, exitIndex.y].removedWall |= (Direction)(1 << side);
            _exit = new Tile(this, 1f, (Direction.Up | Direction.Down | Direction.Left | Direction.Right) ^ (Direction)(1 << side), false)
            {
                Position = new Vector3(exitIndex.x, 0f, -exitIndex.y),
                ShaderState = new Graphics.Shaders.StandartShaderState() { OnlyColor = true },
                DrawToMesh = false,
                EnableCollision = false,
            };
            _exit.Light.DiffusePower = 0f;

            _tiles = new Tile[size, size];
            for (var x = 0; x < size; x++)
                for (var y = 0; y < size; y++)
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

                    _tiles[x, y] = new Tile(this, 1f, direction) { Position = new Vector3(x, 0f, -y) };
                    _tiles[x, y].Light.DiffusePower = 0f;
                }

            _box = new BoundingBox(new Vector3(-0.5f, -0.5f, -size + 0.5f), new Vector3(size - 0.5f, 0.5f, 0.5f));
        }

        public LevelTextures Textures { get; }

        public Level()
        {
            Textures = new LevelTextures();

            Objects = new LevelObjectCollection(this);

            Mesh = new AutoMesh();

            const int size = 10;

            GenerateMaze(size);

            new CollectionInterpolation<Tile>(_tiles.ToIEnumerable<Tile>(),
                (tile, value) =>
                {
                    tile.Light.DiffusePower = value * 5f;
                    tile.Light.SpecularPower = value * 12f;
                },
                tile =>
                    Vector3.Distance(CameraPosition, tile.Position) < 1.5f,
                TimeSpan.FromSeconds(0.25d))
            {
                SkipCondition = tile => !tile.LightEnabled,
                OnEnter = tile => { if (!LightEngine.Lights.Contains(tile.Light)) LightEngine.Lights.Add(tile.Light); },
                OnExit = tile => LightEngine.Lights.Remove(tile.Light)
            }.Start();

            _tree = new BSPTree(_tiles.ToIEnumerable<ICollidable>());

            CameraPosition = new Vector3(0f, -0f, 0f);

            _hookedTime = -1000;

            LightEngine = new LightEngine(this)
            {
                AmbientColor = new Color(new Vector4(new Vector3(0.2f), 1f))
            };

            foreach (var tile in _tiles)
                Objects.Add(tile);
            Objects.Add(_exit);
        }

        public void Draw()
        {
            Objects.SetShaderState(() => new StandartShaderState
            {
                WorldViewProjection = Maze.Instance.Shader.StandartState.WorldViewProjection,
            });
            (_exit.ShaderState as StandartShaderState).OnlyColor = true;

            Objects.Intersect(Maze.Instance.Frustum).Draw();

            Mesh.ShaderState.WorldViewProjection = Maze.Instance.Shader.StandartState.WorldViewProjection;
            Mesh.Draw();

            LightEngine.Draw();
        }

        public bool TraverseAutomatically { get; set; } = false;

        private (int x, int y)? GetTile(Vector2 position)
        {
            var x = position.X + 0.5f;
            if (x >= 0 && x < _tiles.GetLength(0) + 0.5f)
            {
                var y = -(position.Y - 0.5f);
                if (y >= 0 && y < _tiles.GetLength(1) - 0.5f)
                    return ((int)x, (int)y);
            }
            return null;
        }

        private bool ValidateIndexes(int x, int y) =>
            x >= 0 && x < _tiles.GetLength(0) && y >= 0 && y < _tiles.GetLength(1);

        private int _movingDirection = 0;
        private (int x, int y) _oldPosition;
        private (int x, int y) _newPosition;
        private double _hookedTime;

        private void CalculateNewDirection()
        {
            static (int x, int y) GetOffsetDirection(int x, int y, int direction) =>
                direction switch
                {
                    0 => (x, y + 1),
                    1 => (x + 1, y),
                    2 => (x, y - 1),
                    3 => (x - 1, y),
                    _ => throw new ArgumentException($"Wrong {nameof(direction)}"),
                };

            (var x, var y) = _newPosition;

            bool Intersects(int direction)
            {
                if ((_tiles[x, y].ExcludedDirections & (Direction)(1 << direction)) != 0)
                {
                    (var nx, var ny) = GetOffsetDirection(x, y, _movingDirection);

                    if (!ValidateIndexes(nx, ny))
                        return false;

                    direction = (direction + 2) % 4;
                    if ((_tiles[nx, ny].ExcludedDirections & (Direction)(1 << direction)) != 0)
                        return false;
                }
                return true;
            }

            if (Intersects(_movingDirection = (_movingDirection + 1) % 4) &&
                Intersects(_movingDirection = (_movingDirection + 3) % 4) &&
                Intersects(_movingDirection = (_movingDirection + 3) % 4))
                _movingDirection = (_movingDirection + 3) % 4;

            _oldPosition = _newPosition;
            _newPosition = GetOffsetDirection(x, y, _movingDirection);
        }

        public bool LockMovement { get; set; }

        public event EventHandler OutOfBounds;

        private bool _fade;
        public bool Update(GameTime gameTime)
        {
            UpdateCameraDirection();

            if (LockMovement)
                return false;

            if (!TraverseAutomatically)
            {
                UpdateCameraPosition(gameTime);

                if (IsOutOfBounds())
                    OutOfBounds?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                if (gameTime.TotalGameTime.TotalMilliseconds - _hookedTime > 1000d)
                {
                    CalculateNewDirection();

                    if (!ValidateIndexes(_newPosition.x, _newPosition.y))
                        _fade = true;

                    _hookedTime = gameTime.TotalGameTime.TotalMilliseconds;
                }

                var lerp = Vector2.Lerp(new Vector2(_oldPosition.x, _oldPosition.y), new Vector2(_newPosition.x, _newPosition.y), (float)(gameTime.TotalGameTime.TotalMilliseconds - _hookedTime) / 1000f);
                CameraPosition = new Vector3(lerp.X, 0f, -lerp.Y);

                if (_fade)
                {
                    var amount = (float)(gameTime.TotalGameTime.TotalMilliseconds - _hookedTime) / 500f;

                    if (amount > 1f)
                    {
                        OutOfBounds?.Invoke(this, EventArgs.Empty);
                        amount = 1f;
                    }

                    Maze.Instance.FadeAlpha = Lerp(0f, 1f, amount);
                }
            }

            return false;
        }

        void IUpdatable.Begin() { }
        void IUpdatable.End() { }

        public void Dispose()
        {
            _exit.Dispose();
            foreach (var tile in _tiles)
                tile.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
