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
        public float boundsExpands = 0.3f;

        public Shape()
        {
        }

        public virtual void updateTransform()
        {
        }

        public abstract bool contains(Vector2 point);
        public abstract bool raycast(Ray2D ray, out RaycastHit hit);

        public abstract Vector2 getFarthestPointInDirection(Vector2 dir, out int index);

        /// <summary>
        /// 获得松散的包围盒。只要在误差范围内，就不需要更新AABB树
        /// </summary>
        /// <returns></returns>
        public AABB getLooseBounds()
        {
            AABB ret = bounds;
            ret.expand(boundsExpands);
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
            List<Vector2> vertices = new List<Vector2>();
            getDebugVertices(vertices);
            tool.DrawPolygon(vertices, color);
        }

        public override void getDebugVertices(List<Vector2> vertices)
        {
            int n = 20;
            float stepAngle = Mathf.PI * 2 / n;
            float startAngle = rigidbody.rotation * Mathf.Deg2Rad;
            for (int i = 0; i < n; ++i)
            {
                Vector2 pos = new Vector2(
                    center.x + Mathf.Cos(startAngle) * radius,
                    center.y + Mathf.Sin(startAngle) * radius
                    );
                vertices.Add(pos);
                startAngle += stepAngle;
            }
        }

        public override bool raycast(Ray2D ray, out RaycastHit hit)
        {
            hit = new RaycastHit();
            Vector2 e = center - ray.origin;

            // 起点在圆内
            float eLengthSq = e.sqrMagnitude;
            if (eLengthSq <= radius * radius)
            {
                hit.distance = 0;
                hit.normal = ray.direction;
                hit.point = ray.origin;
                hit.shape = this;
                return true;
            }

            float a = Vector2.Dot(e, ray.direction);

            float delta = radius * radius - eLengthSq + a * a;
            // 不相交
            if (delta < 0)
            {
                return false;
            }

            float t = a - Mathf.Sqrt(delta);
            if (t < 0)
            {
                return false;
            }

            hit.distance = t;
            hit.normal = ray.direction;
            hit.point = ray.origin + ray.direction * hit.distance;
            hit.shape = this;
            return true;
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
                bounds.merge(v);
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
        
        public override bool raycast(Ray2D ray, out RaycastHit hit)
        {
            hit = new RaycastHit();

            Vector2 a = ray.direction; // ray.end - ray.start;
            float tMin = float.MaxValue;
            bool bIntersect = false;
    
            for (int i = 0; i < vertices.Count; ++i)
            {
                Vector2 A = vertices[i];
                Vector2 B = vertices[(i + 1) % vertices.Count];
        
                Vector2 b = B - A;
                Vector2 c = A - ray.origin;
        
                float denominator = a.x * b.y - a.y * b.x;
                if (Mathf.Approximately(denominator, 0))
                {
                    continue;
                }
        
                float t1 = (c.x * b.y - c.y * b.x) / denominator;
                float t2 = (c.x * a.y - c.y * a.x) / denominator;
                if (t1 < 0 || t2 < 0 || t2 > 1.0f)
                {
                    continue;
                }
        
                bIntersect = true;
                if (t1 < tMin)
                {
                    tMin = t1;
                }
            }
    
            if (!bIntersect)
            {
                return false;
            }
    
            hit.distance = tMin;
            hit.normal = ray.direction;
            hit.point = ray.origin + ray.direction * hit.distance;
            hit.shape = this;
            return true;
        }
    }


    public struct RaycastHit
    {
        public Shape shape;
        public Vector2 point;
        public Vector2 normal;
        public float distance;
    };
}
