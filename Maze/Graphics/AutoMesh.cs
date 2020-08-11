using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Maze.Graphics.Shaders;
using Maze.Engine;
using System;

namespace Maze.Graphics
{
    public sealed class MeshInfo : IEquatable<MeshInfo>
    {
        public Texture2D Texture { get; }

        public Texture2D Normal { get; }

        public VertexBuffer VertexBuffer { get; }

        public MeshInfo(Texture2D texture, Texture2D normal, VertexBuffer vertexBuffer) =>
            (Texture, Normal, VertexBuffer) = (texture, normal, vertexBuffer);

        public static bool operator ==(MeshInfo left, MeshInfo right) =>
            left.Texture == right.Texture && left.Normal == right.Normal && left.VertexBuffer == right.VertexBuffer;

        public static bool operator !=(MeshInfo left, MeshInfo right) =>
            left.Texture != right.Texture || left.Normal != right.Normal || left.VertexBuffer != right.VertexBuffer;

        public override bool Equals(object obj) =>
            obj is MeshInfo info && this == info;

        public bool Equals(MeshInfo other) =>
            this == other;

        public override int GetHashCode() =>
            Texture.GetHashCode() ^ Normal.GetHashCode() ^ VertexBuffer.GetHashCode();
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
            foreach (var info in _info)
            {
                if (ShaderState is LevelMeshShaderState meshShader)
                {
                    meshShader.Texture = info.Key.Texture;
                    meshShader.NormalTexture = info.Key.Normal;
                }

                if (ShaderState is WriteDepthInstancedShaderState state)
                    state.Texture = info.Key.Texture;

                info.Value.ShaderState = ShaderState;

                info.Value.Draw();
            }
        }
    }
}
