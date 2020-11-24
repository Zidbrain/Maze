using Microsoft.Xna.Framework;
using System;

namespace Maze.Engine
{
    public delegate bool Condition<in T>(T @object);

    public class ConditionalInterpolation<T> : CustomInterpolation<T>
    {
        private bool _passedCondition;

        public Condition<T> Condition { get; set; }

        public ConditionalInterpolation(T @object, Setter<T> setter, Condition<T> condition, TimeSpan time, RepeatOptions repeatOptions = RepeatOptions.None) :
            base(@object, setter, time, repeatOptions) => Condition = condition;

        public static ConditionalInterpolation<T> Start(T @object, Setter<T> setter, Condition<T> condition, TimeSpan time, RepeatOptions repeatOptions = RepeatOptions.None)
        {
            var ret = new ConditionalInterpolation<T>(@object, setter, condition, time, repeatOptions);
            ret.Start();
            return ret;
        }

        public event EventHandler Entered;
        public event EventHandler Exited;

        private bool _toExit;

        protected override void Begin()
        {
            base.Begin();
            Entered?.Invoke(this, EventArgs.Empty);
        }

        protected override bool Update(GameTime time)
        {
            if (Condition(Object))
            {
                if (!_passedCondition)
                {
                    _toExit = false;
                    if (!Active)
                        Entered?.Invoke(this, EventArgs.Empty);

                    From = Value;
                    To = 1f;
                    base.Begin();
                }
                _passedCondition = true;
            }
            else
            {
                if (_passedCondition)
                {
                    From = Value;
                    To = 0f;
                    base.Begin();
                    _toExit = true;
                }
                _passedCondition = false;
            }

            if (Active)
            {
                Active = !base.Update(time);

                if (!Active)
                {
                    End();
                    if (_toExit)
                        Exited?.Invoke(this, EventArgs.Empty);
                }
            }
            return false;
        }
    }
}
