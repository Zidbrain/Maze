using Microsoft.Xna.Framework;

namespace Maze.Engine
{
    public abstract class Entity : ICollideable, IDrawable
    {
        public Level Level { get; }

        public Vector3 Position { get; set; }

        public abstract IBoundary Boundary { get; }
        public bool CollisionEnabled { get; set; } = true;
        public bool IsStatic { get; set; } = true;

        public abstract void Draw();
        public abstract void Update(GameTime time);

        public Entity(Level level) =>
            Level = level;
    }
}
