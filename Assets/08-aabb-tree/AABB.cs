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

        public float width { get { return xMax - xMin; } }
        public float height { get { return yMax - yMin; } }

        public float area { get { return width * height; } }

        public void Merge(Vector2 v)
        {
            xMin = Mathf.Min(xMin, v.x);
            yMin = Mathf.Min(yMin, v.y);
            xMax = Mathf.Max(xMax, v.x);
            yMax = Mathf.Max(yMax, v.y);
        }

        public void Merge(AABB b)
        {
            xMin = Mathf.Min(xMin, b.xMin);
            yMin = Mathf.Min(yMin, b.yMin);
            xMax = Mathf.Max(xMax, b.xMax);
            yMax = Mathf.Max(yMax, b.yMax);
        }

        public void Expand(float delta)
        {
            xMin = xMin - delta;
            yMin = yMin - delta;
            xMax = xMax + delta;
            yMax = yMax + delta;
        }

        public bool Contains(AABB b)
        {
            //return a.xMin <= b.xMin && a.xMax >= b.xMax && a.yMin <= b.xMin && a.yMax >= b.yMax;
            return !(b.xMin < xMin || b.xMax > xMax || b.yMin < yMin || b.yMax > yMax);
        }

        public bool Overlaps(AABB b)
        {
            return !(xMax < b.xMin || yMax < b.yMin || xMin > b.xMax || yMin > b.yMax);
        }

        public static AABB operator + (AABB a, AABB b)
        {
            return new AABB
            {
                xMin = Mathf.Min(a.xMin, b.xMin),
                yMin = Mathf.Min(a.yMin, b.yMin),
                xMax = Mathf.Max(a.xMax, b.xMax),
                yMax = Mathf.Max(a.yMax, b.yMax),
            };
        }
    }
}
