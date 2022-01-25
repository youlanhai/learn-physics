using UnityEngine;
using System.Collections.Generic;

namespace Sample08
{

    public static class GJKTool
    {
        public static float cross(Vector2 a, Vector2 b)
        {
            return a.x * b.y - a.y * b.x;
        }

        public static float sqrDistance(Vector2 a, Vector2 b)
        {
            float dx = a.x - b.x;
            float dz = a.y - b.y;
            return dx * dx + dz * dz;
        }

        /// 判读点c在ab的哪一侧
        public static int whitchSide(Vector2 a, Vector2 b, Vector2 c)
        {
            Vector2 ab = b - a;
            Vector2 ac = c - a;
            float cross = ab.x * ac.y - ab.y * ac.x;
            return cross > 0 ? 1 : (cross < 0 ? -1 : 0);
        }

        /// 获得原点到线段ab的最近点。最近点可以是垂点，也可以是线段的端点。
        public static Vector2 getClosestPointToOrigin(Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            Vector2 ao = Vector2.zero - a;

            float sqrLength = ab.sqrMagnitude;

            // ab点重合了
            if(sqrLength < float.Epsilon)
            {
                return a;
            }

            float projection = Vector2.Dot(ab, ao) / sqrLength;
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

        /// 获得原点到直线ab的垂点
        public static Vector2 getPerpendicularToOrigin(Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            Vector2 ao = Vector2.zero - a;

            float sqrLength = ab.sqrMagnitude;
            if (sqrLength < float.Epsilon)
            {
                return Vector2.zero;
            }

            float projection = Vector2.Dot(ab, ao) / sqrLength;
            return a + ab * projection;
        }

        public static bool contains(List<Vector2> points, Vector2 point)
        {
            int n = points.Count;
            if (n < 3)
            {
                return false;
            }

            // 先计算出内部的方向
            int innerSide = whitchSide(points[0], points[1], points[2]);

            // 通过判断点是否均在三条边的内侧，来判定单形体是否包含点
            for (int i = 0; i < n; ++i)
            {
                int iNext = (i + 1) % n;
                int side = whitchSide(points[i], points[iNext], point);

                if (side == 0) // 在边界上
                {
                    return true;
                }

                if (side != innerSide) // 在外部
                {
                    return false;
                }
            }

            return true;
        }

        public static Rect mergeRect(Rect a, Rect b)
        {
            Rect ret = a;
            ret.xMin = Mathf.Min(a.xMin, b.xMin);
            ret.yMin = Mathf.Min(a.yMin, b.yMin);
            ret.xMax = Mathf.Max(a.xMax, b.xMax);
            ret.yMax = Mathf.Max(a.yMax, b.yMax);
            return a;
        }

        public static Rect expandRect(Rect r, float delta)
        {
            return new Rect(r.xMin - delta, r.yMin - delta, r.width + delta * 2, r.height + delta * 2);
        }

        public static bool containsRect(Rect a, Rect b)
        {
            //return a.xMin <= b.xMin && a.xMax >= b.xMax && a.yMin <= b.xMin && a.yMax >= b.yMax;
            return !(b.xMin < a.xMin || b.xMax > a.xMax || b.yMin < a.yMin || b.yMax > a.yMax);
        }
    }
}
