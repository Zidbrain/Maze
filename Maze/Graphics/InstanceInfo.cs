using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Maze.Graphics
{
    public readonly struct InstanceInfo : IVertexType
    {
        public readonly uint TextureID;
        public readonly uint NormalMapID;
        public readonly uint TransformID;

        private static readonly VertexDeclaration s_declaration = new(
            new VertexElement(0, VertexElementFormat.Byte4, VertexElementUsage.TextureCoordinate, 1),
            new VertexElement(sizeof(uint), VertexElementFormat.Byte4, VertexElementUsage.TextureCoordinate, 2),
            new VertexElement(sizeof(uint) * 2, VertexElementFormat.Byte4, VertexElementUsage.BlendIndices, 0));

        public VertexDeclaration VertexDeclaration => s_declaration;
    }
}
