using UnityEngine;
using System;
using System.Collections.Generic;

namespace Sample07
{
    public class Game
    {
        public Physics physics;
        public Player player;

        Dictionary<int, Entity> entities = new Dictionary<int, Entity>();

        List<EventNode> events = new List<EventNode>
        {
            new EventNode1 { time = 0, dir = 1 },
            new EventNode1 { time = 0, dir = -1 },
            new DummyEvent { time = 99999999, },
        };

        List<Vector2[]> shapeDatas = new List<Vector2[]>
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

        public float gameTime { get; private set; }
        int eventIndex = 0;
        bool isGameOver = false;
        int idCounter = 0;
        public bool isPause;

        public void Init()
        {
            physics = new Physics();

            player = new Player
            {
                hpMax = 100,
                attackPoint = 1,
                camp = Camp.Defence,
                initData = new EntityInitData
                {
                    shapeIndex = 2,
                },
            };
            AddEntity(player);

            events.Sort((a, b) => a.time.CompareTo(b.time));
            foreach(var node in events)
            {
                node.game = this;
            }

            Restart();
        }

        public void Restart()
        {
            gameTime = 0;
            eventIndex = 0;

            player.hp = player.hpMax = 100;
            player.score = 0;
            player.attackPoint = 50;
            player.defencePoint = 10;

            player.rigidbody.sleep();
            player.rigidbody.position = new Vector2(0, 0);
            player.rigidbody.setInertial(float.PositiveInfinity);
        }

        public void Update(float deltaTime)
        {
            if (isGameOver || isPause)
            {
                return;
            }

            gameTime += deltaTime;

            if (eventIndex >= events.Count)
            {
                isGameOver = true;
                OnGameOver(true);
            }
            
            while (eventIndex < events.Count && gameTime > events[eventIndex].time)
            {
                events[eventIndex].Run();
                ++eventIndex;
            }

            player.Update(deltaTime);
            physics.update(Time.fixedDeltaTime);
        }

        void OnGameOver(bool success)
        {

        }

        public void AddEntity(Entity entity)
        {
            entity.id = ++idCounter;
            entity.game = this;
            entities.Add(entity.id, entity);
            entity.OnEnterWorld();

            physics.addRigidbody(entity.rigidbody);
        }

        public void DestroyEntity(Entity entity)
        {
            if (!entities.Remove(entity.id))
            {
                return;
            }
            physics.removeRigidbody(entity.rigidbody);
            entity.OnLeaveWorld();
        }

        public Entity GetEntity(int id)
        {
            entities.TryGetValue(id, out Entity value);
            return value;
        }

        public Vector2[] GetShapeData(int shapeIndex)
        {
            return shapeDatas[shapeIndex];
        }
    }

    public abstract class EventNode
    {
        public float time;
        public Game game;
        
        public abstract void Run();
    }

    public class EventNode1 : EventNode
    {
        public float dir;

        public override void Run()
        {
            for (int i = 0; i < 3; ++i)
            {

            }
        }
    }

    public class DummyEvent : EventNode
    {
        public override void Run()
        {
        }
    }

}
