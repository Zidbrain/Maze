using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace Maze.Engine
{
    public class CollectionInterpolation<T> : IUpdateable
    {
        private readonly List<ConditionalInterpolation<T>> _conditionals;

        public IEnumerable<T> Collection { get; }

        public Setter<T> Setter { get; }

        public Condition<T> Condition { get; }

        public Condition<T> SkipCondition { get; set; }

        public Action<T> OnEnter { get; set; }

        public Action<T> OnExit { get; set; }

        public TimeSpan Time { get; }

        public bool Active { get; private set; }

        public CollectionInterpolation(IEnumerable<T> collection, Setter<T> setter, Condition<T> condition, TimeSpan time)
        {
            (Collection, Setter, Condition, Time) = (collection, setter, condition, time);

            _conditionals = new List<ConditionalInterpolation<T>>();
            foreach (var item in collection)
            {
                var interpolation = new ConditionalInterpolation<T>(item, setter, condition, time);
                interpolation.Entered += (_, _) => 
                OnEnter?.Invoke(item);
                interpolation.Exited += (_, _) => OnExit?.Invoke(item);
                _conditionals.Add(interpolation);
            }
        }

        public static CollectionInterpolation<T> Start(IEnumerable<T> collection, Setter<T> setter, Condition<T> condition, TimeSpan time)
        {
            var ret = new CollectionInterpolation<T>(collection, setter, condition, time);
            ret.Start();
            return ret;
        }

        public CollectionInterpolation<T> Start()
        {
            Maze.Instance.UpdateableManager.Add(this);
            return this;
        }

        public void Stop() => Active = false;

        void IUpdateable.Begin() => Active = true;

        bool IUpdateable.Update(GameTime time)
        {
            foreach (var interpolation in _conditionals)
            {
                if (!SkipCondition?.Invoke(interpolation.Object) ?? true)
                    (interpolation as IUpdateable).Update(time);
            }

            return !Active;
        }

        void IUpdateable.End() { }
    }
}
