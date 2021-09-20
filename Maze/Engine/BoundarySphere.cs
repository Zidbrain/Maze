using Microsoft.Xna.Framework;

namespace Maze.Engine
{
    public class BoundarySphere : IBoundary
    {
        public float Radius { get; set; }

        public Vector3 Position { get; set; }

        public BoundarySphere(in Vector3 position, float radius) =>
            (Position, Radius) = (position, radius);

        public BoundarySphere(in BoundingSphere sphere) : this(sphere.Center, sphere.Radius) { }

        public bool Contains(in Vector3 point) =>
            (point - Position).LengthSquared() <= Radius * Radius;

        public (float rayDistance, Plane plane)? Intersects(in Ray ray)
        {
            var dist = ray.Intersects(new BoundingSphere(Position, Radius));
            if (dist is null)
                return null;

            var pos = ray.Position + dist.Value * ray.Direction;

            return (dist.Value, new Plane(pos, (pos - Position) / Radius));
        }

        public bool IntersectsOrInside(in BoundingSphere sphere) =>
            sphere.Contains(new BoundingSphere(Position, Radius)) is ContainmentType.Contains or ContainmentType.Intersects;

        public PlaneIntersectionType Intersects(in Plane plane)
        {
            var dist = plane.Distance(Position);
            if (dist >= -Radius && dist <= Radius)
                return PlaneIntersectionType.Intersecting;

            if (dist < 0)
                return PlaneIntersectionType.Back;
            return PlaneIntersectionType.Front;
        }

        public bool IntersectsOrInside(BoundingFrustum frustum) =>
            frustum.Contains(new BoundingSphere(Position, Radius)) is ContainmentType.Contains or ContainmentType.Intersects;

        public static implicit operator BoundarySphere(in BoundingSphere sphere) =>
            new(sphere);
        public static implicit operator BoundingSphere(BoundarySphere sphere) =>
            new(sphere.Position, sphere.Radius);
    }
}
