using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Maze.Graphics.Shaders;
using Maze.Graphics;

namespace Maze.Engine
{
    public class Square : CollideableLevelObject
    {
        private static readonly Vector3[] s_pos =
        {
            new Vector3(-1f, 0f, -1f),
            new Vector3(-1f, 0f, 1f),
            new Vector3(1f, 0f, 1f),
            new Vector3(1f, 0f, 1f),
            new Vector3(1f, 0f, -1f),
            new Vector3(-1f, 0f, -1f),
        };

        private static readonly VertexBuffer s_buffer;

        static Square()
        {
            var vertexes = new CommonVertex[6];
            for (var i = 0; i < vertexes.Length; i++)
                vertexes[i] = new CommonVertex(s_pos[i], new Vector2((s_pos[i].X + 1f) / 2f, (s_pos[i].Z + 1f) / 2f), Vector3.Up, Vector3.Right);

            s_buffer = new VertexBuffer(Maze.Instance.GraphicsDevice, typeof(CommonVertex), 6, BufferUsage.WriteOnly);
            s_buffer.SetData(vertexes);
        }

        private Matrix _transform;
        public Matrix Transform
        {
            get => _transform;
            set
            {
                _transform = value;
                _boundary.Basis = _transform;
            }
        }

        public override void Update(GameTime time) { }

        private readonly BoundarySquare _boundary;
        public override BoundarySquare Boundary => _boundary;

        public override Vector3 Position
        {
            get => Transform.Translation;
            set
            {
                _transform.Translation = value;
                _boundary.Basis = _transform;
            }
        }

        public Color Color { get; set; } = Color.White;

        public Texture2D Texture { get; set; }
        public Texture2D Normal { get; set; }

        private readonly MeshInfo _info;

        public Square(Level level, Matrix basis, Texture2D texture, Texture2D normal) : base(level)
        {
            basis.Up = Vector3.Normalize(Vector3.Cross(basis.Right, basis.Backward));

            _boundary = new BoundarySquare(basis);

            Transform = basis;
            Texture = texture;
            Normal = normal;

            _info = new MeshInfo(texture, normal, s_buffer);
        }

        public override void Draw()
        {
            if (ShaderState is null)
                ShaderState = new StandartShaderState();

            if (ShaderState is TransformShaderState state)
                state.Transform = Transform;

            if (ShaderState is StandartShaderState standartState)
            {
                standartState.Texture = Texture;
                standartState.Color = Color;
                standartState.NormalTexture = Normal;
            }

            Maze.Instance.DrawVertexes(s_buffer, ShaderState);
        }

        public override void Draw(AutoMesh mesh) =>
            mesh.Add(_info, Transform);
    }
}
