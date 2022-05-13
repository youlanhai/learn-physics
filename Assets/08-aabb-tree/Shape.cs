using UnityEngine;
using System.Collections.Generic;

namespace Sample08
{
    public class Shape
    {
        public Rigidbody rigidbody;
        public AABB bounds;
        public List<Vector2> originVertices = new List<Vector2>();
        public List<Vector2> vertices = new List<Vector2>();

        public Color color = Color.green;
        public uint selfMask = 0xffffff;
        public uint collisionMask = 0xffffff;
        public bool isTrigger = false;

        /// <summary>
        /// 将包围盒向外扩展一点，避免AABB树频繁更新
        /// </summary>
        public float boundsExpands = 0.1f;

        public Shape(Rigidbody rigidbody, Vector2[] vertices)
        {
            this.rigidbody = rigidbody;
            if (vertices != null)
            {
                originVertices.AddRange(vertices);
            }
        }

        public void updateTransform()
        {
            vertices.Clear();
            for(int i = 0; i < originVertices.Count; ++i)
            {
                vertices.Add(rigidbody.matrix.transformPoint(originVertices[i]));
            }

            bounds = new AABB(vertices[0], Vector2.zero);
            foreach(var v in vertices)
            {
                bounds.xMax = Mathf.Max(bounds.xMax, v.x);
                bounds.xMin = Mathf.Min(bounds.xMin, v.x);
                bounds.yMax = Mathf.Max(bounds.yMax, v.y);
                bounds.yMin = Mathf.Min(bounds.yMin, v.y);
            }
        }

        public Vector2 getFarthestPointInDirection(Vector2 dir, out int index)
        {
            float maxDistance = float.MinValue;
            index = 0;
            for (int i = 0; i < vertices.Count; ++i)
            {
                float distance = Vector2.Dot(vertices[i], dir);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    index = i;
                }
            }
            return vertices[index];
        }

        /// <summary>
        /// 获得松散的包围盒。只要在误差范围内，就不需要更新AABB树
        /// </summary>
        /// <returns></returns>
        public AABB GetLooseBounds()
        {
            AABB ret = bounds;
            ret.Expand(boundsExpands);
            return ret;
        }
    }

}
