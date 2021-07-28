using System;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Maze.Engine;
using Microsoft.Xna.Framework.Graphics;
using Maze.Graphics;
using static System.MathF;
using System.Text.RegularExpressions;

namespace Maze
{
    public interface IDrawable
    {
        void Draw();
    }

    public interface ICollideable
    {
        IEnumerable<Polygon> Polygones { get; }
    }

    public interface IUpdateable
    {
        void Begin();
        bool Update(GameTime time);
        void End();
    }

    public static class Extensions
    {
        private static readonly Random s_random = new();

        private static Texture2D s_sample;
        public static Texture2D Sample => s_sample ??= CreateSample(Color.White, Vector2.One);

        public static Texture2D CreateSample(in Color color, in Vector2 size)
        {
            var ret = new Texture2D(Maze.Instance.GraphicsDevice, (int)size.X, (int)size.Y);

            var surface = (int)size.X * (int)size.Y;
            var data = new Color[surface];

            for (int i = 0; i < surface; i++)
                data[i] = color;
            ret.SetData(data);

            return ret;
        }

        public static bool ConatainsType<T, Type>(this IEnumerable<T> ts) where Type : T
        {
            foreach (var item in ts)
                if (item is Type)
                    return true;
            return false;
        }

        public static Vector3 XZ(this in Vector3 value) =>
            new(value.X, 0f, value.Z);

        public static float Distance(in Vector3 point, in Plane plane) =>
            Math.Abs(SignedDistance(point, plane));

        public static float SignedDistance(in Vector3 point, in Plane plane) =>
            (Vector3.Dot(point, plane.Normal) + plane.D) / plane.Normal.Length();

        public static Vector2 MinusY(this Vector2 value) => new Vector2(value.X, -value.Y);

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

            return Min(a, Min(b, c));
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
            for (var i = 0; i < array.Length; i++)
            {
                var next = s_random.Next(0, array.Length);
                var temp = array[next];
                array[next] = array[i];
                array[i] = temp;
            }
        }

        public static void Fill<T>(this T[] array, T value)
        {
            for (int i = 0; i < array.Length; i++)
                array[i] = value;
        }

        public static IEnumerable<T> ToIEnumerable<T>(this Array target)
        {
            foreach (var item in target)
                yield return (T)item;
        }

        public static void SetRenderTargets(this GraphicsDevice graphicsDevice, RenderTargets renderTargets) =>
            graphicsDevice.SetRenderTargets(renderTargets.Bindings);

        public static Vector4 ToVector4(this in Plane plane) =>
            new(plane.Normal, plane.D);

        public static Matrix GetAlignmentMatrix(in Vector3 from, in Vector3 to)
        {
            var axis = Vector3.Cross(from, to);
            if (axis == Vector3.Zero)
                axis = from;
            return Matrix.CreateFromAxisAngle(Vector3.Normalize(axis), Acos(Vector3.Dot(from, to) / from.Length() / to.Length()));
        }

        /// <summary>
        /// Must be of format (9,9 9,9 9,9)
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Vector3 ParseToVector3(string value)
        {
            var expression = @"\((?'X'[+-]?\d+(,\d+)?)\s" + @"(?'Y'[+-]?\d+(,\d+)?)\s" + @"(?'Z'[+-]?\d+(,\d+)?)\)";

            if (!Regex.IsMatch(value, expression))
                throw new FormatException();

            var groups = Regex.Match(value, expression).Groups;
            return new Vector3(Convert.ToSingle(groups["X"].Value), Convert.ToSingle(groups["Y"].Value), Convert.ToSingle(groups["Z"].Value));
        }

    }
}
