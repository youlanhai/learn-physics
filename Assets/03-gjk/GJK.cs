using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Sample03
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
            yield return null;

            direction = findFirstDirection();
            simplex.add(support(direction));
            yield return null;

            direction = -direction;
            for(int i = 0; i < maxIterCount; ++i)
            {
                // 方向接近于0，说明原点就在边上
                if(direction.sqrMagnitude < epsilon)
                {
                    isCollision = true;
                    break;
                }

                simplex.add(support(direction));
                yield return null;

                // 沿着dir的方向，已经找不到更近的点了。
                if (Vector2.Dot(simplex.getLast(), direction) < epsilon)
                {
                    isCollision = false;
                    break;
                }

                // 单形体包含原点了
                if (simplex.contains(Vector2.zero))
                {
                    isCollision = true;
                    break;
                }

                direction = findNextDirection();
            }
        }


        public Vector2 support(Vector2 dir)
        {
            Vector2 a = shapeA.getFarthestPointInDirection(dir);
            Vector2 b = shapeB.getFarthestPointInDirection(-dir);
            return a - b;
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
                Vector2 crossPoint = GJKTool.getPerpendicularToOrigin(simplex.get(0), simplex.get(1));
                // 取靠近原点方向的向量
                return Vector2.zero - crossPoint;
            }
            else if (simplex.count() == 3)
            {
                Vector2 crossOnCA = GJKTool.getPerpendicularToOrigin(simplex.get(2), simplex.get(0));
                Vector2 crossOnCB = GJKTool.getPerpendicularToOrigin(simplex.get(2), simplex.get(1));

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

        public void clear()
        {
            points.Clear();
        }

        public int count()
        {
            return points.Count;
        }

        public Vector2 get(int i)
        {
            return points[i];
        }

        public void add(Vector2 point)
        {
            points.Add(point);
        }

        public void remove(int index)
        {
            points.RemoveAt(index);
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
}
