using Microsoft.Xna.Framework;
using System.Collections.Immutable;
using System.Collections.Generic;
using static System.MathF;
using System;

namespace Maze.Engine
{
    public class BSPTreeNode
    {
        private enum BoxSplitType
        {
            X, Y, Z
        }

        public Plane SplittingPlane { get; private set; }

        public ImmutableList<ICollideable> Objects { get; private set; }

        public ImmutableList<ICollideable> UnsplittableObjects { get; private set; }

        public BSPTreeNode Front { get; private set; }
        public BSPTreeNode Back { get; private set; }

        private BSPTreeNode(IEnumerable<ICollideable> boundaries, BoundingBox boundingBox, BoxSplitType xyzSplit, int splitDepth)
        {
            List<ICollideable> frontList, backList, unsplittable, objects;
            BoundingBox frontBox, backBox;

            do
            {
                splitDepth--;
                if (splitDepth == 0)
                {
                    Objects = ImmutableList.CreateRange(boundaries);
                    UnsplittableObjects = ImmutableList.Create<ICollideable>();
                    return;
                }

                var center = (boundingBox.Max - boundingBox.Min) / 2f;

                SplittingPlane = xyzSplit switch
                {
                    BoxSplitType.X => new(center, Vector3.Right),
                    BoxSplitType.Y => new(center, Vector3.Up),
                    BoxSplitType.Z => new(center, Vector3.Backward),
                    _ => throw new ArgumentException("Wrong box split type", nameof(xyzSplit))
                };

                frontList = new List<ICollideable>();
                backList = new List<ICollideable>();
                unsplittable = new List<ICollideable>();
                objects = new List<ICollideable>();

                foreach (var boundary in boundaries)
                {
                    if (boundary.IsStatic)
                    {
                        unsplittable.Add(boundary);
                        continue;
                    }

                    switch (boundary.Boundary.Intersects(SplittingPlane))
                    {
                        case PlaneIntersectionType.Front:
                            frontList.Add(boundary);
                            objects.Add(boundary);
                            break;
                        case PlaneIntersectionType.Back:
                            backList.Add(boundary);
                            objects.Add(boundary);
                            break;
                        case PlaneIntersectionType.Intersecting:
                            unsplittable.Add(boundary);
                            break;
                    }
                }

                frontBox = xyzSplit switch
                {
                    BoxSplitType.X => new(new Vector3(center.X, boundingBox.Min.Y, boundingBox.Min.Z), boundingBox.Max),
                    BoxSplitType.Y => new(new Vector3(boundingBox.Min.X, center.Y, boundingBox.Min.Z), boundingBox.Max),
                    BoxSplitType.Z => new(new Vector3(boundingBox.Min.X, boundingBox.Min.Y, center.Z), boundingBox.Max),
                    _ => throw new ArgumentException("Wrong box split type", nameof(xyzSplit))
                };
                backBox = xyzSplit switch
                {
                    BoxSplitType.X => new(boundingBox.Min, new Vector3(center.X, boundingBox.Max.Y, boundingBox.Max.Z)),
                    BoxSplitType.Y => new(boundingBox.Min, new Vector3(boundingBox.Max.X, center.Y, boundingBox.Max.Z)),
                    BoxSplitType.Z => new(boundingBox.Min, new Vector3(boundingBox.Max.X, boundingBox.Max.Y, center.Z)),
                    _ => throw new ArgumentException("Wrong box split type", nameof(xyzSplit))
                };

                xyzSplit++;
                if ((int)xyzSplit > 2)
                    xyzSplit = 0;

                if (frontList.Count == 0 && backList.Count == 0)
                    break;

                if (frontList.Count == 0)
                {
                    boundingBox = backBox;
                    backList.AddRange(unsplittable);
                    boundaries = backList;
                }
                else if (backList.Count == 0)
                {
                    boundingBox = frontBox;
                    frontList.AddRange(unsplittable);
                    boundaries = frontList;
                }

            } while (frontList.Count == 0 || backList.Count == 0);

            UnsplittableObjects = ImmutableList.CreateRange(unsplittable);
            Objects = ImmutableList.CreateRange(objects);

            if (frontList.Count != 0 || backList.Count != 0)
            {
                Front = new BSPTreeNode(frontList, frontBox, xyzSplit, splitDepth);
                Back = new BSPTreeNode(backList, backBox, xyzSplit, splitDepth);
            }
        }

        public BSPTreeNode(IEnumerable<ICollideable> boundaries, in BoundingBox boundingBox, int maxSplitDepth) : this(boundaries, boundingBox, BoxSplitType.Z, maxSplitDepth) { }
    }

    public class BSPTree
    {
        public BSPTreeNode Start { get; }

        public BSPTree(IEnumerable<ICollideable> objects, in BoundingBox boundingBox, int maxSplitDepth) =>
            Start = new BSPTreeNode(objects, boundingBox, maxSplitDepth);

        public List<ICollideable> Intersects(BoundingSphere sphere)
        {
            var result = new List<ICollideable>();

            void Intersects(BSPTreeNode node)
            {
                while (true)
                {
                    foreach (var obj in node.UnsplittableObjects)
                        if (obj.CollisionEnabled && obj.Boundary.IntersectsOrInside(sphere))
                            result.Add(obj);

                    if (node.Front is null || node.Back is null)
                    {
                        foreach (var obj in node.Objects)
                            if (obj.CollisionEnabled && obj.Boundary.IntersectsOrInside(sphere))
                                result.Add(obj);

                        break;
                    }

                    if (Abs(node.SplittingPlane.Distance(sphere.Center)) <= sphere.Radius)
                    {
                        Intersects(node.Front);
                        Intersects(node.Back);

                        break;
                    }

                    if (node.SplittingPlane.DotCoordinate(sphere.Center) >= 0)
                        node = node.Front;
                    else
                        node = node.Back;
                }
            }

            Intersects(Start);

            return result;
        }
    }
}
