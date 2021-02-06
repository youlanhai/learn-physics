using UnityEngine;
using System.Collections.Generic;

namespace Sample06
{
    public class Shape
    {
        public Rigidbody rigidbody;
        public Rect bounds;
        public List<Vector2> originVertices = new List<Vector2>();
        public List<Vector2> vertices = new List<Vector2>();

        public Shape(Rigidbody rigidbody)
        {
            this.rigidbody = rigidbody;
            rigidbody.shape = this;
        }

        public void updateTransform()
        {
            vertices.Clear();
            for(int i = 0; i < originVertices.Count; ++i)
            {
                vertices.Add(rigidbody.matrix.transformPoint(originVertices[i]));
            }

            bounds = new Rect(vertices[0], Vector2.zero);
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
    }

}