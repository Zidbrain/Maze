using Microsoft.Xna.Framework;

namespace Maze.Engine
{
    public interface IBoundary
    {
        bool Contains(in Vector3 point);

        (float rayDistance, Plane plane)? Intersects(in Ray ray);

        bool IntersectsOrInside(in BoundingSphere sphere);

        bool IntersectsOrInside(BoundingFrustum frustum);

        PlaneIntersectionType Intersects(in Plane plane);
    }

    public interface ICollideable
    {
        IBoundary Boundary { get; }

        bool CollisionEnabled { get; }

        bool IsStatic { get; }
    }

    public class CollisionObject : ICollideable
    {
        public IBoundary Boundary { get; set; }
        public bool CollisionEnabled { get; set; } = true;
        public bool IsStatic { get; set; }

        public CollisionObject(IBoundary boundary) =>
            Boundary = boundary;
    }
}