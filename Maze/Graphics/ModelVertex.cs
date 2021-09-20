using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using Microsoft.Xna.Framework;

namespace Maze.Graphics
{
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public readonly struct ModelVertex : IVertexType
    {
        public readonly Vector3 Position;
        public readonly Vector3 Normal;
        public readonly Vector3 Tangent;
        public readonly Vector3 Binormal;
        public readonly Vector2 TextureCoordinate;
        public readonly Byte4 BlendIndices;
        public readonly Vector4 BlendWeights;

        public static readonly VertexDeclaration s_vertexDeclaration = new(
            new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            new VertexElement(sizeof(float) * 3, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
            new VertexElement(sizeof(float) * 6, VertexElementFormat.Vector3, VertexElementUsage.Tangent, 0),
            new VertexElement(sizeof(float) * 9, VertexElementFormat.Vector3, VertexElementUsage.Binormal, 0),
            new VertexElement(sizeof(float) * 12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
            new VertexElement(sizeof(float) * 14, VertexElementFormat.Byte4, VertexElementUsage.BlendIndices, 0),
            new VertexElement(sizeof(float) * 14 + sizeof(byte) * 4, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 0));

        public ModelVertex(Vector3 position, Vector3 normal, Vector3 tangent, Vector3 binormal, Vector2 textureCoordinate, Byte4 blendIndices, Vector4 blendWeights)
        {
            Position = position;
            Normal = normal;
            Tangent = tangent;
            Binormal = binormal;
            TextureCoordinate = textureCoordinate;
            BlendIndices = blendIndices;
            BlendWeights = blendWeights;
        }

        public VertexDeclaration VertexDeclaration => s_vertexDeclaration;
    }
}
