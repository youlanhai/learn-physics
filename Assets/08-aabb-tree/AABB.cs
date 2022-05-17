using UnityEngine;
using System.Collections;

namespace Sample08
{
    public struct AABB
    {
        public float xMin, yMin, xMax, yMax;
        
        public AABB(float x, float y, float width, float height)
        {
            xMin = x;
            yMin = y;
            xMax = x + width;
            yMax = y + height;
        }

        public AABB(Vector2 position, Vector2 size)
        {
            xMin = position.x;
            yMin = position.y;
            xMax = xMin + size.x;
            yMax = yMin + size.y;
        }
        
        public Vector2 min { get { return new Vector2(xMin, yMin); } }
        public Vector2 max { get { return new Vector2(xMax, yMax); } }
        public Vector2 center { get { return new Vector2((xMin + xMax) * 0.5f, (yMin + yMax) * 0.5f); } }

        public float width { get { return xMax - xMin; } }
        public float height { get { return yMax - yMin; } }
        public Vector2 size { get { return new Vector2(width, height); } }
        public Vector2 extends { get { return new Vector2(width * 0.5f, height * 0.5f); } }

        public float area { get { return width * height; } }

        public void merge(Vector2 v)
        {
            xMin = Mathf.Min(xMin, v.x);
            yMin = Mathf.Min(yMin, v.y);
            xMax = Mathf.Max(xMax, v.x);
            yMax = Mathf.Max(yMax, v.y);
        }

        public void merge(AABB b)
        {
            xMin = Mathf.Min(xMin, b.xMin);
            yMin = Mathf.Min(yMin, b.yMin);
            xMax = Mathf.Max(xMax, b.xMax);
            yMax = Mathf.Max(yMax, b.yMax);
        }

        public void expand(float delta)
        {
            xMin = xMin - delta;
            yMin = yMin - delta;
            xMax = xMax + delta;
            yMax = yMax + delta;
        }

        public bool contains(AABB b)
        {
            //return a.xMin <= b.xMin && a.xMax >= b.xMax && a.yMin <= b.xMin && a.yMax >= b.yMax;
            return !(b.xMin < xMin || b.xMax > xMax || b.yMin < yMin || b.yMax > yMax);
        }

        public bool containsc(Vector2 p)
        {
            return !(p.x < xMin || p.x > xMax || p.y < yMin || p.y > yMax);
        }

        public bool intersect(AABB b)
        {
            return !(xMax < b.xMin || yMax < b.yMin || xMin > b.xMax || yMin > b.yMax);
        }

        public float getDistance(Vector2 start, Vector2 end)
        {
            float tMin = 0;
            float tMax = 1;

            Vector2 delta = end - start;

            for (int i = 0; i < 2; ++i)
            {
                if (delta[i] != 0)
                {
                    float dMin = (min[i] - start[i]) / delta[i];
                    float dMax = (max[i] - start[i]) / delta[i];

                    if (dMin > dMax)
                    {
                        float temp = dMin;
                        dMin = dMax;
                        dMax = temp;
                    }

                    tMin = Mathf.Max(tMin, dMin);
                    tMax = Mathf.Min(tMax, dMax);
                }
                else if (start[i] < min[i] || start[i] > max[i]) //起点不在包围盒范围。
                    return float.MaxValue;
            }

            if (tMin > tMax || tMax < 0 || tMin > 1)
            {
                return float.MaxValue;
            }

            return tMin;
        }

        public static AABB operator + (AABB a, AABB b)
        {
            AABB ret = a;
            ret.merge(b);
            return ret;
        }
    }

}
