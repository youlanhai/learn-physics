using UnityEngine;
using System.Collections.Generic;

namespace Sample07
{
    /// <summary>
    /// 阵营
    /// </summary>
    public enum Camp
    {
        None,
        Attack,
        Defence,
    }

    public class EntityInitData
    {
        public int shapeIndex;
        public Vector2 position;
        public float rotation;
        public Vector2 scale = new Vector2(1, 1);
        public Vector2 velocity;
        public float angleVelocity;
    }

    /// <summary>
    /// 战斗实体基类
    /// </summary>
    public class Entity
    {
        public int id;
        public Camp camp;
        public Rigidbody rigidbody;
        public Game game;

        public int hp;
        public int hpMax = 1;
        public int attackPoint = 10;
        public int defencePoint = 0;
        public int collisionDamage = 50;

        public EntityInitData initData;

        public virtual void OnEnterWorld()
        {
            hp = hpMax;

            rigidbody = new Rigidbody(1, 1);
            Shape shape = new Shape(rigidbody, game.GetShapeData(initData.shapeIndex));
            rigidbody.shape = shape;

            rigidbody.position = initData.position;
            rigidbody.rotation = initData.rotation;
            rigidbody.scale = initData.scale;
            rigidbody.velocity = initData.velocity;
            rigidbody.angleVelocity = initData.angleVelocity;

            if (camp == Camp.Attack)
            {
                shape.selfMask = 0x01;
                shape.collisionMask = 0x02;
            }
            else if (camp == Camp.Defence)
            {
                shape.selfMask = 0x02;
                shape.collisionMask = ~0x02;
            }
        }

        public virtual void OnLeaveWorld()
        {

        }
        
        public virtual void OnCollisionEnter(CollisionInfo info)
        {

        }

        public virtual void OnCollisionExit(CollisionInfo info)
        {

        }
    }
    
    public class Enemy : Entity
    {

    }

    public class Bullet : Entity
    {
        public Entity owner;
    }

    /// <summary>
    /// 奖励
    /// </summary>
    public class Prize : Entity
    {
        /// <summary>
        /// 补给血量
        /// </summary>
        public int supplyHp;
    }

    public class Player : Entity
    {
        public int score;
        public float moveSpeed = 5;

        public float emitCD = 0.2f;
        public float nextEmitTime = 0;

        public void Update(float deltaTime)
        {
            float s = moveSpeed * deltaTime;
            
            float dx = Input.GetAxisRaw("Horizontal");
            float dy = Input.GetAxisRaw("Vertical");

            rigidbody.position += new Vector2(dx * s, dy * s);
            rigidbody.velocity = Vector2.Lerp(rigidbody.velocity, Vector2.zero, deltaTime);

            if (Input.GetKey(KeyCode.RightControl))
            {
                if (game.gameTime > nextEmitTime)
                {
                    nextEmitTime = game.gameTime + emitCD;
                    EmitBullet();
                }
            }
        }

        void EmitBullet()
        {
            Bullet bullet = new Bullet
            {
                owner = this,
                camp = camp,
                initData = new EntityInitData
                {
                    position = rigidbody.position,
                    scale = new Vector2(0.1f, 0.5f),
                    velocity = new Vector2(0, 5.0f),
                },
            };
            game.AddEntity(bullet);
        }
    }

}
