using Assimp;
using Maze.Graphics;
using Maze.Graphics.Shaders;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using System;
using PrimitiveType = Microsoft.Xna.Framework.Graphics.PrimitiveType;

namespace Maze.Engine
{
    public class ModelObject : LevelObject, IDisposable
    {
        private readonly Texture2D[] _normalMap;
        private readonly Texture2D[] _texture;

        private readonly VertexBuffer[] _vertexBuffers;
        private readonly IndexBuffer[] _indexBuffers;

        public NodeCollection Nodes { get; }

        private static readonly FileIOSystem s_ioSystem;
        private static readonly AssimpContext s_context;
        static ModelObject()
        {
            s_context = new();
            s_ioSystem = new($@"{Environment.CurrentDirectory}\Content\Models", @$"{Environment.CurrentDirectory}\Content\Models");
            s_context.SetIOSystem(s_ioSystem);
        }

        public override BoundaryBox Boundary { get; }

        public ModelObject(Level level, string path) : base(level)
        {
            DrawToMesh = false;

            var scene = s_context.ImportFile(path, PostProcessSteps.CalculateTangentSpace | PostProcessSteps.EmbedTextures | PostProcessSteps.GenerateBoundingBoxes |
                PostProcessSteps.Triangulate | PostProcessSteps.SortByPrimitiveType | PostProcessSteps.JoinIdenticalVertices | PostProcessSteps.TransformUVCoords | PostProcessSteps.FlipUVs);

            if (!scene.HasMeshes)
                throw new ArgumentException("Provided file does not contain any models", nameof(path));

            _vertexBuffers = new VertexBuffer[scene.MeshCount];
            _indexBuffers = new IndexBuffer[scene.MeshCount];

            _texture = new Texture2D[scene.MeshCount];
            _normalMap = new Texture2D[scene.MeshCount];

            var root = new Node("ROOT");

            for (int m = 0; m < scene.MeshCount; m++)
            {
                var mesh = scene.Meshes[m];
                for (int i = 0; i < mesh.BoneCount; i++)
                    if (root.Find(mesh.Bones[i].Name) is null)
                        root.Children.Add(new Node(scene.RootNode.FindNode(mesh.Bones[i].Name), mesh.Bones, mesh.Bones[i]) { Parent = root });
            }
            root.RecalculateIndexes();

            Nodes = new NodeCollection(root);

            for (int m = 0; m < scene.MeshCount; m++)
            {
                var mesh = scene.Meshes[m];

                if (mesh.PrimitiveType is not Assimp.PrimitiveType.Triangle)
                    continue;

                if (Boundary is null)
                    Boundary = new BoundaryBox(mesh.BoundingBox.Min.ToVector3(), mesh.BoundingBox.Max.ToVector3());
                else
                    Boundary = Boundary.Join(new BoundaryBox(mesh.BoundingBox.Min.ToVector3(), mesh.BoundingBox.Max.ToVector3()));

                var vertexes = new ModelVertex[mesh.VertexCount];
                var positions = mesh.Vertices;
                var normals = mesh.Normals;
                var tangents = mesh.Tangents;
                var binormals = mesh.BiTangents;
                var textureCoordinates = mesh.TextureCoordinateChannels[0];
                var indices = mesh.GetUnsignedIndices();

                var blendIndices = new Byte4[mesh.VertexCount];
                var blendWeights = new Vector4[mesh.VertexCount];

                for (int i = 0; i < mesh.BoneCount; i++)
                {
                    foreach (var weight in mesh.Bones[i].VertexWeights)
                    {
                        var id = 0;
                        while (true)
                        {
                            if (blendIndices[weight.VertexID].GetByIndex(id) == 0)
                            {
                                blendIndices[weight.VertexID].SetByIndex(id, (byte)Nodes.Find(mesh.Bones[i].Name).Index);
                                blendWeights[weight.VertexID].SetByIndex(id, weight.Weight);
                                break;
                            }
                            id++;

                            if (id > 3)
                                throw new Exception();
                        }
                    }
                }

                for (int i = 0; i < mesh.VertexCount; i++)
                    vertexes[i] = new ModelVertex(positions[i].ToVector3(), normals[i].ToVector3(), tangents[i].ToVector3(), binormals[i].ToVector3(), textureCoordinates[i].ToVector2(),
                        blendIndices[i], blendWeights[i]);

                _vertexBuffers[m] = new VertexBuffer(Maze.Instance.GraphicsDevice, typeof(ModelVertex), mesh.VertexCount, BufferUsage.WriteOnly);
                _vertexBuffers[m].SetData(vertexes);
                _indexBuffers[m] = new IndexBuffer(Maze.Instance.GraphicsDevice, IndexElementSize.ThirtyTwoBits, indices.Length, BufferUsage.WriteOnly);
                _indexBuffers[m].SetData(indices);

                _texture[m] = scene.GetEmbeddedTexture(scene.Materials[mesh.MaterialIndex].TextureDiffuse.FilePath).ToGraphicsTexture();
                _normalMap[m] = scene.GetEmbeddedTexture(scene.Materials[mesh.MaterialIndex].TextureNormal.FilePath).ToGraphicsTexture();
            }

            CustomInterpolation<Node>.Start(Nodes.Find("Palm"), static (node, t) => node.Transform = Matrix.CreateRotationY(t * MathHelper.TwoPi), TimeSpan.FromSeconds(1d), RepeatOptions.Jump);
            CustomInterpolation<Node>.Start(Nodes.Find("Forearm"), static (node, t) => node.Transform = Matrix.CreateRotationX(((t * 2) - 1) * MathHelper.PiOver4), TimeSpan.FromSeconds(1d), RepeatOptions.Cycle);

            EnableOcclusionCulling = false;

            ShaderState = new StandartShaderState();
        }

        public override void Draw()
        {
            var shader = Maze.Instance.Shader;
            var gd = Maze.Instance.GraphicsDevice;

            ShaderState.Bones = Nodes.GetAbsoluteTransforms();

            var prev = gd.RasterizerState;
            gd.RasterizerState = new RasterizerState() { DepthBias = prev.DepthBias, SlopeScaleDepthBias = prev.SlopeScaleDepthBias, CullMode = CullMode.CullClockwiseFace, MultiSampleAntiAlias = prev.MultiSampleAntiAlias };

            for (int i = 0; i < _vertexBuffers.Length; i++)
            {
                if (ShaderState is StandartShaderState standartState)
                {
                    standartState.Texture = _texture[i];
                    standartState.NormalTexture = _normalMap[i];
                }

                gd.SetVertexBuffer(_vertexBuffers[i]);
                gd.Indices = _indexBuffers[i];

                shader.State = ShaderState;
                shader.Apply();
                gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _indexBuffers[i].IndexCount / 3);
            }

            gd.RasterizerState = prev;
        }


        public override void Draw(AutoMesh mesh) => throw new NotImplementedException();
        private bool _disposedValue;

        public override void Update(GameTime time) { }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _normalMap.Dispose();
                    _texture.Dispose();
                    _vertexBuffers.Dispose();
                    _indexBuffers.Dispose();
                }

                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
