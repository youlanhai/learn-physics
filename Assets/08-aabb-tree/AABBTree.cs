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

            Rect bounds = shape.GetLooseBounds();

            AABBNode node = root;
            while (!node.isLeaf)
            {
                float leftCost = getCost(node.left.bounds, node.right.bounds, bounds);
                float rightCost = getCost(node.right.bounds, node.left.bounds, bounds);

                if (leftCost < rightCost)
                {
                    node = node.left;
                }
                else
                {
                    node = node.right;
                }
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

            Shape neighbourShape;
            AABBNode parent = node.parent;
            if (node == parent.left)
            {
                neighbourShape = parent.right.shape;
            }
            else
            {
                neighbourShape = parent.left.shape;
            }

            // 将父结点变成一个叶结点
            parent.left = null;
            parent.right = null;
            parent.shape = neighbourShape;
            parent.bounds = neighbourShape.GetLooseBounds();
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

            if (GJKTool.containsRect(node.bounds, shape.bounds))
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
        /// 重新构建整个树。会得到一个查询效率较高的满二叉树
        /// </summary>
        public void rebuild()
        {

        }

        public void rebuild(List<Shape> shapes)
        {

        }

        /// <summary>
        /// 射线拾取
        /// </summary>
        public void raycast(Vector2 origin, Vector3 diretion, float maxDistance, Func<bool, AABBNode> visitor)
        {
        }

        public void query(Rect bounds, Func<bool, AABBNode> visitor)
        {

        }

        private AABBNode createLeaf(Shape shape, AABBNode parent)
        {
            AABBNode ret = new AABBNode
            {
                bounds = shape.GetLooseBounds(),
                shape = shape,
                parent = parent,
            };
            nodes[shape] = ret;
            return ret;
        }

        private AABBNode createNode(Rect bounds, AABBNode parent)
        {
            return new AABBNode
            {
                bounds = bounds,
                parent = parent,
            };
        }

        /// <summary>
        /// 获取c合并到a的估值
        /// </summary>
        private float getCost(Rect a, Rect b, Rect c)
        {
            Rect ac = GJKTool.mergeRect(a, c);
            return ac.width * ac.height + b.width * b.height;
        }

        private void updateBoundsBottomUp(AABBNode node)
        {
            while (node != null)
            {
                node.bounds = GJKTool.mergeRect(node.left.bounds, node.right.bounds);
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
        public Rect bounds;

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
