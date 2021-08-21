using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Sample05
{

    public class GJK
    {
        public Simplex simplex = new Simplex();
        public Shape shapeA;
        public Shape shapeB;
        /// 最大迭代次数
        public int maxIterCount = 10;
        /// 浮点数误差。
        public float epsilon = 0.00001f;

        /// 当前support使用的方向
        public Vector2 direction;
        public bool isCollision;

        // 最近点
        public Vector2 closestOnA;
        public Vector2 closestOnB;

        public SimplexEdge simplexEdge = new SimplexEdge();
        public Vector2 penetrationVector;
        public Edge currentEpaEdge;

        public bool queryCollision(Shape shapeA, Shape shapeB)
        {
            IEnumerator enumerator = queryStepByStep(shapeA, shapeB);
            while(enumerator.MoveNext())
            {}
            return isCollision;
        }
        
        /// 按步骤分解，碰撞检测
        public IEnumerator queryStepByStep(Shape shapeA, Shape shapeB)
        {
            this.shapeA = shapeA;
            this.shapeB = shapeB;

            simplex.clear();
            isCollision = false;
            direction = Vector2.zero;

            closestOnA = Vector2.zero;
            closestOnB = Vector2.zero;

            simplexEdge.clear();
            currentEpaEdge = null;
            penetrationVector = Vector2.zero;
            yield return null;

            direction = findFirstDirection();
            simplex.add(support(direction));
            simplex.add(support(-direction));
            yield return null;

            direction = -GJKTool.getClosestPointToOrigin(simplex.get(0), simplex.get(1));
            for(int i = 0; i < maxIterCount; ++i)
            {
                // 方向接近于0，说明原点就在边上
                if(direction.sqrMagnitude < epsilon)
                {
                    isCollision = true;
                    break;
                }
                
                SupportPoint p = support(direction);
                // 新点与之前的点重合了。也就是沿着dir的方向，已经找不到更近的点了。
                if (GJKTool.sqrDistance(p.point, simplex.get(0)) < epsilon ||
                    GJKTool.sqrDistance(p.point, simplex.get(1)) < epsilon)
                {
                    isCollision = false;
                    break;
                }

                simplex.add(p);
                yield return null;

                // 单形体包含原点了
                if (simplex.contains(Vector2.zero))
                {
                    isCollision = true;
                    break;
                }

                direction = findNextDirection();
            }

            yield return null;

            if (!isCollision)
            {
                ComputeClosetPoint();
            }
            else
            {
                // 仅保留距离原点最近的一条边，避免浮点数误差引起原点落在了边上，造成无法计算出该边的法线方向
                if (simplex.count() > 2)
                {
                    findNextDirection();
                }

                // EPA算法计算穿透向量
                simplexEdge.initEdges(simplex);
                
                for (int i = 0; i < maxIterCount; ++i)
                {
                    Edge e = simplexEdge.findClosestEdge();
                    currentEpaEdge = e;
                    penetrationVector = e.normal * e.distance;
                    yield return null;

                    Vector2 point = support(e.normal).point;
                    float distance = Vector2.Dot(point, e.normal);
                    if (distance - e.distance < epsilon)
                    {
                        break;
                    }

                    simplexEdge.insertEdgePoint(e, point);
                    yield return null;
                }
            }
        }


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
            if (dir.sqrMagnitude < epsilon) // 避免首次取到的点距离为0
            {
                dir = shapeA.vertices[1] - shapeB.vertices[0];
            }
            return dir;
        }

        public Vector2 findNextDirection()
        {
            if (simplex.count() == 2)
            {
                Vector2 crossPoint = GJKTool.getClosestPointToOrigin(simplex.get(0), simplex.get(1));
                // 取靠近原点方向的向量
                return Vector2.zero - crossPoint;
            }
            else if (simplex.count() == 3)
            {
                Vector2 crossOnCA = GJKTool.getClosestPointToOrigin(simplex.get(2), simplex.get(0));
                Vector2 crossOnCB = GJKTool.getClosestPointToOrigin(simplex.get(2), simplex.get(1));

                // 保留距离原点近的，移除较远的那个点
                if (crossOnCA.sqrMagnitude < crossOnCB.sqrMagnitude)
                {
                    simplex.remove(1);
                    return Vector2.zero - crossOnCA;
                }
                else
                {
                    simplex.remove(0);
                    return Vector2.zero - crossOnCB;
                }
            }
            else
            {
                // 不应该执行到这里
                return new Vector2(0, 0);
            }
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

            SupportPoint A = simplex.getSupport(0);
            SupportPoint B = simplex.getSupport(1);

            Vector2 L = B.point - A.point;
            float sqrDistanceL = L.sqrMagnitude;
            // support点重合了
            if (sqrDistanceL < 0.0001f)
            {
                closestOnA = closestOnB = A.point;
            }
            else
            {
                float r2 = -Vector2.Dot(L, A.point) / sqrDistanceL;
                r2 = Mathf.Clamp01(r2);
                float r1 = 1.0f - r2;

                closestOnA = A.fromA * r1 + B.fromA * r2;
                closestOnB = A.fromB * r1 + B.fromB * r2;
            }
        }
    }

    public struct SupportPoint
    {
        public Vector2 point;
        public Vector2 fromA;
        public Vector2 fromB;
    }

    public class Edge
    {
        public Vector2 a;
        public Vector2 b;
        public Vector2 normal;
        public float distance;
        public int index;
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

    public class Simplex
    {
        public List<Vector2> points = new List<Vector2>();
        public List<Vector2> fromA = new List<Vector2>();
        public List<Vector2> fromB = new List<Vector2>();

        public void clear()
        {
            points.Clear();
            fromA.Clear();
            fromB.Clear();
        }

        public int count()
        {
            return points.Count;
        }

        public Vector2 get(int i)
        {
            return points[i];
        }

        public SupportPoint getSupport(int i)
        {
            return new SupportPoint
            {
                point = points[i],
                fromA = fromA[i],
                fromB = fromB[i],
            };
        }

        public void add(SupportPoint point)
        {
            points.Add(point.point);
            fromA.Add(point.fromA);
            fromB.Add(point.fromB);
        }

        public void remove(int index)
        {
            points.RemoveAt(index);
            fromA.RemoveAt(index);
            fromB.RemoveAt(index);
        }

        public Vector2 getLast()
        {
            return points[points.Count - 1];
        }

        public bool contains(Vector2 point)
        {
            return GJKTool.contains(points, point);
        }
    }

    public class SimplexEdge
    {
        public List<Edge> edges = new List<Edge>();

        public void clear()
        {
            edges.Clear();
        }

        public void initEdges(Simplex simplex)
        {
            edges.Clear();

            if (simplex.count() != 2)
            {
                throw new System.Exception("simplex point count must be 2!");
            }

            edges.Add(createInitEdge(simplex.get(0), simplex.get(1)));
            edges.Add(createInitEdge(simplex.get(1), simplex.get(0)));

            updateEdgeIndex();
        }

        public Edge findClosestEdge()
        {
            float minDistance = float.MaxValue;
            Edge ret = null;
            foreach (var e in edges)
            {
                if (e.distance < minDistance)
                {
                    ret = e;
                    minDistance = e.distance;
                }
            }
            return ret;
        }

        public void insertEdgePoint(Edge e, Vector2 point)
        {
            Edge e1 = createEdge(e.a, point);
            edges[e.index] = e1;

            Edge e2 = createEdge(point, e.b);
            edges.Insert(e.index + 1, e2);

            updateEdgeIndex();
        }

        public void updateEdgeIndex()
        {
            for (int i = 0; i < edges.Count; ++i)
            {
                edges[i].index = i;
            }
        }

        public Edge createEdge(Vector2 a, Vector2 b)
        {
            Edge e = new Edge();
            e.a = a;
            e.b = b;

            e.normal = GJKTool.getPerpendicularToOrigin(a, b);
            float lengthSq = e.normal.sqrMagnitude;
            if (lengthSq > float.Epsilon)
            {
                e.distance = Mathf.Sqrt(lengthSq);
                // 单位化边
                e.normal *= 1.0f / e.distance;
            }
            else
            {
                // 用数学的方法来得到直线的垂线，但是方向可能是错的
                Vector2 v = a - b;
                v.Normalize();
                e.normal = new Vector2(-v.y, v.x);
            }
            return e;
        }

        Edge createInitEdge(Vector2 a, Vector2 b)
        {
            Edge e = new Edge
            {
                a = a,
                b = b,
            };

            Vector3 perp = GJKTool.getPerpendicularToOrigin(a, b);
            e.distance = perp.magnitude;

            // 用数学的方法来得到直线的垂线
            // 方向可以随便取，刚好另外一边是反着来的
            Vector2 v = a - b;
            v.Normalize();
            e.normal = new Vector2(-v.y, v.x);

            return e;
        }
    }
}
