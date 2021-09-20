using static System.MathF;
using static Maze.VectorMath;
using Microsoft.Xna.Framework;
using System;

namespace Maze.Engine
{
    public class BoundarySquare : IBoundary
    {
        private Matrix _basis;
        public Matrix Basis
        {
            get => _basis;
            set
            {
                _basis = value;

                Inverse = Matrix.Invert(_basis);

                Plane = new Plane(_basis.Translation, _basis.Up);
                _corners = new Vector3[]
                {
                    Vector3.Transform(new Vector3(1f, 0f, 1f), Basis),
                    Vector3.Transform(new Vector3(1f, 0f, -1f), Basis),
                    Vector3.Transform(new Vector3(-1f, 0f, -1f), Basis),
                    Vector3.Transform(new Vector3(-1f, 0f, 1f), Basis),
                };
            }
        }

        public Matrix Inverse { get; private set; }

        public Plane Plane { get; private set; }

        public BoundarySquare(Matrix basis) =>
            Basis = basis;

        private Vector3[] _corners;
        public Vector3 this[int cornerIndex] => _corners[cornerIndex];

        public bool Contains(in Vector3 point)
        {
            var local = Vector3.Transform(point, Inverse);
            return Math.Abs(local.Y) <= 1e-6f && local.X >= 0f && local.X <= 1f && local.Z >= 0f && local.Z <= 1f;
        }

        public (float rayDistance, Plane plane)? Intersects(in Ray ray)
        {
            var distance = Distance(ray, Plane);

            if (distance < 0)
                return null;

            var point = Vector3.Transform(ray.Position + ray.Direction * distance, Inverse);

            if (point.X <= 1f && point.X >= -1f && point.Z <= 1f && point.Z >= -1f)
                return (distance, Plane);

            return null;
        }

        public bool IntersectsOrInside(in BoundingSphere sphere)
        {
            var projectedRadiusSqr = Sqr(sphere.Radius) - Sqr(Plane.Distance(sphere.Center));

            if (projectedRadiusSqr < 0)
                return false;

            var size = new Vector2(Basis.Right.Length(), Basis.Backward.Length());
            var center = Vector3.Transform(sphere.Center, Inverse);
            center.X *= size.X;
            center.Z *= size.Y;
            var radius = Sqrt(projectedRadiusSqr);
            var min = new Vector2(-size.X, -size.Y);
            var max = -min;

            if (center.X - min.X >= radius &&
                center.Z - min.Y >= radius &&
                max.X - center.X >= radius &&
                min.Y - center.Z >= radius)
                return false;

            var dmin = 0d;

            var e = (double)(center.X - min.X);
            if (e < 0)
            {
                if (e < -radius)
                    return false;
                dmin += e * e;
            }
            else
            {
                e = center.X - max.X;
                if (e > 0)
                {
                    if (e > radius)
                        return false;
                    dmin += e * e;
                }
            }

            e = center.Z - min.Y;
            if (e < 0)
            {
                if (e < -radius)
                    return false;
                dmin += e * e;
            }
            else
            {
                e = center.Z - max.Y;
                if (e > 0)
                {
                    if (e > radius)
                        return false;
                    dmin += e * e;
                }
            }

            if (dmin <= projectedRadiusSqr)
                return true;

            return false;
        }

        public bool IntersectsOrInside(BoundingFrustum frustum)
        {
            var planes = new Plane[BoundingFrustum.PlaneCount]
            {
                frustum.Near,
                frustum.Far,
                frustum.Left,
                frustum.Right,
                frustum.Top,
                frustum.Bottom
            };

            for (int i = 0; i < BoundingFrustum.PlaneCount; i++)
            {
                if (Intersects(planes[i]) is PlaneIntersectionType.Front)
                    return false;
            }

            return true;
        }

        public PlaneIntersectionType Intersects(in Plane plane)
        {
            var prev = float.NaN;
            for (int i = 0; i < 4; i++)
            {
                var dot = plane.DotCoordinate(_corners[i]);

                if (prev * dot <= 0f)
                    return PlaneIntersectionType.Intersecting;

                prev = dot;
            }

            return prev < 0 ? PlaneIntersectionType.Back : PlaneIntersectionType.Front;
        }
    }
}