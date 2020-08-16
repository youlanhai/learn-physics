using UnityEngine;
using System.Collections.Generic;

namespace gjk2d
{
    public static class MinkowskiTool
    {

        // 生成一个近似的闵可夫斯基差集。也只能逼近，但不是最全的。
        public static List<Vector2> computeMinkowskiSet(Shape shapeA, Shape shapeB)
        {
            List<Vector2> vertices = new List<Vector2>();

            int n = shapeA.vertices.Count;
            for (int i = 0; i < n; ++i)
            {
                Vector2 a = shapeA.vertices[i];
                Vector2 b = shapeA.vertices[(i - 1 + n) % n];
                Vector2 c = shapeA.vertices[(i + 1) % n];

                Vector2 dir = getHalfVector(a, b, c);
                vertices.Add(support(shapeA, shapeB, dir));
                vertices.Add(support(shapeA, shapeB, -dir));

                dir = b - a;
                dir = new Vector2(dir.y, -dir.x);
                vertices.Add(support(shapeA, shapeB, dir));
                vertices.Add(support(shapeA, shapeB, -dir));
            }

            n = shapeB.vertices.Count;
            for (int i = 0; i < n; ++i)
            {
                Vector2 a = shapeB.vertices[i];
                Vector2 b = shapeB.vertices[(i - 1 + n) % n];
                Vector2 c = shapeB.vertices[(i + 1) % n];
                Vector2 dir = getHalfVector(a, b, c);

                vertices.Add(support(shapeA, shapeB, dir));
                vertices.Add(support(shapeA, shapeB, -dir));

                dir = b - a;
                dir = new Vector2(dir.y, -dir.x);
                vertices.Add(support(shapeA, shapeB, dir));
                vertices.Add(support(shapeA, shapeB, -dir));
            }

            foreach (var v1 in shapeA.vertices)
            {
                foreach (var v2 in shapeB.vertices)
                {
                    Vector2 dir = v1 - v2;
                    vertices.Add(support(shapeA, shapeB, dir));
                    vertices.Add(support(shapeA, shapeB, -dir));
                }
            }

            return ConvexHullGenerator.GenConvexHull(vertices.ToArray());
        }

        static Vector2 support(Shape shapeA, Shape shapeB, Vector2 dir)
        {
            Vector2 a = shapeA.getFarthestPointInDirection(dir);
            Vector2 b = shapeB.getFarthestPointInDirection(-dir);
            return a - b;
        }

        static Vector2 getHalfVector(Vector2 a, Vector2 aPrev, Vector2 aNext)
        {
            Vector2 ab = aPrev - a;
            Vector2 ac = aNext - a;
            Vector2 dir = ab.normalized + ac.normalized;
            return dir;
        }
    }

}