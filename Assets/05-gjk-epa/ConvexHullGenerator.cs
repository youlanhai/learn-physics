using UnityEngine;
using System.Collections.Generic;

namespace Sample05
{
    // 计算凸包。
    // Graham扫描法。http://blog.csdn.net/bone_ace/article/details/46239187
    public static class ConvexHullGenerator
    {
        public static float epsilon = 0.00001f;

        public static List<Vector2> GenConvexHull(Vector2[] vertices)
        {
            int count = vertices.Length;
            if (count < 3)
            {
                return null;
            }

            // 找到最低点，作为原点
            int bottomIndex = 0;
            for (int i = 1; i < count; ++i)
            {
                if (vertices[i].y < vertices[bottomIndex].y)
                {
                    bottomIndex = i;
                }
            }

            // 把最低点移出数组
            Vector2 origin = vertices[bottomIndex];
            --count;
            if (bottomIndex != count)
            {
                vertices[bottomIndex] = vertices[count];
            }

            // 将任意点与原点相连，并计算斜率的角度
            float[] angles = new float[count];
            for (int i = 0; i < count; ++i)
            {
                Vector2 delta = vertices[i] - origin;
                angles[i] = Mathf.Atan2(delta.y, delta.x);
            }

            // 将角度从小到大排序
            System.Array.Sort(angles, vertices, 0, count);

            // 扫描算法从最小斜率的点开始。也就是从最右侧的点开始，依次向做扫描。

            List<Vector2> result = new List<Vector2>(vertices.Length);
            result.Add(origin);
            result.Add(vertices[0]);
            for (int i = 1; i < count;)
            {
                Vector2 p0 = result[result.Count - 2];
                Vector2 p1 = result[result.Count - 1];
                Vector2 p2 = vertices[i];

                Vector2 a = p1 - p0;
                Vector2 b = p2 - p0;

                float cross = a.x * b.y - a.y * b.x;
                if (cross < -epsilon)
                {
                    // 上一个点(p1)不是凸点
                    result.RemoveAt(result.Count - 1);
                    if (i + 1 == count)
                    {
                        result.Add(p2);
                        ++i;
                    }
                }
                else if (cross > epsilon)
                {
                    // 可能是凸点
                    ++i;
                    result.Add(p2);
                }
                else
                {
                    // 在同一条线上，取最远的
                    if (b.sqrMagnitude > a.sqrMagnitude)
                    {
                        result.RemoveAt(result.Count - 1);
                        if (result.Count == 1 || i + 1 == count)
                        {
                            result.Add(p2);
                            ++i;
                        }
                    }
                    else
                    {
                        ++i;
                    }
                }
            }
            
            return result;
        }
    }
}
