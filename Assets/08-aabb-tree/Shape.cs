using UnityEngine;
using System.Collections.Generic;

namespace Sample08
{
    public abstract class Shape
    {
        public Rigidbody rigidbody { get; private set; }
        public AABB bounds;

        public Color color = Color.green;
        public uint selfMask = 0xffffff;
        public uint collisionMask = 0xffffff;
        public bool isTrigger = false;

        /// <summary>
        /// 将包围盒向外扩展一点，避免AABB树频繁更新
        /// </summary>
        public float boundsExpands = 0.1f;

        public Shape()
        {
        }

        public virtual void updateTransform()
        {
        }

        public abstract bool contains(Vector2 point);

        public abstract Vector2 getFarthestPointInDirection(Vector2 dir, out int index);

        /// <summary>
        /// 获得松散的包围盒。只要在误差范围内，就不需要更新AABB树
        /// </summary>
        /// <returns></returns>
        public AABB getLooseBounds()
        {
            AABB ret = bounds;
            ret.Expand(boundsExpands);
            return ret;
        }

        internal void setRigidbody(Rigidbody body)
        {
            rigidbody = body;
        }

        public virtual void debugDraw(GJKLineTool tool, Color color)
        {

        }

        public virtual void getDebugVertices(List<Vector2> vertices)
        {

        }
    }

    public class CircleShape : Shape
    {
        private Vector2 originCenter;
        private float originRadius;

        public Vector2 center;
        public float radius;

        public CircleShape(Vector2 center, float radius)
        {
            originCenter = center;
            originRadius = radius;
        }

        public override void updateTransform()
        {
            center = rigidbody.matrix.transformPoint(originCenter);
            radius = rigidbody.scale.x * originRadius;

            bounds = new AABB(
                new Vector2(center.x - radius, center.y - radius),
                new Vector2(radius * 2, radius * 2)
            );
        }

        public override bool contains(Vector2 point)
        {
            return GJKTool.sqrDistance(center, point) < radius * radius;
        }

        public override Vector2 getFarthestPointInDirection(Vector2 dir, out int index)
        {
            index = 0;
            return center + dir.normalized * radius;
        }

        public override void debugDraw(GJKLineTool tool, Color color)
        {
            tool.DrawCircle(center, radius, color);
        }

        public override void getDebugVertices(List<Vector2> vertices)
        {
            int n = 36;
            float stepAngle = Mathf.PI * 2 / n;
            for (int i = 0; i < n; ++i)
            {
                Vector2 pos = new Vector2(
                    center.x + Mathf.Cos(i * stepAngle) * radius,
                    center.y + Mathf.Sin(i * stepAngle) * radius
                    );
                vertices.Add(pos);
            }
        }
    }

    public class PolygonShape : Shape
    {
        private List<Vector2> originVertices = new List<Vector2>();
        public List<Vector2> vertices = new List<Vector2>();

        public PolygonShape(Vector2[] vertices)
        {
            if (vertices != null)
            {
                originVertices.AddRange(vertices);
            }
        }

        public override void updateTransform()
        {
            vertices.Clear();
            for (int i = 0; i < originVertices.Count; ++i)
            {
                vertices.Add(rigidbody.matrix.transformPoint(originVertices[i]));
            }

            bounds = new AABB(vertices[0], Vector2.zero);
            foreach (var v in vertices)
            {
                bounds.Merge(v);
            }
        }

        public override bool contains(Vector2 point)
        {
            return GJKTool.contains(vertices, point);
        }

        public override Vector2 getFarthestPointInDirection(Vector2 dir, out int index)
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

        public override void debugDraw(GJKLineTool tool, Color color)
        {
            tool.DrawPolygon(vertices, color);
        }

        public override void getDebugVertices(List<Vector2> vertices)
        {
            vertices.AddRange(this.vertices);
        }
    }

}
