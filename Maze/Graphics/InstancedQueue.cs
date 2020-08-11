using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Maze.Graphics.Shaders;

namespace Maze.Graphics
{
    public class InstancedQueue : IDrawable
    {
        private readonly List<Matrix> _matrices;
        private readonly VertexBuffer _vertexes;
        private readonly IndexBuffer _indices;

        private const int s_maxPolygones = 255;

        public InstancedShaderState ShaderState { get; set; }

        public InstancedQueue(VertexBuffer sample, InstancedShaderState shaderState)
        {
            _matrices = new List<Matrix>();

            _vertexes = sample;

            ShaderState = shaderState;

            var indices = new ushort[_vertexes.VertexCount];
            for (ushort i = 0; i < indices.Length; i++)
                indices[i] = i;

            _indices = new IndexBuffer(Maze.Instance.GraphicsDevice, IndexElementSize.SixteenBits, indices.Length, BufferUsage.WriteOnly);
            _indices.SetData(indices);
        }

        public void Add(in Matrix matrix) =>
            _matrices.Add(matrix);

        public void Draw()
        {
            var gd = Maze.Instance.GraphicsDevice;

            gd.SetVertexBuffer(_vertexes);
            gd.Indices = _indices;

            var completeMatrices = _matrices.ToArray();

            for (var j = 0; j < completeMatrices.Length; j += s_maxPolygones)
            {
                if (j + s_maxPolygones > completeMatrices.Length)
                    ShaderState.Matrices = completeMatrices[j..];
                else
                    ShaderState.Matrices = completeMatrices[j..(j + s_maxPolygones)];

                Maze.Instance.Shader.State = ShaderState;
                Maze.Instance.Shader.Apply();
                gd.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, 0, 2, ShaderState.Matrices.Length, 0);
            }

            _matrices.Clear();
        }
    }
}
