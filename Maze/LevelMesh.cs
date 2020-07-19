using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace Maze
{
    public class LevelMesh : IDrawable
    {
        private InstancedShaderState _shader;
        private readonly Dictionary<Texture2D, List<Matrix>> _info;
        private readonly VertexBuffer _sample;
        private readonly IndexBuffer _indices;

        private const int s_maxPolygones = 255;

        public LevelMesh(VertexBuffer sample, params Texture2D[] textures)
        {
            var values = new KeyValuePair<Texture2D, List<Matrix>>[textures.Length];
            for (int i = 0; i < textures.Length; i++)
                values[i] = new KeyValuePair<Texture2D, List<Matrix>>(textures[i], new List<Matrix>());

            _sample = sample;

            _info = new Dictionary<Texture2D, List<Matrix>>(values);

            _shader = new InstancedShaderState();

            var indices = new ushort[_sample.VertexCount];
            for (ushort i = 0; i < indices.Length; i++)
                indices[i] = i;

            _indices = new IndexBuffer(Maze.Game.GraphicsDevice, IndexElementSize.SixteenBits, indices.Length, BufferUsage.WriteOnly);
            _indices.SetData(indices);
        }

        public void Add(Texture2D texture, in Matrix matrix) =>
            _info[texture].Add(matrix);

        public void Draw()
        {
            var gd = Maze.Game.GraphicsDevice;

            _shader.Matrix = Maze.Game.Shader.StandartState.Matrix;
            gd.SetVertexBuffer(_sample);
            gd.Indices = _indices;

            foreach (var info in _info)
            {
                _shader.Texture = info.Key;

                var completeMatrices = info.Value.ToArray();

                for (int j = 0; j < completeMatrices.Length; j += s_maxPolygones)
                {
                    if (j + s_maxPolygones > completeMatrices.Length)
                        _shader.Matrices = completeMatrices[j..];
                    else
                        _shader.Matrices = completeMatrices[j..(j + s_maxPolygones)];

                    Maze.Game.Shader.State = _shader;
                    Maze.Game.Shader.Apply();
                    gd.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, 0, 2, _shader.Matrices.Length, 0);
                }
            }

            foreach (var info in _info)
                info.Value.Clear();
        }
    }
}
