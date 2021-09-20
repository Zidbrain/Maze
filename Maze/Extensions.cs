using Microsoft.Xna.Framework.Graphics;
using Maze.Graphics;
using static System.MathF;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System;
using System.IO;
using Microsoft.Xna.Framework.Graphics.PackedVector;

namespace Maze
{
    public interface IDrawable
    {
        void Draw();
    }

    public interface IUpdateable
    {
        void Begin();
        bool Update(GameTime time);
        void End();
    }

    public static class VectorMath
    {
        public const float Sqrt2Over2 = 0.70710678118f;

        public static Matrix GetAlignmentMatrix(in Vector3 from, in Vector3 to)
        {
            var axis = Vector3.Cross(from, to);
            if (axis == Vector3.Zero)
            {
                if (Vector3.Dot(from, to) < 0)
                    return Matrix.CreateScale(-1f);
                return Matrix.Identity;
            }
            return Matrix.CreateFromAxisAngle(Vector3.Normalize(axis), Acos(Vector3.Dot(from, to) / from.Length() / to.Length()));
        }

        public static float Distance(in Ray ray, in Vector3 point) =>
            Vector3.Cross(point - ray.Position, Vector3.Normalize(ray.Direction)).Length();

        public static Vector3 Abs(in Vector3 value) =>
            new(MathF.Abs(value.X), MathF.Abs(value.Y), MathF.Abs(value.Z));

        public static bool CheckForOrthagonal(in Matrix matrix) =>
            Vector3.Dot(matrix.Up, matrix.Right) == 0f && Vector3.Dot(matrix.Up, matrix.Backward) == 0f;

        public static Matrix Construct(in Vector3 scale, in Vector3 yawPithRoll, in Vector3 translation) =>
             //Matrix.CreateTranslation(translation) * Matrix.CreateFromYawPitchRoll(yawPithRoll.X, yawPithRoll.Y, yawPithRoll.Z) * Matrix.CreateScale(scale);
             Matrix.CreateScale(scale) * Matrix.CreateFromYawPitchRoll(yawPithRoll.X, yawPithRoll.Y, yawPithRoll.Z) * Matrix.CreateTranslation(translation);

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static float Sqr(float value) => value * value;

        public static float Distance(in Ray ray, in Plane plane)
        {
            var denominator = Vector3.Dot(ray.Direction, plane.Normal);
            if (denominator == 0)
                return float.NaN;

            return -plane.DotCoordinate(ray.Position) / denominator;
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

        public static Vector3 XZ(this in Vector3 value) =>
            new(value.X, 0f, value.Z);

        public static float Distance(this in Plane plane, in Vector3 point) =>
            (Vector3.Dot(point, plane.Normal) + plane.D) / plane.Normal.Length();

        public static Vector2 MinusY(this Vector2 value) => new(value.X, -value.Y);

        public static Vector3 Project(this in Vector3 point, in Plane plane) =>
            point - plane.Normal * plane.DotCoordinate(point) / plane.Normal.Length();
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

        public static Plane[] GetPlanes(this BoundingFrustum frustum) =>
            new[]
            {
                frustum.Far,
                frustum.Near,
                frustum.Right,
                frustum.Left,
                frustum.Top,
                frustum.Bottom
            };

        public static bool ConatainsType<T, Type>(this IEnumerable<T> ts) where Type : T
        {
            foreach (var item in ts)
                if (item is Type)
                    return true;
            return false;
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

        public static float GetFrameTimestep(this GameTime time) =>
            (float)time.ElapsedGameTime.TotalSeconds;

        public static IEnumerable<T> ToIEnumerable<T>(this Array target)
        {
            foreach (var item in target)
                yield return (T)item;
        }

        public static Vector3 ToVector3(this in Assimp.Vector3D vector) =>
            new(vector.X, vector.Y, vector.Z);

        public static Vector2 ToVector2(this in Assimp.Vector3D vector) =>
            new(vector.X, vector.Y);

        public static Matrix ToMatrix(this in Assimp.Matrix4x4 matrix)
        {
            var result = new Matrix();
            for (int x = 0; x < 4; x++)
                for (int y = 0; y < 4; y++)
                    result[x, y] = matrix[y + 1, x + 1];
            return result;
        }

        public static unsafe byte GetByIndex(this Byte4 vector, int index)
        {
            var value = vector.PackedValue;
            byte* refer = (byte*)&value;
            return refer[index];
        }

        public static void Dispose(this IEnumerable<IDisposable> disposables)
        {
            foreach (var obj in disposables)
                obj.Dispose();
        }

        public static Texture2D ToGraphicsTexture(this Assimp.EmbeddedTexture texture)
        {
            if (texture.IsCompressed)
            {
                using var stream = new MemoryStream(texture.CompressedData);
                return Texture2D.FromStream(Maze.Instance.GraphicsDevice, stream);
            }

            var texels = texture.NonCompressedData;
            var result = new Texture2D(Maze.Instance.GraphicsDevice, texture.Width, texture.Height, false, SurfaceFormat.Color);
            var color = new Color[texels.Length];
            for (int i = 0; i < texels.Length; i++)
                color[i] = new Color(texels[i].R, texels[i].G, texels[i].B, texels[i].A);
            result.SetData(color);
            return result;
        }

        public static unsafe void SetByIndex(this ref Byte4 vector, int index, byte @byte)
        {
            var value = vector.PackedValue;
            byte* refer = (byte*)&value;
            refer[index] = @byte;
            vector.PackedValue = *(uint*)refer;
        }

        public static float GetByIndex(this in Vector4 vector, int index) =>
            index switch
            {
                0 => vector.X,
                1 => vector.Y,
                2 => vector.Z,
                3 => vector.W,
                _ => throw new ArgumentOutOfRangeException(nameof(index), "Index must be in rage"),
            };

        public static void SetByIndex(this ref Vector4 vector, int index, float value)
        {
            switch (index)
            {
                case 0:
                    vector.X = value;
                    break;
                case 1:
                    vector.Y = value;
                    break;
                case 3:
                    vector.Z = value;
                    break;
                case 4:
                    vector.W = value;
                    break;
            }
        }

        public static void SetRenderTargets(this GraphicsDevice graphicsDevice, RenderTargets renderTargets) =>
            graphicsDevice.SetRenderTargets(renderTargets.Bindings);

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
