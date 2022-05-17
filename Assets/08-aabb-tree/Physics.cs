using UnityEngine;
using System.Collections.Generic;

namespace Sample08
{
    public class Physics
    {
        /// 速度衰减
        public float damping = 0.1f;
        public float allowedPenetration = 0.01f;
        public float biasFactor = 0.1f;
        public int maxIteration = 10;

        public Vector2 gravity;
        public float sleepSpeed = 0.01f;
        public float sleepIdleTime = 0.0f;

        public List<Rigidbody> rigidbodies = new List<Rigidbody>();
        private List<Rigidbody> activeBodies = new List<Rigidbody>();
        public List<Shape> shapes = new List<Shape>();

        AABBTree tree = new AABBTree();

        List<Rigidbody> pendingAdds = new List<Rigidbody>();
        List<Rigidbody> pendingRemoves = new List<Rigidbody>();

        public Dictionary<int, CollisionPair> collisions = new Dictionary<int, CollisionPair>();

        int idCounter = 0;
        int updateIndex = 0;
        List<int> removeCache = new List<int>();

        public bool verbose = false;

        public void addRigidbody(Rigidbody body)
        {
            pendingAdds.Add(body);
        }

        public void removeRigidbody(Rigidbody body)
        {
            pendingRemoves.Add(body);
        }

        internal void addActiveBody(Rigidbody body)
        {
            activeBodies.Add(body);
        }

        public void update(float dt)
        {
            ++updateIndex;
            flushPendingBodies();

            // 不能用foreach，因为activeBodies会变化
            // foreach (var body in activeBodies)
            for (int i = 0; i < activeBodies.Count; ++i)
            {
                Rigidbody body = activeBodies[i];
                body.preUpdate(dt);
            }

            queryCollisionPairs();
            updateSeperation(dt);

            for (int i = 0; i < activeBodies.Count; ++i)
            {
                Rigidbody body = activeBodies[i];
                body.postUpdate(dt);
                tree.updateShape(body.shape);
            }
            activeBodies.RemoveAll((Rigidbody body) => !body.isActive);

            flushPendingBodies();
        }

        void queryCollisionPairs()
        {
            for (int i = 0; i < activeBodies.Count; ++i)
            {
                Rigidbody body = activeBodies[i];
                Shape shape = body.shape;
                tree.query(shape.bounds, (AABBNode leaf) =>
                {
                    doCollisionTest(shape, leaf.shape);
                    return false;
                });
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
                if (collision.stage == CollisionStage.Enter)
                {
                    notifyCollisionEvent(collision);
                    collision.stage = CollisionStage.Stay;
                }
                else if (collision.stage == CollisionStage.Exit)
                {
                    collision.stage = CollisionStage.None;
                    removeCache.Add(pair.Key);
                    notifyCollisionEvent(collision);
                }
                
                if (collision.stage == CollisionStage.Stay)
                {
                    notifyCollisionEvent(collision);

                    if (!collision.isTrigger)
                    {
                        doPreSeperation(dt, collision);
                    }
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
                    if (!pair.Value.isTrigger)
                    {
                        doPostSeperation(dt, pair.Value);
                    }
                }
            }
        }

