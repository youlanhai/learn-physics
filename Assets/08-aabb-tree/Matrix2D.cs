using UnityEngine;
using System.Collections;

namespace Sample08
{
    // 3 x 2 行矩阵
    public struct Matrix2D
    {
        public static Matrix2D identity = new Matrix2D(1, 0, 0, 1, 0, 0);

        public float a, b;
        public float c, d;
        public float x, y;

        public Matrix2D(float _a, float _b, float _c, float _d, float _x, float _y)
        {
            a = _a; b = _b;
            c = _c; d = _d;
            x = _x; y = _y;
        }

        public void setIdentity()
        {
            this = identity;
        }

        /** 使用变换参数构造矩阵
         *  @param position 位置
         *  @param rotation 旋转角度。单位: 度
         *  @param scale 缩放
        */
        public void setTransform(Vector2 position, float rotation, Vector2 scale)
        {
            float sinR = Mathf.Sin(rotation * Mathf.Deg2Rad);
            float cosR = Mathf.Cos(rotation * Mathf.Deg2Rad);

            a = cosR * scale.x;  b = -sinR * scale.x;
            c = sinR * scale.y;  d = cosR * scale.y;
            x = position.x; y = position.y;
        }

        public void setTranslate(Vector2 position)
        {
            a = 0; b = 0;
            c = 0; d = 0;
            x = position.x; y = position.y;
        }

        // x` = x * cosR - y * sinR
        // y` = x * sinR + y * cosR
        public void setRotate(float angle)
        {
            float sinR = Mathf.Sin(angle * Mathf.Deg2Rad);
            float cosR = Mathf.Cos(angle * Mathf.Deg2Rad);

            a = cosR; b = -sinR;
            c = sinR; d = cosR;
            x = 0; y = 0;
        }

        public void setScale(Vector2 scale)
        {
            a = scale.x; b = 0;
            c = 0; d = scale.y;
            x = 0; y = 0;
        }

        public void inverseFrom(Matrix2D t)
        {
            float invDet = 1 / (t.a * t.d - t.b * t.c);
            a = t.d * invDet;
            b = -t.b * invDet;
            c = -t.c * invDet;
            d = t.a * invDet;
            x = (t.c * y - t.d * x) * invDet;
            y = (t.b * x - t.a * y) * invDet;
        }

        public void inverse()
        {
            Matrix2D t = this;
            inverseFrom(t);
        }

        public void multiply(Matrix2D t1, Matrix2D t2)
        {
            a = t1.a * t2.a + t1.b * t2.c; b = t1.a * t2.b + t1.b * t2.d;
            c = t1.c * t2.a + t1.d * t2.c; d = t1.c * t2.b + t1.d * t2.d;
            x = t1.x * t2.a + t1.y * t2.c + t2.x; y = t1.x * t2.b + t1.y * t2.d + t2.y;
        }

        // this = this * t;
        public void postMultiply(Matrix2D t)
        {
            Matrix2D t0 = this;
            multiply(t0, t);
        }

        // this = t * this
        public void preMultiply(Matrix2D t)
        {
            Matrix2D t0 = this;
            multiply(t, t0);
        }

        public Vector2 transformPoint(Vector2 p)
        {
            return new Vector2(
                p.x* a + p.y* c + x,
                p.x* b + p.y* d + y);
        }

        public Vector2 transformVector(Vector2 p)
        {
            return new Vector2(
                p.x * a + p.y * c,
                p.x * b + p.y * d);
        }
    }
}
