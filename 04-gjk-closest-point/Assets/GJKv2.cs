using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace gjk2dv2
{

    public class GJK
    {
        public Simplex simplex = new Simplex();
        public Shape shapeA;
        public Shape shapeB;

        public Vector2 closestOnA;
        public Vector2 closestOnB;

        public SupportPoint support(Vector2 dir)
        {
            Vector2 a = shapeA.getFarthestPointInDirection(dir);
            Vector2 b = shapeB.getFarthestPointInDirection(-dir);
            return new SupportPoint
            {
                point = a - b,
                fromA = a,
                fromB = b,
            };
        }

        public Vector2 findFirstDirection()
        {
            Vector2 dir = shapeA.vertices[0] - shapeB.vertices[0];
            if (dir.sqrMagnitude < 0.01f)
            {
                dir = shapeA.vertices[1] - shapeB.vertices[0];
            }
            return dir;
        }

        public bool queryCollision(Shape shapeA, Shape shapeB)
        {
            IEnumerator enumerator = queryStepByStep(shapeA, shapeB);
            while(enumerator.MoveNext())
            {}
            return isCollision;
        }
        
        public Vector2 direction;
        public bool isCollision;

        // 按步骤分解，碰撞检测
        public IEnumerator queryStepByStep(Shape shapeA, Shape shapeB)
        {
            this.shapeA = shapeA;
            this.shapeB = shapeB;

            simplex.clear();
            isCollision = false;

            direction = findFirstDirection();
            simplex.add(support(direction));
            simplex.add(support(-direction));
            yield return null;

            // direction = XO (O是原点, X是原点到ab边上的垂点)
            direction = getClosestPointToOrigin(simplex[0], simplex[1]);

            while (true)
            {
                // 方向接近于0，说明原点就在边上
                if (direction.sqrMagnitude < 0.001f)
                {
                    isCollision = true;
                    break;
                }

                // 取交点到原点方向
                direction = -direction;

                if (!simplex.add(support(direction)))
                {
                    // 已经找不到更多的点了
                    isCollision = false;
                    break;
                }

#if false
                // 这种方式不准确。比如原点就在ab方向，此时c = b, direction = ab, da < dc < 0, dc - da > 0，
                // 这会导致下一次direction变为0，认为是发生了碰撞。
                float dc = Vector2.Dot(simplex[2], direction);
                float da = Vector2.Dot(simplex[0], direction);
                // c在ab上
                if (Mathf.Abs(dc - da) < 0.001f)
                {
                    isCollision = false;
                    break;
                }
#endif
                if (simplex.contains(Vector2.zero))
                {
                    isCollision = true;
                    break;
                }

                yield return null;

                Vector2 p1 = getClosestPointToOrigin(simplex[0], simplex[2]);
                Vector2 p2 = getClosestPointToOrigin(simplex[1], simplex[2]);
                // a点更近，则删除b点
                if (p1.sqrMagnitude < p2.sqrMagnitude)
                {
                    direction = p1;
                    simplex.points.RemoveAt(1);
                }
                else
                {
                    direction = p2;
                    simplex.points.RemoveAt(0);
                }
            }

            ComputeClosetPoint();
        }

        void ComputeClosetPoint()
        {
            /*
             *  L = AB，是Minkowski差集上的一个边，同时构成A、B两点的顶点也来自各自shape的边。
             *  E1 = Aa - Ba，E2 = Ab - Bb
             *  则求两个凸包的最近距离，就演变成了求E1和E2两个边的最近距离。
             *  
             *  设Q点是原点到L的垂点，则有:
             *      L = B - A
             *      Q · L = 0
             *  因为Q是L上的点，可以用r1, r2来表示Q (r1 + r2 = 1)，则有: Q = A * r1 + B * r2
             *      (A * r1 + B * r2) · L = 0
             *  用r2代替r1: r1 = 1 - r2
             *      (A - A * r2 + B * r2) · L = 0
             *      (A + (B - A) * r2) · L = 0
             *      L · A + L · L * r2 = 0
             *      r2 = -(L · A) / (L · L)
             */

            SupportPoint A = simplex.points[0];
            SupportPoint B = simplex.points[1];

            Vector2 L = B.point - A.point;
            float sqrDistanceL = L.sqrMagnitude;
            // support点重合了
            if (sqrDistanceL < 0.0001f)
            {
                closestOnA = closestOnB = A.point;
            }
            else
            {
                float r2 = -Vector2.Dot(L, simplex[0]) / sqrDistanceL;
                r2 = Mathf.Clamp01(r2);
                float r1 = 1.0f - r2;

                closestOnA = A.fromA * r1 + B.fromA * r2;
                closestOnB = A.fromB * r1 + B.fromB * r2;
            }
        }

        static Vector2 getClosestPointToOrigin(Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            Vector2 ao = Vector2.zero - a;
            
            float projection = Vector2.Dot(ab, ao) / ab.sqrMagnitude;
            if (projection < 0)
            {
                return a;
            }
            else if (projection > 1.0f)
            {
                return b;
            }
            else
            {
                return a + ab * projection;
            }
        }

        static Vector2 getPerpendicularToOrigin(Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            Vector2 ao = Vector2.zero - a;

            float projection = Vector2.Dot(ab, ao) / ab.sqrMagnitude;
            return a + ab * projection;
        }
    }

    public class Shape
    {
        public List<Vector2> vertices = new List<Vector2>();

        public Vector2 getFarthestPointInDirection(Vector2 dir)
        {
            float maxDistance = float.MinValue;
            int maxIndex = 0;
            for(int i = 0; i < vertices.Count; ++i)
            {
                float distance = Vector2.Dot(vertices[i], dir);
                if(distance > maxDistance)
                {
                    maxDistance = distance;
                    maxIndex = i;
                }
            }
            return vertices[maxIndex];
        }
    }

    public struct SupportPoint
    {
        public Vector2 point;
        public Vector2 fromA;
        public Vector2 fromB;
    }

    public class Simplex
    {
        public List<SupportPoint> points = new List<SupportPoint>();

        public void clear()
        {
            points.Clear();
        }

        bool almostEqual(Vector2 a, Vector2 b)
        {
            float dx = a.x - b.x;
            float dy = a.y - b.y;
            return dx * dx + dy * dy < 0.0001f;
        }
        
        public bool add(SupportPoint point)
        {
            for(int i = 0; i < points.Count; ++i)
            {
                if(almostEqual(points[i].point, point.point))
                {
                    return false;
                }
            }

            points.Add(point);
            return true;
        }

        public Vector2 getLast()
        {
            return points[points.Count - 1].point;
        }

        public Vector2 this[int i]
        {
            get
            {
                return points[i].point;
            }
        }

        public bool contains(Vector2 point)
        {
            if(points.Count < 3)
            {
                return false;
            }

            // 先计算出内部的方向
            Vector2 ab = this[1] - this[0];
            Vector2 ac = this[2] - this[0];
            int innerSide = whitchSide(ab, ac);

            // 通过判断点是否均在三条边的内侧，来判定单形体是否包含点
            for (int i = 0; i < 3; ++i)
            {
                Vector2 ax = this[(i + 1) % 3] - this[i];
                Vector2 ap = point - this[i];

                int side = whitchSide(ax, ap);
                if(side == 0)
                {
                    return true;
                }

                if (side != innerSide)
                {
                    return false;
                }
            }

            return true;
        }

        static int whitchSide(Vector2 ab, Vector2 ac)
        {
            float cross = ab.x * ac.y - ab.y * ac.x;
            return cross > 0 ? 1 : (cross < 0 ? -1 : 0);
        }
    }
}
