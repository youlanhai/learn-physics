using UnityEngine;
using System.Collections.Generic;

namespace Sample07
{
    /// 碰撞点信息
    public class ContactInfo
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

    public enum CollisionStage
    {
        None,
        Enter,
        Stay,
        Exit,
    }

    public class CollisionPair
    {
        public int updateIndex;
        public Rigidbody rigidbodyA;
        public Rigidbody rigidbodyB;
        public bool isTrigger;

        public CollisionStage stage;
        public List<ContactInfo> contacts = new List<ContactInfo>();

        public GJK gjk;
    }

    public class CollisionInfo
    {
        public Rigidbody rigidbody;
        public Entity entity;
        public Vector2 point;
        public Vector2 normal;
        public float penetration;
    }
}
