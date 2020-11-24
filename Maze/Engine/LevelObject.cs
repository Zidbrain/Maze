using System;
using Microsoft.Xna.Framework;
using Maze.Graphics;
using System.Collections.ObjectModel;
using System.Collections;
using System.Collections.Generic;
using Maze.Graphics.Shaders;
using Microsoft.Xna.Framework.Graphics;

namespace Maze.Engine
{
    public abstract class LevelObject : IDrawable, ICollidable
    {
        public Level Level { get; }

        public virtual ShaderState ShaderState { get; set; }

        public bool DrawToMesh { get; set; } = true;

        public bool EnableCollision { get; set; } = true;

        public LevelObject(Level level) =>
            Level = level;

        public abstract void Draw();

        public abstract void Draw(AutoMesh mesh);

        public abstract bool Intersects(BoundingFrustum frustum);

        public abstract bool Intersects(in BoundingSphere sphere);

        public virtual Vector3 Position { get; set; }

        public abstract IEnumerable<Polygon> Polygones { get; }
    }

    public class LevelObjectCollection : Collection<LevelObject>, IDrawable
    {
        public Level Level { get; }

        public LevelObjectCollection(Level level) =>
            Level = level;

        public LevelObjectCollection Intersect(BoundingFrustum frustrum)
        {
            var ret = new LevelObjectCollection(Level);

            foreach (var levelObject in this)
                if (levelObject.Intersects(frustrum))
                    ret.Add(levelObject);

            return ret;
        }

        public LevelObjectCollection Intersect(in BoundingSphere sphere)
        {
            var ret = new LevelObjectCollection(Level);

            foreach (var levelObject in this)
                if (levelObject.Intersects(sphere))
                    ret.Add(levelObject);

            return ret;
        }

        public void Draw()
        {
            foreach (var levelObject in this)
                if (levelObject.DrawToMesh)
                    levelObject.Draw(Level.Mesh);
                else
                    levelObject.Draw();
        }

        public void SetShaderState(Func<ShaderState> stateGenerator)
        {
            foreach (var @object in this)
                @object.ShaderState = stateGenerator?.Invoke();
        }

        public void SetShaderState(ShaderState state)
        {
            foreach (var @object in this)
                @object.ShaderState = state;
        }
    }
}
