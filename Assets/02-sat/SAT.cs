using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Sample02
{

    public class SAT
    {
        public static bool queryCollision(Shape shapeA, Shape shapeB)
        {
            return satTestHalf(shapeA, shapeB) && satTestHalf(shapeB, shapeA);
        }
        
        static bool satTestHalf(Shape shapeA, Shape shapeB)
        {
            // 计算shape本身在边那一侧
            int side = whitchSide(shapeA.get(0), shapeA.get(1), shapeA.get(2));
            int n = shapeA.count();
            for (int i = 0; i < n; ++i)
            {
                Edge edge = new Edge
                {
                    start = shapeA.get(i),
                    end = shapeA.get((i + 1) % n),
                };

                if (isInDifferentSide(edge, side, shapeB))
                {
                    return false;
                }
            }
            return true;
        }

        /// 判读shape是否在edge的另一侧
        static bool isInDifferentSide(Edge edge, int side, Shape shape)
        {
            foreach(Vector2 vertex in shape.vertices)
            {
                int s = whitchSide(edge.start, edge.end, vertex);
                if (s * side > 0)
                {
                    // 在同一侧，也就是说edge无法将shape分离开
                    return false;
                }
            }
            return true;
        }

        /// 判读点c在ab的哪一侧
        public static int whitchSide(Vector2 a, Vector2 b, Vector2 c)
        {
            Vector2 ab = b - a;
            Vector2 ac = c - a;
            float cross = ab.x * ac.y - ab.y * ac.x;
            return cross > 0 ? 1 : (cross < 0 ? -1 : 0);
        }
    }


    public class Edge
    {
        public Vector2 start;
        public Vector2 end;
    }


    public class Shape
    {
        public List<Vector2> vertices = new List<Vector2>();

        public int count()
        {
            return vertices.Count;
        }

        public Vector2 get(int index)
        {
            return vertices[index];
        }


        // 多边形是否包含点
        public bool contains(Vector2 point)
        {
            int n = vertices.Count;
            if (n < 3)
            {
                return false;
            }

            // 先计算出内部的方向
            int innerSide = SAT.whitchSide(vertices[0], vertices[1], vertices[2]);

            // 通过判断点是否均在三条边的内侧，来判定单形体是否包含点
            for (int i = 0; i < n; ++i)
            {
                int iNext = (i + 1) % n;
                int side = SAT.whitchSide(vertices[i], vertices[iNext], point);

                if (side * innerSide < 0) // 在外部
                {
                    return false;
                }
            }

            return true;
        }
    }
}
