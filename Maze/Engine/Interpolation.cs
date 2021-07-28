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

    public class Interpolation : IUpdateable
    {
        private double _hook;
        private readonly double _totalMilliseconds;
        private bool _stop;
        private bool _setValue;

        protected float From { get; set; }
        protected float To { get; set; }

        public TimeSpan Time { get; }

        public float Value { get; private set; }

        public bool Active { get; protected set; }

        public bool Reverse { get; set; }


        public RepeatOptions RepeatOptions { get; set; }

        public static Interpolation Start(TimeSpan time, RepeatOptions repeatOptions = RepeatOptions.None)
        {
            var ret = new Interpolation(time, repeatOptions);
            return ret.Start();
        }

        public Interpolation Start()
        {
            if (!Active)
                Maze.Instance.UpdateableManager.Add(this);
            else _hook = Maze.Instance.GameTime.TotalGameTime.TotalMilliseconds;

            return this;
        }

        public void Stop(bool setValue = true) =>
            (_stop, _setValue) = (true, setValue);

        public Interpolation(TimeSpan time, RepeatOptions repeatOptions = RepeatOptions.None) =>
            (From, To, Time, _totalMilliseconds, RepeatOptions) = (0f, 1f, time, time.TotalMilliseconds, repeatOptions);

        public event EventHandler Started;
        public event EventHandler Updated;
        public event EventHandler Stopped;

        protected virtual void Begin()
        {
            _hook = Maze.Instance.GameTime.TotalGameTime.TotalMilliseconds;
            _stop = false;
            _setValue = true;

            Value = From;

            Active = true;

            Started?.Invoke(this, EventArgs.Empty);
        }

        protected virtual bool Update(GameTime time)
        {
            if (_stop)
                return true;

            var factor = (float)((time.TotalGameTime.TotalMilliseconds - _hook) / _totalMilliseconds);

            if (factor >= 1f)
            {
                switch (RepeatOptions)
                {
                    case RepeatOptions.None:
                        Active = false;
                        return true;
                    case RepeatOptions.Cycle:
                        (From, To) = (To, From);
                        goto case RepeatOptions.Jump;
                    case RepeatOptions.Jump:
                        Begin();
                        return false;
                }
            }

            Value = MathHelper.Lerp(From, To, Reverse ? 1f - factor : factor);

            Updated?.Invoke(this, EventArgs.Empty);

            return false;
        }

        protected virtual void End()
        {
            if (_setValue)
            {
                Value = To;
                Stopped?.Invoke(this, EventArgs.Empty);
            }
        }

        void IUpdateable.Begin() => Begin();

        void IUpdateable.End() => End();

        bool IUpdateable.Update(GameTime time) => Update(time);
    }
}
