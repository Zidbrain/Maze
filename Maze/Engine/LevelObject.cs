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
    public abstract class LevelObject : IDrawable
    {
        /// <summary>
        /// Indicates that this object is unmoving and doesn't change properties which may affect it's appearance
        /// </summary>
        /// <remarks>All static objects can be moved but it may have unintended side effects in static object handlers (e.g. <see cref="Engine.Level"/>)</remarks>
        public bool IsStatic { get; set; }

        public Level Level { get; }

        public virtual TransformShaderState ShaderState { get; set; }

        public abstract IBoundary Boundary { get; }

        public bool DrawToMesh { get; set; } = true;

        public LevelObject(Level level) => Level = level;

        public abstract void Update(GameTime time);

        public abstract void Draw();

        public abstract void Draw(AutoMesh mesh);

        public virtual Vector3 Position { get; set; }

        public bool EnableOcclusionCulling { get; set; } = true;
    }

    public abstract class CollideableLevelObject : LevelObject, ICollideable
    {
        public CollideableLevelObject(Level level) : base(level) { }

        public bool CollisionEnabled { get; set; } = true;
    }
}
