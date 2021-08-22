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
        public GameData gameData = new GameData();

        List<EventNode> events = new List<EventNode>
        {
            new EventNode1 { time = 0, dir = 1 },
            new EventNode2 { time = 10},
            new EventNode1 { time = 20, dir = -1 },
            new EventNode3 { time = 30, },
            new EventNode1 { time = 40, dir = 1 },
            new EventNode4 { time = 50,},
            new EventNode1 { time = 70, dir = -1 },
            new EventNode5 { time = 80,},
            new EventNode1 { time = 90, dir = -1, dropAp = 2, },
            new EventNode1 { time = 100, dir = -1, dropAp = 2 },
            new EventNode1 { time = 120, dir = -1, dropAp = 2 },
            new EventNode1 { time = 140, dir = -1, dropAp = 2 },
            new EventNode1 { time = 160, dir = -1, dropAp = 2 },
            new EventNode1 { time = 180, dir = -1, dropAp = 2 },
            new EventNode1 { time = 200, dir = -1, dropAp = 2 },
            new EventNode1 { time = 220, dir = -1, dropAp = 2 },
            new DummyEvent { time = 600, },
        };

        public float gameTime { get; private set; }
        int eventIndex = 0;
        public bool isGameOver = false;
        int idCounter = 0;
        public bool isPause;
        public Rect rect;

        public Color enemyColor = new Color(0.5f, 0.7f, 1.0f);

        List<Entity> pendingAdds = new List<Entity>();
        List<Entity> pendingRemoves = new List<Entity>();
        List<Entity> pendingCache = new List<Entity>();

        public void Init()
        {
            physics = new Physics();

            player = new Player
            {
                hpMax = 100,
                moveSpeed = 10,
                initData = new EntityInitData
                {
                    shapeIndex = 2,
                    selfMask = LayerMask.PLAYER,
                    collisionMask = LayerMask.COLLISION_PLAYER,
                    emiterData = gameData.line2,
                    scale = new Vector2(0.4f, 1.0f),
                },
            };
            AddEntity(player);
            FlushPendingEntities();

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
            isGameOver = false;

            player.hpMax = 100;
            player.SetHp(player.hpMax);
            player.SetDead(false);

            player.score = 0;
            player.attackPoint = 2;
            player.defencePoint = 1;

            player.rigidbody.sleep();
            player.rigidbody.position = new Vector2(0, 0);
            player.rigidbody.setInertial(float.PositiveInfinity);

            foreach (var pair in entities)
            {
                if (pair.Value != player)
                {
                    DestroyEntity(pair.Value);
                }
            }

            FlushPendingEntities();
            player.OnRestart();
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
                //OnGameOver(true);
                eventIndex = 0;
            }

            while (eventIndex < events.Count && gameTime > events[eventIndex].time)
            {
                events[eventIndex].Run();
                ++eventIndex;
            }

            FlushPendingEntities();

            foreach (var pair in entities)
            {
                pair.Value.Update(deltaTime);
            }
            
            physics.update(Time.fixedDeltaTime);

            FlushPendingEntities();
        }

        public void OnGameOver(bool success)
        {
            isGameOver = true;
            Debug.Log("GameOver: " + success);
        }

        public void AddEntity(Entity entity)
        {
            entity.id = ++idCounter;
            entity.game = this;
            pendingAdds.Add(entity);
        }

        public void DestroyEntity(Entity entity)
        {
            pendingRemoves.Add(entity);
        }

        public Entity GetEntity(int id)
        {
            entities.TryGetValue(id, out Entity value);
            return value;
        }

        void FlushPendingEntities()
        {
            List<Entity> temp = pendingAdds;
            pendingAdds = pendingCache;
            pendingCache = temp;
            foreach (var entity in pendingCache)
            {
                entities.Add(entity.id, entity);
                entity.OnEnterWorld();
                physics.addRigidbody(entity.rigidbody);
            }
            pendingCache.Clear();

            temp = pendingRemoves;
            pendingRemoves = pendingCache;
            pendingCache = temp;
            foreach (var entity in pendingCache)
            {
                if (!entities.Remove(entity.id))
                {
                    Debug.LogError("Destroy Entity was not found: " + entity.id);
                    continue;
                }
                if (entity.isInWorld)
                {
                    physics.removeRigidbody(entity.rigidbody);
                    entity.OnLeaveWorld();
                }
            }
            pendingCache.Clear();
        }

        public Vector2[] GetShapeData(int shapeIndex)
        {
            return gameData.shapeDatas[shapeIndex];
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
        public int dropAp;

        public override void Run()
        {
            float x = -2;
            float y = game.rect.yMax + 2;
            for (int i = 0; i < 3; ++i)
            {
                Enemy e = new Enemy
                {
                    dropHp = 10,
                    dropAp = dropAp,
                    initData = new EntityInitData
                    {
                        shapeIndex = 2,
                        rotation = 180,
                        scale = new Vector2(0.5f, 0.5f),
                        velocity = new Vector2(-1, -2),
                        position = new Vector2(x, y),
                        selfMask = LayerMask.ENEMY,
                        collisionMask = LayerMask.COLLISION_ENEMY,
                        emiterData = game.gameData.line1,
                        color = game.enemyColor,
                    },
                };
                game.AddEntity(e);

                x += 2;
                y += 2;
            }
        }
    }
    
    public class EventNode2 : EventNode
    {
        public override void Run()
        {
            Enemy e = new Enemy
            {
                hpMax = 300,
                lifeTime = 30,
                initData = new EntityInitData
                {
                    shapeIndex = 2,
                    rotation = 180,
                    velocity = new Vector2(0, -0.5f),
                    position = new Vector2(0, game.rect.yMax + 2),
                    selfMask = LayerMask.ENEMY,
                    collisionMask = LayerMask.COLLISION_ENEMY,
                    emiterData = game.gameData.sun,
                    mass = 100,
                    inertial = 100,
                    color = game.enemyColor,
                },
            };
            game.AddEntity(e);
        }
    }

    public class EventNode3 : EventNode
    {
        public override void Run()
        {
            Enemy e = new Enemy
            {
                hpMax = 1000,
                lifeTime = 30,
                initData = new EntityInitData
                {
                    shapeIndex = 2,
                    rotation = 180,
                    velocity = new Vector2(0, -0.5f),
                    position = new Vector2(0, game.rect.yMax + 2),
                    scale = new Vector2(1.2f, 1.2f),
                    selfMask = LayerMask.ENEMY,
                    collisionMask = LayerMask.COLLISION_ENEMY,
                    emiterData = game.gameData.whip,
                    mass = 100,
                    inertial = 100,
                    color = game.enemyColor,
                },
            };
            game.AddEntity(e);
        }
    }

    public class EventNode4 : EventNode
    {
        public override void Run()
        {
            Enemy e = new Enemy
            {
                hpMax = 2000,
                lifeTime = 120,
                initData = new EntityInitData
                {
                    shapeIndex = 2,
                    rotation = 180,
                    velocity = new Vector2(0, -0.5f),
                    position = new Vector2(0, game.rect.yMax + 2),
                    scale = new Vector2(1.5f, 1.5f),
                    selfMask = LayerMask.ENEMY,
                    collisionMask = LayerMask.COLLISION_ENEMY,
                    emiterData = game.gameData.screw,
                    mass = 100,
                    inertial = 100,
                    color = Color.red,
                },
            };
            game.AddEntity(e);
        }
    }

    public class EventNode5 : EventNode
    {
        public override void Run()
        {
            Enemy e = new Enemy
            {
                hpMax = 20000,
                lifeTime = 600,
                initData = new EntityInitData
                {
                    shapeIndex = 2,
                    rotation = 180,
                    velocity = new Vector2(0, -0.5f),
                    position = new Vector2(0, game.rect.yMax + 2),
                    scale = new Vector2(1.8f, 1.5f),
                    selfMask = LayerMask.ENEMY,
                    collisionMask = LayerMask.COLLISION_ENEMY,
                    emiterData = game.gameData.grid,
                    mass = 100000,
                    inertial = 100000,
                    color = Color.red,
                },
            };
            game.AddEntity(e);
        }
    }

    public class DummyEvent : EventNode
    {
        public override void Run()
        {
        }
    }

}
