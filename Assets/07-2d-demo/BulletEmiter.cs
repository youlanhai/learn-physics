using UnityEngine;

namespace Sample07
{
    public class EmiterData
    {
        /// <summary>
        /// 发射CD
        /// </summary>
        public float emitCD = 1;

        /// <summary>
        /// 允许角度偏移计算
        /// </summary>
        public bool enableAngleRange;

        /// <summary>
        /// 允许位置偏移计算
        /// </summary>
        public bool enablePositionRange;

        /// <summary>
        /// 允许反向折回
        /// </summary>
        public bool enableReverse;

        /// <summary>
        /// 每次发射的组数。一组又包括若干个子弹
        /// </summary>
        public int groupCount = 1;
        /// <summary>
        /// 组之间的间隔时间
        /// </summary>
        public float groupTimeDelta;

        /// <summary>
        /// 发射角度范围起始点
        /// </summary>
        public float angleBegin = 0;
        /// <summary>
        /// 发射角度范围终点
        /// </summary>
        public float angleEnd = 360;

        public Vector2 positionBegin;
        public Vector2 positionEnd;

        public Vector2 bulletVelocity = new Vector2(0, 1.0f);

        #region 以下是单次发射的参数
        /// <summary>
        /// 单次发射的子弹数量
        /// </summary>
        public int bulletCount = 1;
        /// <summary>
        /// 每个子弹的时间间隔
        /// </summary>
        public float bulletTimeDelta;
        /// <summary>
        /// 每个子弹的角度间隔
        /// </summary>
        public float bulletAngleDelta;
        /// <summary>
        /// 每个子弹的位置偏移
        /// </summary>
        public Vector2 bulletPositionDelta;
        #endregion
    }

    /// <summary>
    /// 子弹发射器
    /// </summary>
    public class BulletEmiter
    {
        public Entity owner;
        public EmiterData data;

        public uint selfMask;
        public uint collisionMask;
        
        /// <summary>
        /// 是否正在发射中
        /// </summary>
        bool isEmiting;

        /// <summary>
        /// 下次可发射子弹的时间. 受发射CD影响
        /// </summary>
        float nextEmitTime;

        /// <summary>
        /// 当前发射组索引
        /// </summary>
        int groupIndex = 0;
        /// <summary>
        /// 当前组内的子弹索引
        /// </summary>
        int bulletIndex = 0;

        /// <summary>
        /// 折回的正负
        /// </summary>
        float reverseSign = 1;

        /// <summary>
        /// 发射器当前偏移位置
        /// </summary>
        Vector2 position;

        /// <summary>
        /// 发射器当前偏移角度
        /// </summary>
        float angle;

        public void Reset()
        {
            isEmiting = false;
            nextEmitTime = 0;
        }

        public void Update(float deltaTime)
        {
            if (isEmiting)
            {
                OnEmitUpdate();
            }
        }

        public void Fire()
        {
            if (isEmiting)
            {
                return;
            }

            if (owner.game.gameTime > nextEmitTime)
            {
                OnEmitStart();
            }
        }

        void OnEmitStart()
        {
            isEmiting = true;
            nextEmitTime = owner.game.gameTime;

            groupIndex = 0;

            bulletIndex = 0;
            position = data.positionBegin;
            angle = data.angleBegin;
            reverseSign = 1;
        }

        void OnEmitFinish()
        {
            isEmiting = false;
            nextEmitTime += data.emitCD;
        }

        void OnEmitUpdate()
        {
            float gameTime = owner.game.gameTime;
            for (; bulletIndex < data.bulletCount && nextEmitTime < gameTime; ++bulletIndex)
            {
                EmitOneBullet();
                nextEmitTime += data.bulletTimeDelta;

                AdvanceAngle();
                AdvancePosition();
            }

            // 当前组还没有发射完. 单次发射时间用完了
            if (bulletIndex < data.bulletCount)
            {
                return;
            }

            bulletIndex = 0;
            position = data.positionBegin;
            angle = data.angleBegin;
            reverseSign = 1;

            ++groupIndex;

            // 组还没有完
            if (groupIndex < data.groupCount)
            {
                nextEmitTime += data.groupTimeDelta;
            }
            // 全部组都发射完毕了
            else
            {
                OnEmitFinish();
            }
        }

        void EmitOneBullet()
        {
            Vector2 pos = owner.rigidbody.position + position;
            float rot = owner.rigidbody.rotation + angle;
            
            Matrix2D mat = new Matrix2D();
            mat.setRotate(rot);
            Vector2 velocity = mat.transformVector(data.bulletVelocity);

            Bullet bullet = new Bullet
            {
                owner = owner,
                initData = new EntityInitData
                {
                    position = pos,
                    rotation = rot,
                    scale = new Vector2(0.1f, 0.5f),
                    velocity = owner.rigidbody.velocity + velocity,
                    selfMask = selfMask,
                    collisionMask = collisionMask,
                    mass = 0.001f,
                    inertial = 0.001f,
                    color = owner.initData.color,
                },
            };
            owner.game.AddEntity(bullet);
        }

        void AdvanceAngle()
        {
            float next = angle + data.bulletAngleDelta * reverseSign;
            if (!data.enableAngleRange)
            {
                angle = next;
                return;
            }

            if (reverseSign > 0 && next > data.angleEnd)
            {
                if (data.enableReverse)
                {
                    reverseSign = -reverseSign;
                    angle = data.angleEnd;
                }
                else
                {
                    angle = data.angleBegin;
                }
            }
            else if (reverseSign < 0 && next < data.angleBegin)
            {
                reverseSign = -reverseSign;
                angle = data.angleBegin;
            }
            else
            {
                angle = next;
            }
        }

        void AdvancePosition()
        {
            Vector2 next = position + data.bulletPositionDelta * reverseSign;
            if (!data.enablePositionRange)
            {
                position = next;
                return;
            }

            float sqrDistance = (data.positionEnd - data.positionBegin).sqrMagnitude;

            if (reverseSign > 0 && (next - data.positionBegin).sqrMagnitude > sqrDistance)
            {
                if (data.enableReverse)
                {
                    reverseSign = -reverseSign;
                    position = data.positionEnd;
                }
                else
                {
                    position = data.positionBegin;
                }
            }
            else if (reverseSign < 0 && (next - data.positionEnd).sqrMagnitude > sqrDistance)
            {
                reverseSign = -reverseSign;
                position = data.positionBegin;
            }
            else
            {
                position = next;
            }
        }

    }
}