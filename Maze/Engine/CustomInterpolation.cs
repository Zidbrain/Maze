using Microsoft.Xna.Framework;
using System;

namespace Maze.Engine
{
    public delegate void Setter<in T>(T @object, float value);

    public class CustomInterpolation<T> : Interpolation
    {
        public T Object { get; set; }

        public Setter<T> Setter { get; set; }

        public CustomInterpolation(T @object, Setter<T> setter, TimeSpan time, RepeatOptions repeatOptions = RepeatOptions.None) :
            base(time, repeatOptions) => (Object, Setter) = (@object, setter);

        protected override void Begin()
        {
            base.Begin();
            Setter(Object, Value);
        }

        protected override void End()
        {
            base.End();
            Setter(Object, Value);
        }

        protected override bool Update(GameTime time)
        {
            var ret = base.Update(time);
            Setter(Object, Value);
            return ret;
        }

        public new CustomInterpolation<T> Start() => base.Start() as CustomInterpolation<T>;

        public static CustomInterpolation<T> Start(T @object, Setter<T> setter, TimeSpan time, RepeatOptions repeatOptions = RepeatOptions.None)
        {
            var ret = new CustomInterpolation<T>(@object, setter, time, repeatOptions);
            return ret.Start();
        }
    }
}
