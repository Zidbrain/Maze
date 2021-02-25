using Microsoft.Xna.Framework;
using System.Collections.Generic;
using static System.MathF;

namespace Maze.Engine
{
    public class Polygon
    {
        private readonly Vector3[] _buf;

        public static float Range => 0.1f;

        public Vector3 A => _buf[0];
        public Vector3 B => _buf[1];
        public Vector3 C => _buf[2];

        public Plane Plane { get; }

        public Polygon(params Vector3[] values)
        {
            _buf = new Vector3[3];
            System.Array.Copy(values, _buf, 3);
            Plane = new Plane(_buf[0], _buf[1], _buf[2]);
            Plane.Normalize();
        }

        public Vector3 this[int index] =>
            _buf[index];

        public bool IsInside(in Vector3 point) =>
            Extensions.IsInsideTriangle(A, B, C, point);
    }

    public class BSPTreeNode
    {
        public Plane Plane { get; private set; }
        public Vector3 PointOnPlane { get; private set; }

        public List<Polygon> Polygones { get; private set; }

        public BSPTreeNode Front { get; private set; }
        public BSPTreeNode Back { get; private set; }

        public BSPTreeNode(Polygon[] polygones)
        {
            Polygones = new List<Polygon>(polygones);

            var back = new List<Polygon>();
            var front = new List<Polygon>();

            for (var j = 0; (back.Count == 0 || front.Count == 0) && j < polygones.Length; j++)
            {
                back.Clear();
                front.Clear();

                var point = polygones[j];
                var plane = point.Plane;
                Plane = plane;
                PointOnPlane = point[0];

                if (polygones.Length == 1)
                    return;

                for (var i = 0; i < polygones.Length; i++)
                {
                    var a1 = Vector3.Dot(polygones[i][0] - point[0], plane.Normal);
                    var a2 = Vector3.Dot(polygones[i][1] - point[0], plane.Normal);
                    var a3 = Vector3.Dot(polygones[i][2] - point[0], plane.Normal);

                    if (a1 < 0 || a2 < 0 || a3 < 0)
                        front.Add(polygones[i]);
                    else if (a1 >= 0 && a2 >= 0 && a3 >= 0 )
                        back.Add(polygones[i]);
                }
            }

            if (back.Count == 0 || front.Count == 0)
                return;

            Back = new BSPTreeNode(back.ToArray());
            Front = new BSPTreeNode(front.ToArray());
        }
    }

    public class BSPTree
    {
        private readonly BSPTreeNode _start;

        public BSPTree(IEnumerable<ICollideable> objects)
        {
            var compile = new List<Polygon>();
            foreach (var @object in objects)
                compile.AddRange(@object.Polygones);

            _start = new BSPTreeNode(compile.ToArray());
        }

        public List<Polygon> Collides(in BoundingSphere sphere)
        {
            var node = _start;

            while (true)
            {
                var dot = node.Plane.DotCoordinate(node.PointOnPlane - sphere.Center);
                BSPTreeNode next;

                if (System.Math.Abs(dot) <= sphere.Radius)
                    next = null;
                else if (dot < 0)
                    next = node.Front;
                else
                    next = node.Back;

                if (next is null)
                {
                    var list = new List<Polygon>();
                    foreach (var polygon in node.Polygones)
                    {
                        if (Extensions.Distance(sphere.Center, polygon.Plane) <= sphere.Radius)
                            list.Add(polygon);
                    }

                    return list;
                }
                node = next;
            }
        }

        [System.Obsolete("Incomplete")]
        public (Vector3 point, Polygon polygon)? Collides(in Ray ray)
        {
            static (float dist, Polygon polygon)? Distance(in Ray ray, BSPTreeNode node, float planeDist, bool findMin)
            {
                if (node == null)
                    return null;

                while (true)
                {
                    var dist = Extensions.IntersectionDistance(ray, node.PointOnPlane, node.Plane.Normal);
                    BSPTreeNode next = null;
                    var dot = Vector3.Dot(ray.Position - node.PointOnPlane, node.Plane.Normal);
                    if (dist is null || dist < 0)
                    {
                        if (dot < 0)
                            next = node.Front;
                        else
                            next = node.Back;
                    }
                    else
                    {
                        var frontToRay = dot < 0 ? node.Front : node.Back;
                        var backToRay = dot < 0 ? node.Back : node.Front;

                        var fr = Distance(ray, frontToRay, dist.Value, true);
                        var br = Distance(ray, backToRay, dist.Value, false);

                        if (fr == null)
                            return br;
                        if (br == null)
                            return fr;
                        if (fr.Value.dist < br.Value.dist)
                            return fr;
                        else
                            return br;
                    }

                    if (next is null)
                    {
                        (float dist, Polygon pol)? min = null;

                        foreach (var polygon in node.Polygones)
                        {
                            var ds = Extensions.IntersectionDistance(ray, polygon.A, polygon.Plane.Normal);
                            if (ds != null && ds.Value >= 0 && (min == null || ds.Value < min.Value.dist) && Extensions.IsInsideTriangle(polygon.A, polygon.B, polygon.C, ray.Position + ds.Value * ray.Direction))
                                    min = (ds.Value, polygon);
                        }

                        return min;
                    }

                    node = next;
                }
            }

            var ret = Distance(ray, _start, -1f, true);
            if (ret != null)
                return (ray.Position + ret.Value.dist * ray.Direction, ret.Value.polygon);
            else
                return null;
        }
    }
}
