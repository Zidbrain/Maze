using System;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Maze.Engine;

namespace Maze
{
    public interface IDrawable
    {
        void Draw();
    }

    public interface ICollidable
    {
        IEnumerable<Polygon> Polygones { get; }
    }

    public static class Extensions
    {
        private static Random s_random = new Random();

        public static Vector3 XZ(this Vector3 value) =>
            new Vector3(value.X, 0f, value.Z);

        public static float Distance(in Vector3 point, in Plane plane) =>
            Math.Abs(SignedDistance(point, plane));

        public static float SignedDistance(in Vector3 point, in Plane plane) =>
            (Vector3.Dot(point, plane.Normal) + plane.D) / plane.Normal.Length();

        public static float SignedDistance(in Vector3 point, in Vector3 pointOnPlane, in Vector3 normal) =>
            Vector3.Dot(normal, pointOnPlane - point) / normal.LengthSquared();

        public static float? IntersectionDistance(in Ray ray, in Vector3 point, in Vector3 normal)
        {
            var pn = Vector3.Dot(ray.Direction, normal);
            if (pn != 0)
                return Vector3.Dot(normal, point - ray.Position) / pn;
            return null;
        }

        public static float Distance(in Ray ray, in Vector3 point) =>
            Vector3.Cross(point - ray.Position, Vector3.Normalize(ray.Direction)).Length();

        public static float Distance(Polygon polygon, in Vector3 point)
        {
            var a = Distance(new Ray(polygon.A, polygon.B - polygon.A), point);
            var b = Distance(new Ray(polygon.A, polygon.C - polygon.A), point);
            var c = Distance(new Ray(polygon.B, polygon.C - polygon.B), point);

            return MathF.Min(a, MathF.Min(b, c));
        }

        public static bool IsInsideTriangle(in Vector3 a, in Vector3 b, in Vector3 c, in Vector3 point)
        {
            var newCoord = Vector3.Transform(point - a, Matrix.Invert(new Matrix(
                new Vector4(b - a, 0),
                new Vector4(c - a, 0),
                new Vector4(Vector3.Cross(b - a, c - a), 0),
                new Vector4(0, 0, 0, 1))));
            return newCoord.X >= 0 && newCoord.X <= 1 && newCoord.Y >= 0 && newCoord.Y <= 1 && newCoord.X + newCoord.Y <= 1;
        }

        public static void Shuffle<T>(this T[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                var next = s_random.Next(0, array.Length);
                var temp = array[next];
                array[next] = array[i];
                array[i] = temp;
            }
        }

        public static IEnumerable<T> ToIEnumerable<T>(this Array target)
        {
            foreach (var item in target)
                yield return (T)item;
        }
    }
}
