using UnityEngine;
using System.Collections;

namespace Sample08
{
    /// <summary>
    /// 刚体。表示了一个物体的运动属性
    /// </summary>
    public class Rigidbody
    {
        public int id;
        /// 位移
        public Vector2 position;
        /// 移动速度
        public Vector2 velocity;
        /// 质量
        public float mass;
        /// 质量的倒数。方便除法计算，对于静态物体，可以认为质量无限大，invMass=0，所有作用力相乘结果都是0。
        public float invMass;
        /// 持续作用力。持续影响移动速度
        public Vector2 force;
        /// 脉冲力。仅影响移动速度一次
        public Vector2 forceImpulse;

        /// 旋转角度
        public float rotation;
        /// 角速度。单位是度，计算作用力的时候，切记要转换成弧度。
        public float angleVelocity;
        /// 角动量。相当于旋转质量
        public float inertial;
        /// 角动量倒数。方便除法计算
        public float invInertial;
        /// 扭矩力。持续的影响旋转速度
        public float torque;
        /// 扭矩脉冲力。仅影响旋转速度一次
        public float torqueImpulse;

        /// 摩擦力
        public float fraction = 0.3f;

        public Vector2 scale = Vector2.one;

        /// 坐标变换矩阵。用来变换形状的坐标点。
        public Matrix2D matrix;

        public Shape shape { get; private set; }
        public Physics physics { get; private set; }
        public object entity;

        public bool isStatic { get { return invMass < 0.000001f; } }
        public bool isActive { get; private set; }
        public bool isInPhysics { get { return physics != null; } }

        private float idleTime;

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
            if (isStatic)
            {
                return;
            }

            // 计算作用力. v += a * t; a = F / m
            velocity += (force * invMass + physics.gravity) * dt;
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
            if (!isStatic)
            {
                position += velocity * dt;
                rotation += angleVelocity * dt;
                
                if (canEnterIdle())
                {
                    idleTime += dt;
                }
                else
                {
                    idleTime = 0;
                }
            }
            
            if (isStatic || idleTime > physics.sleepIdleTime)
            {
                sleep();
            }

            updateTransform();
        }

        public void updateTransform()
        {
            matrix.setTransform(position, rotation, scale);
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

        public void sleep()
        {
            velocity.Set(0, 0);
            angleVelocity = 0;

            force.Set(0, 0);
            forceImpulse.Set(0, 0);

            isActive = false;
        }

        // 暂时只支持一个shape，且需要在添加到Physics之前调用
        public void addShape(Shape shape)
        {
            this.shape = shape;
            shape.setRigidbody(this);
        }

        public AABB getBounds()
        {
            AABB bounds = new AABB(position, Vector2.zero);
            bounds.merge(shape.bounds);
            return bounds;
        }

        private bool canEnterIdle()
        {
            float sleepSpeed = physics.sleepSpeed;
            if (velocity.sqrMagnitude > sleepSpeed * sleepSpeed)
            {
                return false;
            }

            AABB bounds = getBounds();
            float r = Mathf.Max(bounds.width, bounds.height) * 0.5f;
            if (r * angleVelocity * Mathf.Deg2Rad > sleepSpeed)
            {
                return false;
            }
            return true;
        }

        internal void onAddToPhysics(Physics physics)
        {
            this.physics = physics;
            isActive = !isStatic;
            physics.addActiveBody(this);
        }

        internal void onRemoveFromPhysics()
        {
            physics = null;
        }

        public void setActive(bool active)
        {
            if (isActive == active)
            {
                return;
            }

            isActive = active;
            if (isInPhysics && active)
            {
                idleTime = 0;
                physics.addActiveBody(this);
            }
        }
    }

}
