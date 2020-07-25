using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Maze.Graphics.Shaders;
using Maze.Engine;

namespace Maze.Graphics
{
    public class LevelMesh : IDrawable
    {
        private readonly InstancedShaderState _shader;
        private readonly Dictionary<(Texture2D texture, Texture2D normal), List<Matrix>> _info;
        private readonly VertexBuffer _sample;
        private readonly IndexBuffer _indices;

        private const int s_maxPolygones = 255;

        public LevelMesh(VertexBuffer sample, Level level)
        {
            var values = new KeyValuePair<(Texture2D, Texture2D), List<Matrix>>[3];
            var array = level.Textures.GetArray();
            for (var i = 0; i < 3; i++)
                values[i] = new KeyValuePair<(Texture2D, Texture2D), List<Matrix>>(array[i], new List<Matrix>());

            _sample = sample;

            _info = new Dictionary<(Texture2D, Texture2D), List<Matrix>>(values);

            _shader = new InstancedShaderState();

            var indices = new ushort[_sample.VertexCount];
            for (ushort i = 0; i < indices.Length; i++)
                indices[i] = i;

            _indices = new IndexBuffer(Maze.Instance.GraphicsDevice, IndexElementSize.SixteenBits, indices.Length, BufferUsage.WriteOnly);
            _indices.SetData(indices);
        }

        public void Add((Texture2D texture, Texture2D normal) info, in Matrix matrix) =>
            _info[info].Add(matrix);

        public void Draw()
        {
            var gd = Maze.Instance.GraphicsDevice;

            _shader.Matrix = Maze.Instance.Shader.StandartState.Matrix;
            gd.SetVertexBuffer(_sample);
            gd.Indices = _indices;

            foreach (var info in _info)
            {
                _shader.Texture = info.Key.texture;
                _shader.NormalTexture = info.Key.normal;

                var completeMatrices = info.Value.ToArray();

                for (var j = 0; j < completeMatrices.Length; j += s_maxPolygones)
                {
                    if (j + s_maxPolygones > completeMatrices.Length)
                        _shader.Matrices = completeMatrices[j..];
                    else
                        _shader.Matrices = completeMatrices[j..(j + s_maxPolygones)];

                    Maze.Instance.Shader.State = _shader;
                    Maze.Instance.Shader.Apply();
                    gd.DrawInstancedPrimitives(PrimitiveType.TriangleList, 0, 0, 2, _shader.Matrices.Length, 0);
                }
            }

            foreach (var info in _info)
                info.Value.Clear();
        }
    }
}
