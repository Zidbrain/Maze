using Microsoft.Xna.Framework;
using System;

namespace Maze.Engine
{
    public delegate void Setter<in T>(T @object, float value);

    public class CustomInterpolation<T> : Interpolation
    {
        public T Object { get; set; }

        public Setter<T> Setter { get; set; }

        public CustomInterpolation(T @object, Setter<T> setter, float from, float to, TimeSpan time, RepeatOptions repeatOptions = RepeatOptions.None) :
            base(from, to, time, repeatOptions)
        {
            (Object, Setter) = (@object, setter);

            Started += Set;
            Updated += Set;
            Stopped += Set;
        }


        private void Set(object sender, EventArgs e) =>
            Setter(Object, Value);

        public static CustomInterpolation<T> Start(T @object, Setter<T> setter, float from, float to, TimeSpan time, RepeatOptions repeatOptions = RepeatOptions.None)
        {
            var ret = new CustomInterpolation<T>(@object, setter, from, to, time, repeatOptions);
            ret.Start();
            return ret;
        }
    }
}
