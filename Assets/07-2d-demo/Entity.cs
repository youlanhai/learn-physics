using UnityEngine;
using System.Collections.Generic;

namespace Sample07
{
    /// <summary>
    /// 碰撞掩码
    /// </summary>
    public static class LayerMask
    {
        public const uint NONE = 0;
        public const uint ENEMY = 1 << 0;
        public const uint PLAYER = 1 << 1;
        public const uint BULLET_ENEMY = 1 << 2;
        public const uint BULLET_PLAYER = 1 << 3;
        public const uint WALL = 1 << 32;

        /// <summary>
        /// 敌人不会主动与任何物体发生碰撞
        /// </summary>
        public const uint COLLISION_ENEMY = 0;

        /// <summary>
        /// 敌人子弹会主动碰撞玩家。由子弹来处理伤害逻辑。
        /// </summary>
        public const uint COLLISION_BULLET_ENEMY = PLAYER;

        /// <summary>
        /// 玩家只会主动碰撞敌人和障碍。
        /// </summary>
        public const uint COLLISION_PLAYER = ENEMY | WALL;

        /// <summary>
        /// 子弹只会主动碰撞敌人和障碍
        /// </summary>
        public const uint COLLISION_BULLET_PLAYER = ENEMY | WALL;
    }

    /// <summary>
    /// 初始化Entity需要的数据。可以配置在表里
    /// </summary>
    public class EntityInitData
    {
        public int shapeIndex;
        public Vector2 position;
        public float rotation;
        public Vector2 scale = new Vector2(1, 1);
        public Vector2 velocity;
        public float angleVelocity;
        public uint selfMask;
        public uint collisionMask;
        public EmiterData emiterData;
        public float mass = 1;
        public float inertial = 1;
        public Color color = Color.green;
    }

    /// <summary>
    /// 战斗实体基类
    /// </summary>
    public class Entity
    {
        public Rigidbody rigidbody;
        public Game game;

        /// <summary>
        /// 唯一id
        /// </summary>
        public int id;
        /// <summary>
        /// 血量
        /// </summary>
        public int hp { get; private set; }
        /// <summary>
        /// 血量上限
        /// </summary>
        public int hpMax = 1;
        /// <summary>
        /// 攻击力
        /// </summary>
        public int attackPoint = 2;
        /// <summary>
        /// 防御力
        /// </summary>
        public int defencePoint = 0;
        /// <summary>
        /// 发生碰撞后，对目标造成的伤害
        /// </summary>
        public int collisionDamage = 50;

        /// <summary>
        /// 出生时间
        /// </summary>
        public float bornTime = 0;
        /// <summary>
        /// 存活时间。超时后自动销毁
        /// </summary>
        public float lifeTime = 10;

        /// <summary>
        /// 是否死亡
        /// </summary>
        public bool isDead { get; protected set; }
        /// <summary>
        /// 是否处在世界中
        /// </summary>
        public bool isInWorld { get; protected set; }

        /// <summary>
        /// 初始化需要的数据
        /// </summary>
        public EntityInitData initData;

        public virtual void OnEnterWorld()
        {
            if (isInWorld)
            {
                throw new System.Exception("Entity already in world: " + id);
            }

            isInWorld = true;
            hp = hpMax;
            bornTime = game.gameTime;

            rigidbody = new Rigidbody(initData.mass, initData.inertial);
            rigidbody.entity = this;

            Shape shape = new Shape(rigidbody, game.GetShapeData(initData.shapeIndex));
            shape.selfMask = initData.selfMask;
            shape.collisionMask = initData.collisionMask;
            shape.color = initData.color;

            rigidbody.shape = shape;
            rigidbody.position = initData.position;
            rigidbody.rotation = initData.rotation;
            rigidbody.scale = initData.scale;
            rigidbody.velocity = initData.velocity;
            rigidbody.angleVelocity = initData.angleVelocity;
        }

        public virtual void OnLeaveWorld()
        {
            if (!isInWorld)
            {
                throw new System.Exception("Entity not in world: " + id);
            }

            isInWorld = false;
        }

        /// <summary>
        /// 每帧刷新调用
        /// </summary>
        public virtual void Update(float deltaTime)
        {
            if (game.gameTime - bornTime > lifeTime)
            {
                SetDead(true);
            }
        }
        
        /// <summary>
        /// 碰撞开始回调
        /// </summary>
        public virtual void OnCollisionEnter(CollisionInfo info)
        {

        }

        /// <summary>
        /// 碰撞结束回调
        /// </summary>
        public virtual void OnCollisionExit(CollisionInfo info)
        {

        }

        /// <summary>
        /// 碰撞中回调
        /// </summary>
        public virtual void OnCollisionStay(CollisionInfo info)
        {

        }

        public void SetHp(int v)
        {
            hp = Mathf.Clamp(v, 0, hpMax);
            if (hp == 0)
            {
                SetDead(true);
            }
        }

        public void AddHp(int v)
        {
            SetHp(hp + v);
        }

        public void SetDead(bool dead)
        {
            if (isDead == dead)
            {
                return;
            }
            isDead = dead;
            if (dead)
            {
                OnDead();
            }
            else
            {
                OnReborn();
            }
        }

