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
            AABBNode neighbour = node == parent.left ? parent.right : parent.left;

            // 将邻居结点作为父结点
            AABBNode grandParent = parent.parent;
            neighbour.parent = grandParent;
            if (grandParent == null)
            {
                root = neighbour;
            }
            else
            {
                if (parent == grandParent.left)
                {
                    grandParent.left = neighbour;
                }
                else
                {
                    grandParent.right = neighbour;
                }
                updateBoundsBottomUp(grandParent);
            }
        }

        public void updateShape(Shape shape)
        {
            AABBNode node;
            if (!nodes.TryGetValue(shape, out node))
            {
                return;
            }

            if (node.bounds.contains(shape.bounds))
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
        public bool raycast(Ray2D ray, float maxDistance, out RaycastHit hit)
        {
            hit = new RaycastHit();
            if (root == null)
            {
                return false;
            }

            hit.distance = maxDistance;
            return raycast(root, ray, ref hit);
        }

        private bool raycast(AABBNode node, Ray2D ray, ref RaycastHit hit)
        {
            if (node.isLeaf)
            {
                RaycastHit temp;
                if (node.shape.raycast(ray, out temp) && temp.distance < hit.distance)
                {
                    hit = temp;
                    return true;
                }
                return false;
            }

            Vector2 rayEnd = ray.origin + ray.direction * hit.distance;
            float d1 = node.left.bounds.getDistance(ray.origin, rayEnd);
            float d2 = node.right.bounds.getDistance(ray.origin, rayEnd);

            bool ret = false;
            if (d1 < d2)
            {
                if (d1 < hit.distance)
                {
                    ret = raycast(node.left, ray, ref hit) || ret;
                }
                if (d2 < hit.distance)
                {
                    ret = raycast(node.right, ray, ref hit) || ret;
                }
            }
            else
            {
                if (d2 < hit.distance)
                {
                    ret = raycast(node.right, ray, ref hit) || ret;
                }
                if (d1 < hit.distance)
                {
                    ret = raycast(node.left, ray, ref hit) || ret;
                }
            }
            return ret;
        }

        /// <summary>
        /// 碰撞查询
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="visitor">visitor的返回值表示是否终止查询</param>
        public void query(AABB bounds, Func<AABBNode, bool> visitor)
        {
            if (root == null)
            {
                return;
            }
            query(root, bounds, visitor);
        }

        private bool query(AABBNode node, AABB bounds, Func<AABBNode, bool> visitor)
        {
            if (!node.bounds.intersect(bounds))
            {
                return false;
            }

            if (node.isLeaf)
            {
                return visitor(node);
            }
            else
            {
                if (query(node.left, bounds, visitor))
                {
                    return true;
                }
                return query(node.right, bounds, visitor);
            }
        }

        private AABBNode createLeaf(Shape shape, AABBNode parent = null)
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

        private AABBNode createNode(AABBNode left, AABBNode right, AABBNode parent = null)
        {
            AABBNode ret = new AABBNode
            {
                bounds = new AABB(),
                left = left,
                right = right,
                parent = parent,
            };
            left.parent = ret;
            right.parent = ret;
            return ret;
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
