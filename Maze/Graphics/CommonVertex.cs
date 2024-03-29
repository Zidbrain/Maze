﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.InteropServices;

namespace Maze.Graphics
{
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct CommonVertex : IVertexType
    {
        private static readonly VertexDeclaration s_vertexDeclaration = new(
            new[]
            {
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                new VertexElement(sizeof(float) * 3, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
                new VertexElement(sizeof(float) * 5, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
                new VertexElement(sizeof(float) * 8, VertexElementFormat.Vector3, VertexElementUsage.Tangent, 0)
            }
        );

        public readonly Vector3 Position;

        public readonly Vector2 TextureCoordinate;

        public readonly Vector3 Normal;

        public readonly Vector3 Tangent;

        public VertexDeclaration VertexDeclaration => s_vertexDeclaration;

        public CommonVertex(Vector3 position, Vector2 textureCoordinate, Vector3 normal, Vector3 tangent) =>
            (Position, TextureCoordinate, Normal, Tangent) = (position, textureCoordinate, normal, tangent);
    }
}
