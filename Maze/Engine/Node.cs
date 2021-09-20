using Assimp;
using Microsoft.Xna.Framework;
using System.Collections;
using System.Collections.Generic;

namespace Maze.Engine
{
    public class Node
    {
        private bool _needsUpdate;
        private void Update()
        {
            _needsUpdate = true;
            foreach (var child in Children)
                child.Update();
        }
        public Matrix InverseBind { get; private init; }

        public Matrix ParentOffset { get; private init; }

        private Matrix _transform = Matrix.Identity;
        /// <summary>
        /// Local transform of this <see cref="Node"/>
        /// </summary>
        public Matrix Transform
        {
            get => _transform;
            set
            {
                _transform = value;
                Update();
            }
        }

        private Matrix _absTransform;
        public Matrix AbsoluteTransform
        {
            get
            {
                if (_needsUpdate)
                {
                    if (Parent is not null)
                        _absTransform = _transform * ParentOffset * Parent.AbsoluteTransform;
                    else
                        _absTransform = _transform;
                    _needsUpdate = false;
                }

                return _absTransform;
            }
        }

        public List<Node> Children { get; }

        public Node Parent { get; init; }

        private int _index;
        public int Index
        {
            get => _index;
            private set
            {
                _index = value;
                for (int i = 0; i < Children.Count; i++)
                    Children[i].Index = _index + i + 1;
            }
        }

        public string Name { get; private init; }

        public Node Find(string name)
        {
            if (Name == name)
                return this;

            for (int i = 0; i < Children.Count; i++)
            {
                var find = Children[i].Find(name);
                if (find is not null)
                    return find;
            }

            return null;
        }

        public void RecalculateIndexes()
        {
            var root = this;
            while (root.Parent is not null)
                root = root.Parent;

            var currentLayer = new List<Node>() { root };
            root.Index = 0;

            var count = 0;

            do
            {
                var nextLayer = new List<Node>();
                foreach (var node in currentLayer)
                    nextLayer.AddRange(node.Children);

                count += currentLayer.Count;

                for (int i = 0; i < nextLayer.Count; i++)
                    nextLayer[i]._index = count + i;
                currentLayer = nextLayer;
            } while (currentLayer.Count is not 0);
        }

        public List<Node> ToList()
        {
            var result = new List<Node>() { this };
            var currentLayer = new List<Node> { this };
            do
            {
                var nextLayer = new List<Node>();
                foreach (var node in currentLayer)
                    nextLayer.AddRange(node.Children);
                result.AddRange(nextLayer);
                currentLayer = nextLayer;
            } while (currentLayer.Count is not 0);
            return result;
        }

        public Matrix[] GetVertexTransforms()
        {
            var result = new List<Matrix>() { InverseBind * AbsoluteTransform };
            var currentLayer = new List<Node> { this };
            do
            {
                var nextLayer = new List<Node>();
                foreach (var node in currentLayer)
                    nextLayer.AddRange(node.Children);

                foreach (var node in nextLayer)
                    result.Add(node.InverseBind * node.AbsoluteTransform);

                currentLayer = nextLayer;
            } while (currentLayer.Count is not 0);
            return result.ToArray();
        }

        public Node(Assimp.Node node, List<Bone> bones, Bone root)
        {
            Children = new List<Node>(node.ChildCount);
            for (int i = 0; i < node.ChildCount; i++)
                Children.Add(new Node(node.Children[i], bones, bones.Find(t => t.Name == node.Children[i].Name)) { Parent = this, Index = Index + i });

            InverseBind = root.OffsetMatrix.ToMatrix();
            ParentOffset = node.Transform.ToMatrix();

            Name = node.Name;
            Update();
        }

        public Node(string name)
        {
            Children = new List<Node>();

            InverseBind = Matrix.Identity;
            ParentOffset = Matrix.Identity;

            Name = name;
            Update();
        }
    }

    public class NodeCollection : IReadOnlyList<Node>
    {
        private readonly List<Node> _list;

        public Node this[int index] => _list[index];

        public int Count => _list.Count;

        public IEnumerator<Node> GetEnumerator() => _list.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public NodeCollection(Node parent) => _list = parent.ToList();

        public Matrix[] GetAbsoluteTransforms()
        {
            var result = new Matrix[_list.Count];
            for (int i = 0; i < _list.Count; i++)
                result[i] = _list[i].InverseBind * _list[i].AbsoluteTransform;
            return result;
        }

        public Node Find(string name) => _list.Find(t =>  t.Name == name);
    }
}
