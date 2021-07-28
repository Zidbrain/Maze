using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Maze.Graphics.Shaders;

namespace Maze.Graphics
{
    public sealed record MeshInfo(Texture2D Texture, Texture2D Normal, VertexBuffer VertexBuffer)
    {
        public Texture2D Texture { get; init; } = Texture;

        public Texture2D Normal { get; init; } = Normal;

        public VertexBuffer VertexBuffer { get; init; } = VertexBuffer;
    }

    public class AutoMesh : IDrawable
    {
        private readonly Dictionary<MeshInfo, InstancedQueue> _info;

        private InstancedShaderState _shaderState;
        public InstancedShaderState ShaderState
        {
            get
            {
                if (_shaderState is null)
                    _shaderState = new LevelMeshShaderState();
                return _shaderState;
            }
            set => _shaderState = value;
        }

        public AutoMesh()
        {
            ShaderState = new LevelMeshShaderState();

            _info = new Dictionary<MeshInfo, InstancedQueue>();
        }

        public void Add(MeshInfo info, in Matrix matrix)
        {
            if (!_info.ContainsKey(info))
                _info.Add(info, new InstancedQueue(info.VertexBuffer, ShaderState));

            _info[info].Add(matrix);
        }

        public void Draw()
        {
            foreach (KeyValuePair<MeshInfo, InstancedQueue> info in _info)
            {
                if (ShaderState is LevelMeshShaderState meshShader)
                {
                    meshShader.Texture = info.Key.Texture;
                    meshShader.NormalTexture = info.Key.Normal;
                }

                info.Value.ShaderState = ShaderState;

                info.Value.Draw();
            }
        }
    }
}
