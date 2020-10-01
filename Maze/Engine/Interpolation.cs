using Microsoft.Xna.Framework;
using System;

namespace Maze.Engine
{
    public enum RepeatOptions
    {
        None,
        Jump,
        Cycle
    }

    public class Interpolation : IUpdatable
    {
        private double _hook;
        private readonly double _totalMilliseconds;
        private bool _stop;
        private bool _setValue;

        public float From { get; private set; }
        public float To { get; private set; }

        public TimeSpan Time { get; }

        public float Value { get; private set; }

        public bool Active { get; private set; }

        public bool Reverse { get; set; }


        public RepeatOptions RepeatOptions { get; set; }

        public static Interpolation Start(float from, float to, TimeSpan time, RepeatOptions repeatOptions = RepeatOptions.None)
        {
            var ret = new Interpolation(from, to, time, repeatOptions);
            ret.Start();
            return ret;
        }

        public void Start()
        {
            if (!Active)
                Maze.Instance.UpdatableManager.Add(this);
            else _hook = Maze.Instance.GameTime.TotalGameTime.TotalMilliseconds;
        }

        public void Stop(bool setValue = true) =>
            (_stop, _setValue) = (true, setValue);

        public Interpolation(float from, float to, TimeSpan time, RepeatOptions repeatOptions = RepeatOptions.None) =>
            (From, To, Time, _totalMilliseconds, RepeatOptions) = (from, to, time, time.TotalMilliseconds, repeatOptions);

        public event EventHandler Started;
        public event EventHandler Updated;
        public event EventHandler Stopped;

        void IUpdatable.Begin()
        {
            _hook = Maze.Instance.GameTime.TotalGameTime.TotalMilliseconds;
            _stop = false;
            _setValue = true;

            Value = From;

            Active = true;

            Started?.Invoke(this, EventArgs.Empty);
        }

        void IUpdatable.End()
        {
            if (_setValue)
                Value = To;

            switch (RepeatOptions)
            {
                case RepeatOptions.None:
                    if (_setValue)
                        Stopped?.Invoke(this, EventArgs.Empty);
                    Active = false;
                    break;
                case RepeatOptions.Cycle:
                    (From, To) = (To, From);
                    goto case RepeatOptions.Jump;
                case RepeatOptions.Jump:
                    Start();
                    break;
            }
        }

        bool IUpdatable.Update(GameTime time)
        {
            if (_stop)
                return true;

            var factor = (float)((time.TotalGameTime.TotalMilliseconds - _hook) / _totalMilliseconds);

            if (factor >= 1f)
                return true;

            Value = MathHelper.Lerp(From, To, Reverse ? 1f - factor : factor);

            Updated?.Invoke(this, EventArgs.Empty);

            return false;
        }
    }
}
