using UnityEngine;
using System.Collections.Generic;

namespace Sample06
{
    /// 碰撞点信息
    class ContactInfo
    {
        /// 碰撞点
        public Vector2 point;
        /// 穿透方向
        public Vector2 normal;
        /// 穿透深度
        public float penetration;
        /// 碰撞点的hash值，用来累加碰撞结果
        public int hash;

        /// 法线方向的分离力
        public float forceNormal;
        /// 切线方向的分离力
        public float forceTangent;

        public float bias;
        /// 法线方向质量系数
        public float massNormal;
        /// 切线方向的质量系数
        public float massTangent;
    }

    public class Physics
    {
        /// 速度衰减
        public float damping = 0.1f;
        public float allowedPenetration = 0.01f;
        public float biasFactor = 0.1f;
        public int maxIteration = 10;

        public List<Rigidbody> rigidbodies = new List<Rigidbody>();
        public List<Shape> shapes = new List<Shape>();

        public GJK gjk = new GJK();

        enum CollisionStage
        {
            None,
            Enter,
            Stay,
            Exit,
        }

        CollisionStage stage;
        List<ContactInfo> contacts = new List<ContactInfo>();

        public void addRigidbody(Rigidbody body)
        {
            body.physics = this;
            rigidbodies.Add(body);
            shapes.Add(body.shape);

            body.updateTransform();
        }

        public void update(float dt)
        {
            foreach(var body in rigidbodies)
            {
                body.preUpdate(dt);
            }

            doCollisionTest();

            // 可以在这里派发事件
            switch(stage)
            {
                case CollisionStage.Enter:
                    stage = CollisionStage.Stay;
                    Debug.Log("collisionEnter");
                    break;
                case CollisionStage.Exit:
                    stage = CollisionStage.None;
                    Debug.Log("collisionExit");
                    break;
                case CollisionStage.Stay:
                    break;
                default:
                    break;
            }

            if (stage == CollisionStage.Stay)
            {
                doPreSeperation(dt);

                for(int i = 0; i < maxIteration; ++i)
                {
                    doPostSeperation(dt);
                }
            }

            foreach (var body in rigidbodies)
            {
                body.postUpdate(dt);
            }
        }

        void doCollisionTest()
        {
            if (gjk.queryCollision(shapes[0], shapes[1]))
            {
                if (stage == CollisionStage.None)
                {
                    stage = CollisionStage.Enter;
                    getContacts(contacts);
                }
                else
                {
                    List<ContactInfo> newContacts = new List<ContactInfo>();
                    getContacts(newContacts);

                    foreach (var info in newContacts)
                    {
                        ContactInfo old = contacts.Find((v) => v.hash == info.hash);
                        if (old != null)
                        {
                            info.forceNormal = old.forceNormal;
                            info.forceTangent = old.forceTangent;
                        }
                    }

                    contacts = newContacts;
                }
            }
            else
            {
                if (stage == CollisionStage.Stay)
                {
                    stage = CollisionStage.Exit;
                }
            }
        }

        void doPreSeperation(float dt)
        {
            Rigidbody a = rigidbodies[0];
            Rigidbody b = rigidbodies[1];
            foreach (var contact in contacts)
            {
                Vector2 normal = contact.normal;
                Vector2 tangent = new Vector2(-normal.y, normal.x);

                float kNormal = a.getPointMoment(contact.point, normal) + b.getPointMoment(contact.point, normal);
                contact.massNormal = 1.0f / kNormal;

                float kTangent = a.getPointMoment(contact.point, tangent) + b.getPointMoment(contact.point, tangent);
                contact.massTangent = 1.0f / kTangent;

                contact.bias = biasFactor * Mathf.Max(0, contact.penetration - allowedPenetration) / dt;

                Vector2 F = normal * contact.forceNormal + tangent * contact.forceTangent;
                a.applyImpulse(-F);
                a.applyTorqueImpulse(contact.point, -F);

                b.applyImpulse(F);
                b.applyTorqueImpulse(contact.point, F);
            }
        }

        void doPostSeperation(float dt)
        {
            Rigidbody a = rigidbodies[0];
            Rigidbody b = rigidbodies[1];
            float fraction = 1.0f - (a.fraction + b.fraction) * 0.5f;

            foreach (var contact in contacts)
            {
                Vector2 relativeVelocity = a.getPointVelocity(contact.point) - b.getPointVelocity(contact.point);

                Vector2 normal = contact.normal;
                float vn = Vector2.Dot(relativeVelocity, normal);
                float dFn = (vn + contact.bias) * contact.massNormal;
                float oldFn = contact.forceNormal;
                contact.forceNormal = Mathf.Max(oldFn + dFn, 0);
                dFn = contact.forceNormal - oldFn;

                Vector2 tangent = new Vector2(-normal.y, normal.x);
                float vt = Vector2.Dot(relativeVelocity, tangent);
                float dFt = vt * contact.massTangent;
                float maxFt = fraction * contact.forceNormal;
                float oldFt = contact.forceTangent;
                contact.forceTangent = Mathf.Clamp(oldFt + dFt, -maxFt, maxFt);
                dFt = contact.forceTangent - oldFt;

                Vector2 F = normal * dFn + tangent * dFt;
                a.applyImpulse(-F);
                a.applyTorqueImpulse(contact.point, -F);

                b.applyImpulse(F);
                b.applyTorqueImpulse(contact.point, F);
            }
        }

        int genHash(int indexA, int indexB)
        {
            return (indexA << 16) | (indexB & 0xffff);
        }

        void getContacts(List<ContactInfo> list)
        {
            Edge e = gjk.currentEpaEdge;
            ContactInfo a = new ContactInfo
            {
                point = gjk.closestOnA,
                normal = e.normal,
                penetration = e.distance,
                hash = genHash(e.a.indexA, e.a.indexB),
            };

            list.Clear();
            list.Add(a);
        }

    }
}