        void doCollisionTest(Shape shapeA, Shape shapeB)
        {
            if (shapeA.rigidbody == shapeB.rigidbody ||
                shapeA.rigidbody.isStatic && shapeB.rigidbody.isStatic)
            {
                return;
            }

            if ((shapeA.selfMask & shapeB.collisionMask) == 0 &&
                (shapeB.selfMask & shapeA.collisionMask) == 0)
            {
                return;
            }

            if (!shapeA.bounds.intersect(shapeB.bounds))
            {
                return;
            }
            
            if (shapeA.rigidbody.id > shapeB.rigidbody.id)
            {
                Shape temp = shapeA;
                shapeA = shapeB;
                shapeB = temp;
            }

            GJK gjk = new GJK();
            if (!gjk.queryCollision(shapeA, shapeB))
            {
                return;
            }

            if (!shapeA.rigidbody.isStatic)
            {
                shapeA.rigidbody.setActive(true);
            }
            if (!shapeB.rigidbody.isStatic)
            {
                shapeB.rigidbody.setActive(true);
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
            collision.isTrigger = shapeA.isTrigger || shapeB.isTrigger;

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

            var contact = collision.contacts[0];
            if (verbose)
            {
                Debug.LogFormat("Collision Test: {0}-{1}. penetration: {2}, contact: {3}: {4}, {5}",
                    collision.rigidbodyA.id, collision.rigidbodyB.id, gjk.penetrationVector, contact.hash, contact.normal, contact.penetration);
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

                if (verbose)
                {
                    Debug.LogFormat("Pre seperation: {0}-{1}. contact: {2}, last force: {3}, bias: {4}", a.id, b.id, contact.hash, F, contact.bias);
                }
            }
        }

        void doPostSeperation(float dt, CollisionPair collision)
        {
            Rigidbody a = collision.rigidbodyA;
            Rigidbody b = collision.rigidbodyB;
            float fraction = 1.0f - (a.fraction + b.fraction) * 0.5f;

            foreach (var contact in collision.contacts)
            {
                // 计算分离力
                Vector2 relativeVelocity = a.getPointVelocity(contact.point) - b.getPointVelocity(contact.point);

                Vector2 normal = contact.normal;
                float vn = Vector2.Dot(relativeVelocity, normal);
                float dFn = (vn + contact.bias) * contact.massNormal;
                float oldFn = contact.forceNormal;
                contact.forceNormal = Mathf.Max(oldFn + dFn, 0);
                dFn = contact.forceNormal - oldFn;

                Vector2 F = normal * dFn;
                a.applyImpulse(-F);
                a.applyTorqueImpulse(contact.point, -F);

                b.applyImpulse(F);
                b.applyTorqueImpulse(contact.point, F);

                // 计算摩擦力
                relativeVelocity = a.getPointVelocity(contact.point) - b.getPointVelocity(contact.point);

                Vector2 tangent = new Vector2(-normal.y, normal.x);
                float vt = Vector2.Dot(relativeVelocity, tangent);
                float dFt = vt * contact.massTangent;
                float maxFt = fraction * contact.forceNormal;
                float oldFt = contact.forceTangent;
                contact.forceTangent = Mathf.Clamp(oldFt + dFt, -maxFt, maxFt);
                dFt = contact.forceTangent - oldFt;

                F = tangent * dFt;
                a.applyImpulse(-F);
                a.applyTorqueImpulse(contact.point, -F);

                b.applyImpulse(F);
                b.applyTorqueImpulse(contact.point, F);

                if (verbose)
                {
                    Debug.LogFormat("Post seperation: {0}-{1}. contact: {2}, fn: {3}, ft: {4}", a.id, b.id, contact.hash, dFn, dFt);
                }
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

        /// <summary>
        /// 添加和删除操作，延迟到物理循环之外进行，避免迭代器失效
        /// </summary>
        void flushPendingBodies()
        {
            foreach (var body in pendingAdds)
            {
                body.id = ++idCounter;
                rigidbodies.Add(body);
                body.onAddToPhysics(this);

                shapes.Add(body.shape);
                tree.addShape(body.shape);

                body.updateTransform();
            }
            pendingAdds.Clear();

            foreach (var body in pendingRemoves)
            {
                rigidbodies.Remove(body);
                activeBodies.Remove(body);
                shapes.Remove(body.shape);
                tree.removeShape(body.shape);
                body.onRemoveFromPhysics();
            }
            pendingRemoves.Clear();
        }

        void notifyCollisionEvent(CollisionPair info)
        {
            ContactInfo contact = info.contacts[0];
            Rigidbody ra = info.rigidbodyA;
            Rigidbody rb = info.rigidbodyB;

            if (ra.entity != null && (ra.shape.collisionMask & rb.shape.selfMask) != 0)
            {
                CollisionInfo data = new CollisionInfo
                {
                    rigidbody = rb,
                    point = contact.point,
                    normal = contact.normal,
                    penetration = contact.penetration,
                };

                notifyCollisionEvent(ra, info.stage, data);
                contact.penetration = data.penetration;
            }
            
            if (rb.entity != null && (rb.shape.collisionMask & ra.shape.selfMask) != 0)
            {
                CollisionInfo data = new CollisionInfo
                {
                    rigidbody = ra,
                    point = contact.point,
                    normal = -contact.normal,
                    penetration = contact.penetration,
                };

                notifyCollisionEvent(rb, info.stage, data);
                contact.penetration = data.penetration;
            }
        }

        void notifyCollisionEvent(Rigidbody rigidbody, CollisionStage stage, CollisionInfo data)
        {
            //Entity entity = rigidbody.entity;
            switch (stage)
            {
                case CollisionStage.Enter:
                {
                    //entity.OnCollisionEnter(data);
                    break;
                }
                case CollisionStage.Exit:
                {
                    //entity.OnCollisionExit(data);
                    break;
                }
                case CollisionStage.Stay:
                {
                    //entity.OnCollisionStay(data);
                    break;
                }
            }
        }

        public bool raycast(Ray2D ray, float maxDistance, out RaycastHit hit)
        {
            return tree.raycast(ray, maxDistance, out hit);
        }
        
        public Shape queryShape(AABB bounds)
        {
            Shape ret = null;
            tree.query(bounds, (AABBNode node) =>
            {
                ret = node.shape;
                return true;
            });
            return ret;
        }

        public Shape pointCast(Vector2 point, float radius = 0.1f)
        {
            AABB bounds = new AABB(point, new Vector2(radius * 2, radius * 2));
            Shape ret = null;
            tree.query(bounds, (AABBNode node) =>
            {
                if (node.shape.contains(point))
                {
                    ret = node.shape;
                    return true;
                }
                return false;
            });
            return ret;
        }

    }
}
