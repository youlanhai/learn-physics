using UnityEngine;
using System.Collections;

namespace Sample06
{
    /// <summary>
    /// 刚体。表示了一个物体的运动属性
    /// </summary>
    public class Rigidbody
    {
        public Vector2 position;
        public float rotation;

        public float mass;
        public float invMass;
        public Vector2 velocity;
        /// 持续作用力
        public Vector2 force;
        /// 脉冲力
        public Vector2 forceImpulse;

        /// 角动量。相当于旋转质量
        public float inertial;
        public float invInertial;
        /// 角速度
        public float angleVelocity;
        /// 扭矩力。影响旋转
        public float torque;
        /// 扭矩脉冲力
        public float torqueImpulse;

        public Matrix2D matrix;

        public Shape shape;
        public Physics physics;

        public Rigidbody(float mass, float inertial)
        {
            setMass(mass);
            setInertial(inertial);
        }

        public void setMass(float m)
        {
            mass = m;
            if (m >= float.PositiveInfinity)
            {
                invMass = 0;
            }
            else
            {
                invMass = 1.0f / m;
            }
        }

        public void setInertial(float i)
        {
            inertial = i;
            if (i >= float.PositiveInfinity)
            {
                invInertial = 0;
            }
            else
            {
                invInertial = 1.0f / i;
            }
        }

        public void preUpdate(float dt)
        {
            // 计算作用力. v += a * t; a = F / m
            velocity += force * invMass * dt;
            angleVelocity += torque * invInertial * dt;

            // 计算脉冲力
            velocity += forceImpulse * invMass;
            angleVelocity += torqueImpulse * invInertial;

            forceImpulse = Vector2.zero;
            torqueImpulse = 0;

            // 计算速度衰减
            velocity *= 1.0f - physics.damping * dt;
            angleVelocity *= 1.0f - physics.damping * dt;
        }

        public void postUpdate(float dt)
        {
            position += velocity * dt;
            rotation += angleVelocity * dt;

            updateTransform();
        }

        public void updateTransform()
        {
            matrix.setTransform(position, rotation, Vector2.one);
            shape.updateTransform();
        }

        /// 获得点关于法线方向的动量
        public float getPointMoment(Vector2 point, Vector2 normal)
        {
            Vector2 r = point - position;
            float rn = Vector2.Dot(r, normal);
            return invMass + invInertial * (r.sqrMagnitude - rn * rn);
        }

        /// 获得点的速度
        public Vector2 getPointVelocity(Vector2 point)
        {
            // 点的旋转速度为：每秒转过的弧度L = (angle / 360) * 2 PI * R。
            // 点的旋转方向为：r的切线方向
            Vector2 r = point - position;
            return velocity + new Vector2(-r.y, r.x) * angleVelocity * Mathf.Deg2Rad;
        }

        public void applyImpulse(Vector2 force)
        {
            velocity += force * invMass;
        }

        public void applyTorqueImpulse(Vector2 point, Vector2 torque)
        {
            Vector2 radius = point - position;
            angleVelocity += GJKTool.cross(radius, torque) * Mathf.Rad2Deg * invInertial;
        }
    }

}
