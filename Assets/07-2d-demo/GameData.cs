using UnityEngine;
using System.Collections.Generic;

namespace Sample07
{

    public class GameData
    {
        /// <summary>
        /// 3s发射1颗子弹
        /// </summary>
        public EmiterData line1 = new EmiterData
        {
            emitCD = 3,
        };

        /// <summary>
        /// 密集竖直双弹道
        /// </summary>
        public EmiterData line2 = new EmiterData
        {
            emitCD = 0.1f,
            positionBegin = new Vector2(-0.5f, 0),
            groupCount = 3,
            groupTimeDelta = 0.1f,
            bulletCount = 2,
            bulletVelocity = new Vector2(0, 10.0f),
            bulletPositionDelta = new Vector2(1.0f, 0),
        };

        /// <summary>
        /// 太阳辐射
        /// </summary>
        public EmiterData sun = new EmiterData
        {
            emitCD = 3.0f,

            groupCount = 3,
            groupTimeDelta = 0.5f,

            bulletCount = 36,
            bulletAngleDelta = 360.0f / 36,
            bulletVelocity = new Vector2(0, 3.0f),
        };

        /// <summary>
        /// 螺旋
        /// </summary>
        public EmiterData screw = new EmiterData
        {
            emitCD = 3.0f,
            
            bulletCount = 64,
            bulletAngleDelta = 360.0f / 24,
            bulletVelocity = new Vector2(0, 3.0f),
            bulletTimeDelta = 0.05f,
        };
        
        /// <summary>
        /// 甩鞭
        /// </summary>
        public EmiterData whip = new EmiterData
        {
            emitCD = 3.0f,

            enableReverse = true,

            enableAngleRange = true,
            angleBegin = -60,
            angleEnd = 60,
            
            bulletCount = 64,
            bulletAngleDelta = 120.0f / 12,
            bulletVelocity = new Vector2(0, 3.0f),
            bulletTimeDelta = 0.1f,
        };

        /// <summary>
        /// 网格
        /// </summary>
        public EmiterData grid = new EmiterData
        {
            emitCD = 5.0f,

            enableReverse = true,

            groupCount = 3,
            groupTimeDelta = 0.5f,

            enableAngleRange = true,
            angleBegin = -60,
            angleEnd = 60,

            enablePositionRange = true,
            positionBegin = new Vector2(-2, 0),
            positionEnd = new Vector2(2, 0),
            bulletPositionDelta = new Vector2(0.4f, 0),

            bulletCount = 48,
            bulletAngleDelta = 120.0f / 12,
            bulletVelocity = new Vector2(0, 3.0f),
            bulletTimeDelta = 0.1f,
        };

        public List<EmiterData> emiters;

        public List<Vector2[]> shapeDatas = new List<Vector2[]>
        {
            // 正方形
            new Vector2[]
            {
                new Vector2(-0.5f,  0.5f),
                new Vector2(-0.5f, -0.5f),
                new Vector2( 0.5f, -0.5f),
                new Vector2( 0.5f,  0.5f),
            },
            // 三角形
            new Vector2[]
            {
                new Vector2(0,  0.5f),
                new Vector2(-0.5f, -0.5f),
                new Vector2( 0.5f, -0.5f),
            },
            // 五边形
            new Vector2[]
            {
                new Vector2(0.0f, 1.0f),
                new Vector2(-1.0f, 0.3f),
                new Vector2(-0.6f, -0.8f),
                new Vector2(0.6f, -0.8f),
                new Vector2(1.0f, 0.3f),
            },
        };

        public GameData()
        {
            emiters = new List<EmiterData>
            {
                line1,
                line2,
                sun,
                whip,
                screw,
                grid,
            };
        }
    }

}
