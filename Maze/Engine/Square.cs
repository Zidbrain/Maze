using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Maze.Graphics.Shaders;
using Maze.Graphics;

namespace Maze.Engine
{
    public class Square : IDrawable, ICollideable
    {
        private static readonly Vector3[] s_pos =
        {
            new Vector3(-0.5f, 0f, -0.5f),
            new Vector3(-0.5f, 0f, 0.5f),
            new Vector3(0.5f, 0f, 0.5f),
            new Vector3(0.5f, 0f, 0.5f),
            new Vector3(-0.5f, 0f, -0.5f),
            new Vector3(0.5f, 0f, -0.5f)
        };

        private static readonly VertexBuffer s_buffer;

        static Square()
        {
            var vertexes = new CommonVertex[6];
            for (var i = 0; i < vertexes.Length; i++)
                vertexes[i] = new CommonVertex(s_pos[i], new Vector2(s_pos[i].X + 0.5f, s_pos[i].Z + 0.5f), Vector3.Up, Vector3.Right);

            s_buffer = new VertexBuffer(Maze.Instance.GraphicsDevice, typeof(CommonVertex), 6, BufferUsage.WriteOnly);
            s_buffer.SetData(vertexes);
        }

        public Matrix Transform { get; set; }

        public Vector3 Position { get; set; }
        public Vector2 Size { get; set; } = Vector2.One;
        public Color Color { get; set; } = Color.White;

        public IShaderState ShaderState { get; set; }

        public Texture2D Texture { get; set; }
        public Texture2D Normal { get; set; }

        public IEnumerable<Polygon> Polygones
        {
            get
            {
                var ret = new Polygon[2];
                for (var i = 0; i < 2; i++)
                {
                    var buf = new Vector3[3];
                    for (var j = 0; j < 3; j++)
                        buf[j] = Vector3.Transform(s_pos[i * 3 + j], Matrix.CreateScale(Size.X, 1f, Size.Y) * Transform * Matrix.CreateTranslation(Position));

                    ret[i] = new Polygon(buf);
                }

                return ret;
            }
        }

        private readonly MeshInfo _info;

        public Square(Matrix basis, Texture2D texture, Texture2D normal)
        {
            Transform = basis;
            Texture = texture;
            Normal = normal;

            _info = new MeshInfo(texture, normal, s_buffer);
        }

        public void Draw()
        {
            if (ShaderState is null)
                ShaderState = new StandartShaderState();

            if (ShaderState is TransformShaderState state)
                state.Transform = Matrix.CreateScale(Size.X, 1f, Size.Y) * Transform * Matrix.CreateTranslation(Position);

            if (ShaderState is StandartShaderState standartState)
            {
                standartState.Texture = Texture;
                standartState.Color = Color;
                standartState.NormalTexture = Normal;
            }

            Maze.Instance.DrawVertexes(s_buffer, ShaderState);
        }

        public void Draw(AutoMesh mesh) =>
            mesh.Add(_info, Matrix.CreateScale(Size.X, 1f, Size.Y) * Transform * Matrix.CreateTranslation(Position));
    }
}
