using static System.MathF;
using Microsoft.Xna.Framework;
using System;

namespace Maze.Engine
{
    public class BoundaryBox : IBoundary
    {
        public Vector3 Min { get; set; }
        public Vector3 Max { get; set; }

        public Vector3 Center => Min + (Max - Min) / 2f;

        public BoundaryBox(Vector3 min, Vector3 max) => (Min, Max) = (min, max);

        public bool Contains(in Vector3 point) =>
            point.X > Min.X && point.Y > Min.Y && point.Z > Min.Z &&
            point.X < Max.X && point.Y < Max.Y && point.Z < Max.Z;

        public Plane GetPlane(in Vector3 point)
        {
            int planeIndex;
            var k = (Max.Y - Min.Y) / (Max.X - Min.X);

            var y = point.Y - Min.Y;
            var xmin = point.X - Min.X;
            var xmax = point.X - Max.X;

            if (y >= k * xmin)
                planeIndex = 2;
            else
                planeIndex = 0;
            if (y >= -k * xmax)
                planeIndex++;

            var kz = (Max.Z - Min.Z) / (Max.X - Min.X);
            var z = point.Z - Min.Z;
            var a = z >= kz * xmin;
            var b = z >= -kz * xmax;

            if (a == b)
                if (a)
                    planeIndex = 5;
                else
                    planeIndex = 4;

            return planeIndex switch
            {
                0 => new(Min, Vector3.Down),
                1 => new(Max, Vector3.Right),
                2 => new(Min, Vector3.Left),
                3 => new(Max, Vector3.Up),
                4 => new(Min, Vector3.Forward),
                5 => new(Max, Vector3.Backward),
                _ => throw new Exception()
            };
        }

        public BoundaryBox Join(BoundaryBox other)
        {
            Vector3 min = new();
            Vector3 max = new();

            min.X = Min(Min.X, other.Min.X);
            min.Y = Min(Min.Y, other.Min.Y);
            min.Z = Min(Min.Z, other.Min.Z);
            max.X = Max(Max.X, other.Max.X);
            max.Y = Max(Max.Y, other.Max.Y);
            max.Z = Max(Max.Z, other.Max.Z);

            return new(min, max);
        }

        public (float rayDistance, Plane plane)? Intersects(in Ray ray)
        {
            static (float, int) Min((float, int) a, (float, int) b) =>
                (a.Item1 < b.Item1) ? a : b;
            static (float, int) Max((float, int) a, (float, int) b) =>
                (a.Item1 > b.Item1) ? a : b;

            var dirInv = new Vector3(1f / ray.Direction.X, 1f / ray.Direction.Y, 1f / ray.Direction.Z);

            var min = this.Min;
            var max = this.Max;

            (float val, int plane) t1 = ((min.X - ray.Position.X) * dirInv.X, 2);
            (float val, int plane) t2 = ((max.X - ray.Position.X) * dirInv.X, 1);

            (var tmin, var tmax) = (t1.val < t2.val) ?
                (t1, t2) : (t2, t1);

            t1 = ((min.Y - ray.Position.Y) * dirInv.Y, 0);
            t2 = ((max.Y - ray.Position.Y) * dirInv.Y, 3);

            tmin = Max(tmin, Min(Min(t1, t2), tmax));
            tmax = Min(tmax, Max(Max(t1, t2), tmin));

            t1 = ((min.Z - ray.Position.Z) * dirInv.Z, 4);
            t2 = ((max.Z - ray.Position.Z) * dirInv.Z, 5);

            tmin = Max(tmin, Min(Min(t1, t2), tmax));
            tmax = Min(tmax, Max(Max(t1, t2), tmin));

            if (float.IsInfinity(tmin.val) || float.IsInfinity(tmax.val))
                return null;

            (float val, int plane) result;

            if (tmin.val < 0)
                if (tmax.val >= 0)
                    result = tmax;
                else
                    return null;
            else
                result = tmin;

            return (result.val, result.plane switch
            {
                0 => new(min, Vector3.Down),
                1 => new(max, Vector3.Right),
                2 => new(min, Vector3.Left),
                3 => new(max, Vector3.Up),
                4 => new(min, Vector3.Forward),
                5 => new(max, Vector3.Backward),
                _ => throw new Exception()
            });
        }

        public PlaneIntersectionType Intersects(in Plane plane) =>
            plane.Intersects(new BoundingBox(Min, Max));

        public bool IntersectsOrInside(in BoundingSphere sphere) => 
            new BoundingBox(Min, Max).Contains(sphere) is ContainmentType.Intersects;

        public bool IntersectsOrInside(BoundingFrustum frustum) =>
            frustum.Intersects(new BoundingBox(Min, Max));

        /// <summary>
        /// Get specified corner of the box. Counting from <see cref="Max"/> clock-wise, starting with a XY plane.
        /// </summary>
        /// <param name="cornerIndex"></param>
        /// <returns></returns>
        public Vector3 this[int cornerIndex]
        {
            get
            {
                if (cornerIndex < 0 || cornerIndex > 7)
                    throw new ArgumentException("Corner index must be in range of 0-7", nameof(cornerIndex));

                return new(
                    (cornerIndex & 0b11) is 0 or 1 ? Max.X : Min.X,
                    (cornerIndex & 0b11) is 0 or 3 ? Max.Y : Min.Y,
                    (cornerIndex & 0b100) is 0 ? Max.Z : Min.Z);
            }
        }

        public static implicit operator BoundingBox(BoundaryBox boundaryBox) =>
            new(boundaryBox.Min, boundaryBox.Max);
        public static implicit operator BoundaryBox(BoundingBox boundingBox) =>
            new(boundingBox.Min, boundingBox.Max);
    }
}