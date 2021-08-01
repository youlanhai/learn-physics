using UnityEngine;
using System.Collections.Generic;

namespace Sample07
{
    public class Physics
    {
        /// 速度衰减
        public float damping = 0.1f;
        public float allowedPenetration = 0.01f;
        public float biasFactor = 0.1f;
        public int maxIteration = 10;

        public List<Rigidbody> rigidbodies = new List<Rigidbody>();
        public List<Shape> shapes = new List<Shape>();

        public Dictionary<int, CollisionPair> collisions = new Dictionary<int, CollisionPair>();

        int idCounter = 0;
        int updateIndex = 0;
        public List<int> removeCache = new List<int>();

        public void addRigidbody(Rigidbody body)
        {
            body.physics = this;
            body.id = ++idCounter;
            rigidbodies.Add(body);
            shapes.Add(body.shape);

            body.updateTransform();
        }

        public void update(float dt)
        {
            ++updateIndex;

            foreach (var body in rigidbodies)
            {
                body.preUpdate(dt);
            }

            updateCollisionTest();
            updateSeperation(dt);

            foreach (var body in rigidbodies)
            {
                body.postUpdate(dt);
            }
        }

        void updateCollisionTest()
        {
            for (int i = 0; i < shapes.Count - 1; ++i)
            {
                for (int k = i + 1; k < shapes.Count; ++k)
                {
                    doCollisionTest(shapes[i], shapes[k]);
                }
            }
        }

        void updateSeperation(float dt)
        {
            foreach (var pair in collisions)
            {
                CollisionPair collision = pair.Value;
                if (collision.updateIndex != updateIndex)
                {
                    collision.stage = CollisionStage.Exit;
                }

                // 可以在这里派发事件
                switch (collision.stage)
                {
                    case CollisionStage.Enter:
                        collision.stage = CollisionStage.Stay;
                        Debug.Log("collisionEnter");
                        break;
                    case CollisionStage.Exit:
                        collision.stage = CollisionStage.None;
                        Debug.Log("collisionExit");
                        removeCache.Add(pair.Key);
                        break;
                    case CollisionStage.Stay:
                        break;
                    default:
                        break;
                }

                if (collision.stage == CollisionStage.Stay)
                {
                    doPreSeperation(dt, collision);
                }
            }

            foreach (int hash in removeCache)
            {
                collisions.Remove(hash);
            }
            removeCache.Clear();

            for (int i = 0; i < maxIteration; ++i)
            {
                foreach (var pair in collisions)
                {
                    doPostSeperation(dt, pair.Value);
                }
            }
        }

        void doCollisionTest(Shape shapeA, Shape shapeB)
        {
            if (shapeA.rigidbody.isStatic && shapeB.rigidbody.isStatic)
            {
                return;
            }

            if (shapeA.rigidbody.id > shapeB.rigidbody.id)
            {
                Shape temp = shapeA;
                shapeA = shapeB;
                shapeB = temp;
            }

            if (!shapeA.bounds.Overlaps(shapeB.bounds))
            {
                return;
            }
            
            GJK gjk = new GJK();
            if (!gjk.queryCollision(shapeA, shapeB))
            {
                return;
            }
            
            int hash = genHash(shapeA.rigidbody.id, shapeB.rigidbody.id);

            CollisionPair collision;
            if (!collisions.TryGetValue(hash, out collision))
            {
                collision = new CollisionPair
                {
                    rigidbodyA = shapeA.rigidbody,
                    rigidbodyB = shapeB.rigidbody,
                };
                collisions.Add(hash, collision);
            }
            collision.gjk = gjk;
            collision.updateIndex = updateIndex;

            if (collision.stage == CollisionStage.None)
            {
                collision.stage = CollisionStage.Enter;
                getContacts(gjk, collision.contacts);
            }
            else
            {
                List<ContactInfo> newContacts = new List<ContactInfo>();
                getContacts(gjk, newContacts);

                foreach (var info in newContacts)
                {
                    ContactInfo old = collision.contacts.Find((v) => v.hash == info.hash);
                    if (old != null)
                    {
                        info.forceNormal = old.forceNormal;
                        info.forceTangent = old.forceTangent;
                    }
                }

                collision.contacts = newContacts;
            }
        }

        void doPreSeperation(float dt, CollisionPair collision)
        {
            Rigidbody a = collision.rigidbodyA;
            Rigidbody b = collision.rigidbodyB;
            foreach (var contact in collision.contacts)
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

        void doPostSeperation(float dt, CollisionPair collision)
        {
            Rigidbody a = collision.rigidbodyA;
            Rigidbody b = collision.rigidbodyB;
            float fraction = 1.0f - (a.fraction + b.fraction) * 0.5f;

            foreach (var contact in collision.contacts)
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

        void getContacts(GJK gjk, List<ContactInfo> list)
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
