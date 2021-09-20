using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using static Microsoft.Xna.Framework.MathHelper;
using static Maze.VectorMath;
using System;

namespace Maze.Engine
{
    public class Player : Entity
    {
        private ModelObject _model;

        public static readonly BoundingBox Hitbox = new(
            new(-0.05f, -1.7f, -0.05f),
            new(0.05f, 0.1f, 0.05f));

        private const float s_jumpHeight = 3f;
        private const float s_walkSpeed = 8f;

        public override BoundaryBox Boundary =>
            new(Position + Hitbox.Min, Position + Hitbox.Max);

        public Vector3 CameraDirection { get; private set; } = Vector3.Forward;
        public Vector3 CameraUp { get; private set; } = Vector3.Up;

        public Player(Level level) : base(level)
        {
            ConditionalInterpolation<Player>.Start(this, static (player, t) => player._yawpitchroll.Z = -ToRadians(5f) * t,
                static player => player._startLeftAnim, TimeSpan.FromSeconds(0.1d));

            ConditionalInterpolation<Player>.Start(this, static (player, t) => player._yawpitchroll.Z = ToRadians(5f) * t,
                static player => player._startRightAnim, TimeSpan.FromSeconds(0.1d));

            _model = new ModelObject(level, "Arm.glTF");
            level.Objects.Add(_model);
        }

        public Vector3 Velocity { get; set; }
        public bool OnGround { get; private set; }

        public override void Draw() { }

        private Vector3 _yawpitchroll;
        private void UpdateCameraDirection()
        {
            var dif = (Mouse.GetState().Position - new Point(Maze.Instance.Window.ClientBounds.Width / 2, Maze.Instance.Window.ClientBounds.Height / 2)).ToVector2() / 750f;
            Mouse.SetPosition(Maze.Instance.Window.ClientBounds.Width / 2, Maze.Instance.Window.ClientBounds.Height / 2);
            dif += GamePad.GetState(0).ThumbSticks.Right.MinusY() / 70f;
            if (dif != Vector2.Zero)
            {
                _yawpitchroll -= new Vector3(dif, 0f);

                if (_yawpitchroll.Y >= PiOver2)
                    _yawpitchroll.Y = PiOver2;
                else if (_yawpitchroll.Y <= -PiOver2)
                    _yawpitchroll.Y = -PiOver2;
            }

            var transform = Matrix.CreateFromYawPitchRoll(_yawpitchroll.X, _yawpitchroll.Y, _yawpitchroll.Z);
            CameraDirection = Vector3.Transform(Vector3.Forward, transform);
            CameraUp = Vector3.Transform(Vector3.Up, transform);
        }

        private bool _startLeftAnim, _startRightAnim, _startBoostAnim;
        private float _boostSpeed;
        private Vector3 _boostDir;

        private void UpdateVelocity()
        {
            var speed = s_walkSpeed;

            Vector3 forward;
            if (_yawpitchroll.Y == -PiOver2)
                forward = CameraUp;
            else if (_yawpitchroll.Y == PiOver2)
                forward = -CameraUp;
            else
                forward = CameraDirection.XZ();
            forward.Normalize();
            var right = Vector3.Cross(CameraDirection, CameraUp).XZ();
            right.Normalize();

            var vector = Vector3.Zero;
            var camera = 0;
            if (!_startBoostAnim)
            {
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
                            camera--;
                            break;
                        case Keys.A:
                            vector += -right;
                            camera++;
                            break;
                    }
                }

                var gamepadDir = GamePad.GetState(0).ThumbSticks.Left;
                vector += forward * gamepadDir.Y + right * gamepadDir.X;
            }

            if (OnGround)
                Velocity = new Vector3(0f, Velocity.Y, 0f);

            if (Maze.Instance.Input.ChangedDown(Keys.Space) && OnGround)
                Velocity += new Vector3(0f, MathF.Sqrt(2 * s_jumpHeight * 9.8f), 0f);

            if (Maze.Instance.Input.ChangedDown(Keys.LeftShift) && !_startBoostAnim)
            {
                _startBoostAnim = true;

                if (vector == Vector3.Zero)
                    vector += forward;

                _boostDir = vector;

                CustomInterpolation<Player>.Start(this, static (player, t) => player._boostSpeed = 1f + 2 * (1f - t), TimeSpan.FromSeconds(0.25d)).Stopped +=
                    (sender, e) => _startBoostAnim = false;
            }

            if (_startBoostAnim)
            {
                vector = _boostDir;
                speed *= _boostSpeed;
            }

            _startLeftAnim = false;
            _startRightAnim = false;
            if (camera == 1)
                _startRightAnim = true;
            else if (camera == -1)
                _startLeftAnim = true;

            if (vector != Vector3.Zero)
            {
                vector.Normalize();
                vector *= speed;
            }

            Velocity += vector;

            var dir = Velocity.XZ();
            var len = dir.Length();

            if (len > speed)
            {
                dir *= speed / len;
                Velocity = new Vector3(dir.X, Velocity.Y, dir.Z);
            }
        }

        private Vector3 CheckForCollisions(Vector3 vector)
        {
            if (vector != Vector3.Zero)
                OnGround = false;

            var intersections = Level.BSPTree.Intersects(new(Boundary.Center, ((Boundary.Max - Boundary.Min) / 2f + Abs(vector)).Length()));

            if (intersections.Count == 0)
                return vector;

            var box = Boundary;
            var center = box.Center;
            for (int i = 0; i < 8; i++)
            {
                if (!box.Contains(box[i] + vector))
                    foreach (var obj in intersections)
                    {
                        var result = obj.Boundary.Intersects(new Ray(box[i], vector));
                        if (result is null)
                            continue;

                        (var distance, var plane) = result.Value;
                        if (distance < 1f && distance >= 0f &&
                            (distance != 0f || MathF.Abs(plane.Distance(center)) >= MathF.Abs(plane.Distance(center + vector))))
                        {
                            vector = (box[i] + vector).Project(plane) - box[i];

                            if (vector.Y <= 0f && MathF.Abs(plane.DotNormal(Vector3.Up)) >= Sqrt2Over2)
                                OnGround = true;
                        }
                    }
            }

            return vector;
        }

        private void UpdateModel()
        {
           // _model.Nodes[0].Transform = Matrix.CreateRotationY(Pi) * GetAlignmentMatrix(Vector3.Backward, CameraDirection)  * Matrix.CreateTranslation(Position + CameraDirection * 2);
        }

        public override void Update(GameTime time)
        {
            if (!Maze.Instance.IsActive)
                return;

            UpdateCameraDirection();
            UpdateVelocity();

            var at = new Vector3(0f, -9.8f * time.GetFrameTimestep(), 0f);

            var x = Velocity * time.GetFrameTimestep() + at * time.GetFrameTimestep() / 2f;
            var newx = CheckForCollisions(x);

            Velocity += at;
            if (time.GetFrameTimestep() != 0)
                Velocity += (newx - x) / time.GetFrameTimestep();

            Position += newx;

            UpdateModel();
        }
    }
}