        protected virtual void OnDead()
        {
            game.DestroyEntity(this);
        }
        
        protected virtual void OnReborn()
        {

        }
    }
    
    /// <summary>
    /// 敌人类
    /// </summary>
    public class Enemy : Entity
    {
        BulletEmiter emiter;
        public int dropHp;
        public int dropAp;

        public override void OnEnterWorld()
        {
            base.OnEnterWorld();

            emiter = new BulletEmiter
            {
                owner = this,
                selfMask = LayerMask.BULLET_ENEMY,
                collisionMask = LayerMask.COLLISION_BULLET_ENEMY,
                data = initData.emiterData,
            };
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            if (rigidbody.shape.bounds.yMax < game.rect.yMin)
            {
                SetDead(true);
                return;
            }

            emiter.Fire();
            emiter.Update(deltaTime);
        }
    }
    
    /// <summary>
    /// 子弹类
    /// </summary>
    public class Bullet : Entity
    {
        public Entity owner;

        public override void OnEnterWorld()
        {
            base.OnEnterWorld();
            rigidbody.shape.isTrigger = true;
        }

        public override void Update(float deltaTime)
        {
            base.Update(deltaTime);

            if (rigidbody.shape.bounds.yMax < game.rect.yMin)
            {
                SetDead(true);
            }
        }

        public override void OnCollisionEnter(CollisionInfo info)
        {
            if (isDead || !isInWorld)
            {
                return;
            }
            
            if (info.entity != null)
            {
                HandleCollisionWithTarget(info.entity);
            }

            SetDead(true);
        }

        void HandleCollisionWithTarget(Entity target)
        {
            if (target.isDead)
            {
                return;
            }

            int damage = Mathf.Max(owner.attackPoint + attackPoint - target.defencePoint, 1);
            target.AddHp(-damage);
            
            if (owner is Player player)
            {
                player.AddScore(1);

                Enemy e = target as Enemy;
                if (target.isDead && e != null)
                {
                    player.AddHp(e.dropHp);
                    player.attackPoint += e.dropAp;
                }
            }
        }
    }
    

    /// <summary>
    /// 奖励
    /// </summary>
    public class Prize : Entity
    {
        /// <summary>
        /// 补给血量
        /// </summary>
        public int supplyHp = 100;
        
        public override void OnCollisionEnter(CollisionInfo info)
        {
            if (isDead || !isInWorld)
            {
                return;
            }

            if (info.entity != null)
            {
                info.entity.AddHp(supplyHp);
            }

            SetDead(true);
        }
    }

    public class Player : Entity
    {
        public int score;
        public float moveSpeed = 5;
        
        public float collisionProtectCD = 3;
        public float nextCollisionTime = 0;

        public void AddScore(int v)
        {
            score += v;
        }

        BulletEmiter emiter;
        int emiterIndex = 1;

        public void OnRestart()
        {
            emiter.Reset();
        }

        public override void OnEnterWorld()
        {
            base.OnEnterWorld();

            emiter = new BulletEmiter
            {
                owner = this,
                selfMask = LayerMask.BULLET_PLAYER,
                collisionMask = LayerMask.COLLISION_BULLET_PLAYER,
                data = initData.emiterData,
            };
        }

        public override void Update(float deltaTime)
        {
            float s = moveSpeed * deltaTime;
            
            float dx = Input.GetAxisRaw("Horizontal");
            float dy = Input.GetAxisRaw("Vertical");

            rigidbody.position += new Vector2(dx * s, dy * s);
            rigidbody.velocity = Vector2.Lerp(rigidbody.velocity, Vector2.zero, deltaTime);

            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                emiter.Fire();
            }
            if (Input.GetKeyDown(KeyCode.J))
            {
                emiterIndex = (emiterIndex + 1) % game.gameData.emiters.Count;
                emiter.data = game.gameData.emiters[emiterIndex];
            }

            emiter.Update(deltaTime);
        }

        protected override void OnDead()
        {
            game.OnGameOver(false);
        }

        public override void OnCollisionEnter(CollisionInfo info)
        {
            if (isDead || !isInWorld)
            {
                return;
            }
        }

        public override void OnCollisionStay(CollisionInfo info)
        {
            Entity target = info.entity;
            // 撞墙了
            if (target == null)
            {
                // 矫正坐标，不要穿到墙里
                rigidbody.position -= info.normal * info.penetration;
                // 将碰撞深度归零，不要让物理引擎产生作用力
                info.penetration = 0;
            }
            // 被Enemy撞击了
            else
            {
                HandleCollisionWithTarget(target);
            }
        }

        void HandleCollisionWithTarget(Entity target)
        {
            if (target.isDead)
            {
                return;
            }

            // 先检查碰撞CD保护
            if (game.gameTime < nextCollisionTime)
            {
                return;
            }

            nextCollisionTime += collisionProtectCD;

            // 对敌人进行碰撞伤害
            int damage = Mathf.Max(collisionDamage - target.defencePoint, 1);
            target.AddHp(-damage);
            if (target.isDead)
            {
                ++score;
            }

            // 对自己进行碰撞伤害
            damage = Mathf.Max(target.collisionDamage - defencePoint, 1);
            AddHp(-damage);
        }
    }

}
