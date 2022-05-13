using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sample08
{
    /// <summary>
    /// 每个子结点都是包围盒的二叉树
    /// </summary>
    public class AABBTree
    {
        private AABBNode root;

        /// <summary>
        /// 用于快速查询Shape所在的AABB结点
        /// </summary>
        private Dictionary<Shape, AABBNode> nodes = new Dictionary<Shape, AABBNode>();

        public AABBNode getRoot() { return root; }

        public void addShape(Shape shape)
        {
            if (root == null)
            {
                root = createLeaf(shape, null);
                return;
            }

            AABB bounds = shape.getLooseBounds();

            AABBNode node = root;
            while (!node.isLeaf)
            {
                bool isLeft = getBestParent(node.left.bounds, node.right.bounds, bounds);
                node = isLeft ? node.left : node.right;
            }
            
            node.left = createLeaf(node.shape, node);
            node.right = createLeaf(shape, node);
            // node结点原本是一个叶结点，现在变成一个中间结点
            node.shape = null;

            updateBoundsBottomUp(node);
        }

        public void removeShape(Shape shape)
        {
            AABBNode node;
            if (!nodes.TryGetValue(shape, out node))
            {
                return;
            }
            nodes.Remove(shape);

            if (node == root)
            {
                root = null;
                return;
            }

            AABBNode parent = node.parent;
            Shape neighbourShape = node == parent.left ? parent.right.shape : parent.left.shape;

            // 将父结点变成一个叶结点
            parent.left = null;
            parent.right = null;
            parent.shape = neighbourShape;
            parent.bounds = neighbourShape.getLooseBounds();
            nodes[shape] = parent;

            updateBoundsBottomUp(parent.parent);
        }

        public void updateShape(Shape shape)
        {
            AABBNode node;
            if (!nodes.TryGetValue(shape, out node))
            {
                return;
            }

            if (node.bounds.Contains(shape.bounds))
            {
                return;
            }

            removeShape(shape);
            addShape(shape);
        }

        public void clear()
        {
            root = null;
            nodes.Clear();
        }
        
        /// <summary>
        /// 射线拾取
        /// </summary>
        public void raycast(Vector2 origin, Vector3 diretion, float maxDistance, Func<bool, AABBNode> visitor)
        {
        }

        public void query(Rect bounds, Func<bool, AABBNode> visitor)
        {
            if (root == null)
            {
                return;
            }
            query(root, bounds, visitor);
        }

        private void query(AABBNode node, Rect bounds, Func<bool, AABBNode> visitor)
        {
        }

        private AABBNode createLeaf(Shape shape, AABBNode parent)
        {
            AABBNode ret = new AABBNode
            {
                bounds = shape.getLooseBounds(),
                shape = shape,
                parent = parent,
            };
            nodes[shape] = ret;
            return ret;
        }

        private AABBNode createNode(AABB bounds, AABBNode parent)
        {
            return new AABBNode
            {
                bounds = bounds,
                parent = parent,
            };
        }


        private bool getBestParent(AABB a, AABB b, AABB c)
        {
            float w1 = (a + c).area + b.area;
            float w2 = (b + c).area + a.area;

            if (w1 < w2)
            {
                return true;
            }
            else if (w1 > w2)
            {
                return false;
            }

            w1 = Mathf.Abs(c.xMin + c.xMax - a.xMin - a.xMax) + Mathf.Abs(c.yMin + c.yMax - a.yMin - a.yMax);
            w2 = Mathf.Abs(c.xMin + c.xMax - b.xMin - b.xMax) + Mathf.Abs(c.yMin + c.yMax - b.yMin - b.yMax);
            return w1 < w2;
        }

        private void updateBoundsBottomUp(AABBNode node)
        {
            while (node != null)
            {
                node.bounds = node.left.bounds + node.right.bounds;
                node = node.parent;
            }
        }
    }

    /// <summary>
    /// AABB树的结点
    /// </summary>
    public class AABBNode
    {
        /// <summary>
        /// 当前结点的包围盒
        /// </summary>
        public AABB bounds;

        /// <summary>
        /// 父结点
        /// </summary>
        public AABBNode parent;

        /// <summary>
        /// 左子结点。非叶结点才有
        /// </summary>
        public AABBNode left;

        /// <summary>
        /// 右子结点。非叶结点才有
        /// </summary>
        public AABBNode right;

        /// <summary>
        /// 当前结点的碰撞体。只有叶结点才有。
        /// </summary>
        public Shape shape;

        public bool isLeaf { get { return shape != null; } }
    }
}
