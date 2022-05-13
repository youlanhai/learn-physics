using UnityEngine;
using System.Collections.Generic;

namespace Sample08
{
    public static class MinkowskiTool
    {

        // 生成闵可夫斯基差集
        public static List<Vector2> computeMinkowskiSet(Shape shapeA, Shape shapeB)
        {
            List<Vector2> verts1 = new List<Vector2>();
            shapeA.getDebugVertices(verts1);

            List<Vector2> verts2 = new List<Vector2>();
            shapeB.getDebugVertices(verts2);

            List<Vector2> vertices = new List<Vector2>();
            foreach (var v1 in verts1)
            {
                foreach (var v2 in verts2)
                {
                    vertices.Add(v1 - v2);
                }
            }
            return ConvexHullGenerator.GenConvexHull(vertices.ToArray());
        }
        
    }

}
