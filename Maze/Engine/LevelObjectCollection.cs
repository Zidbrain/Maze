using System.Collections.Generic;
using System.Collections;
using Microsoft.Xna.Framework;
using Maze.Graphics.Shaders;
using System;

namespace Maze.Engine
{
    public class EnumerableLevelObjects : IEnumerable<LevelObject>, IDrawable
    {
        public virtual IEnumerable<LevelObject> Base { get; }

        public Level Level { get; }

        public EnumerableLevelObjects(Level level, IEnumerable<LevelObject> objects) =>
            (Level, Base) = (level, objects);

        public IEnumerator<LevelObject> GetEnumerator() => Base.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Base).GetEnumerator();

        public EnumerableLevelObjects Where(Func<LevelObject, bool> condition)
        {
            IEnumerable<LevelObject> Where(Func<LevelObject, bool> condition)
            {
                foreach (var levelObject in this)
                    if (condition(levelObject))
                        yield return levelObject;
            }

            return new EnumerableLevelObjects(Level, Where(condition));
        }

        public EnumerableLevelObjects Intersect(BoundingFrustum frustrum) =>
            Where(t => !t.EnableOcclusionCulling || t.Boundary.IntersectsOrInside(frustrum));

        public EnumerableLevelObjects Intersect(BoundingSphere sphere) =>
            Where(t => t.Boundary.IntersectsOrInside(sphere));

        public EnumerableLevelObjects Static(bool isStatic = true) =>
            Where(t => t.IsStatic == isStatic);

        public LevelObjectCollection Evaluate() => new(Level, new List<LevelObject>(this));

        public void Draw()
        {
            foreach (var levelObject in this)
                if (levelObject.DrawToMesh)
                    levelObject.Draw(Level.Mesh);
                else
                    levelObject.Draw();
        }

        public void SetShaderState(Func<TransformShaderState> stateGenerator)
        {
            foreach (var @object in this)
                @object.ShaderState = stateGenerator?.Invoke();
        }

        public void SetShaderState(TransformShaderState state)
        {
            if (state is null)
                state = new StandartShaderState();

            foreach (var @object in this)
                @object.ShaderState = state;
        }

        public void Update(GameTime time)
        {
            foreach (var obj in this)
                obj.Update(time);
        }
    }

    public class LevelObjectCollection : EnumerableLevelObjects, IList<LevelObject>
    {
        public LevelObject this[int index] { get => ((IList<LevelObject>)Base)[index]; set => ((IList<LevelObject>)Base)[index] = value; }

        public override List<LevelObject> Base { get; }

        public LevelObjectCollection(Level level) : this(level, new List<LevelObject>()) { }

        public LevelObjectCollection(Level level, List<LevelObject> objects) : base(level, objects) =>
            Base = objects;

        public int Count => ((ICollection<LevelObject>)Base).Count;

        public bool IsReadOnly => ((ICollection<LevelObject>)Base).IsReadOnly;

        public void Add(LevelObject item) => ((ICollection<LevelObject>)Base).Add(item);
        public void Clear() => ((ICollection<LevelObject>)Base).Clear();
        public bool Contains(LevelObject item) => ((ICollection<LevelObject>)Base).Contains(item);
        public void CopyTo(LevelObject[] array, int arrayIndex) => ((ICollection<LevelObject>)Base).CopyTo(array, arrayIndex);
        public int IndexOf(LevelObject item) => ((IList<LevelObject>)Base).IndexOf(item);
        public void Insert(int index, LevelObject item) => ((IList<LevelObject>)Base).Insert(index, item);
        public bool Remove(LevelObject item) => ((ICollection<LevelObject>)Base).Remove(item);
        public void RemoveAt(int index) => ((IList<LevelObject>)Base).RemoveAt(index);
    }
}
