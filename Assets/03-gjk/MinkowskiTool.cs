using UnityEngine;
using System.Collections.Generic;

namespace Sample03
{
    public static class MinkowskiTool
    {

        // 生成闵可夫斯基差集
        public static List<Vector2> computeMinkowskiSet(Shape shapeA, Shape shapeB)
        {
            List<Vector2> vertices = new List<Vector2>();
            foreach (var v1 in shapeA.vertices)
            {
                foreach (var v2 in shapeB.vertices)
                {
                    vertices.Add(v1 - v2);
                }
            }
            return ConvexHullGenerator.GenConvexHull(vertices.ToArray());
        }
        
    }

}
