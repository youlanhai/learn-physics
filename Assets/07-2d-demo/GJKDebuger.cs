using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sample07
{

    public class GJKDebuger : MonoBehaviour
    {
        public GJKLineTool lineTool;
        public int collisionCount = 0;
        public bool verbose = false;
        
        public int maxIteration = 5;

        Physics physics;
        List<Shape> shapes;
        Game game = new Game();
        
        // 当前选中的shape信息
        int selectedIndex = -1;
        Vector2 selectedOffset;
        float selectedAngle;

        GUIContent helpContent = new GUIContent(
            "WASD移动;" +
            "CTRL开火;" +
            "ENTER重置;"
            );

        public Vector2[] boxVertices = new Vector2[]
        {
            new Vector2(-0.5f,  0.5f),
            new Vector2(-0.5f, -0.5f),
            new Vector2( 0.5f, -0.5f),
            new Vector2( 0.5f,  0.5f),
        };

        void Start()
        {
            lineTool = GetComponent<GJKLineTool>();

            game = new Game();
            game.Init();

            physics = game.physics;
            shapes = physics.shapes;
            
            Camera camera = Camera.main;
            float halfHeight = camera.orthographicSize;
            float halfWidth = camera.aspect * halfHeight;

            game.rect = new Rect(-halfWidth, -halfHeight, halfWidth * 2, halfHeight * 2);

            // left
            CreateWall(new Vector2(-halfWidth - 0.4f, 0), new Vector2(1, halfHeight * 2));
            // right
            CreateWall(new Vector2(halfWidth + 0.5f, 0), new Vector2(1, halfHeight * 2));
            // top
            CreateWall(new Vector2(0, halfHeight + 0.4f), new Vector2(halfWidth * 2, 1));
            // botom
            CreateWall(new Vector2(0, -halfHeight - 0.4f), new Vector2(halfWidth * 2, 1));
        }

        void CreateWall(Vector2 pos, Vector2 size)
        {
            Rigidbody body = new Rigidbody(float.PositiveInfinity, float.PositiveInfinity);
            body.position = pos;
            body.scale = size;

            body.shape = new Shape(body, boxVertices);
            body.shape.selfMask = LayerMask.WALL;
            body.shape.collisionMask = 0;
            physics.addRigidbody(body);
        }
        
        void Update()
        {
            lineTool.BeginDraw();

            physics.maxIteration = maxIteration;
            physics.verbose = verbose;

            if (Input.GetKeyUp(KeyCode.Return))
            {
                game.Restart();
            }

            game.Update(Time.deltaTime);

            collisionCount = physics.collisions.Count;
            UpdateSelection();
            
            DrawAxis();
            DrawShapes();
            
            lineTool.EndDraw();
        }
        
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 100));
            GUILayout.Label(helpContent);
            GUILayout.Label("血量: " + game.player.hp);
            GUILayout.Label("分数: " + game.player.score);
            GUILayout.Label("攻击: " + game.player.attackPoint);
            GUILayout.EndArea();

            if (game.isGameOver)
            {
                Color old = GUI.color;
                GUI.color = Color.red;
                Rect rc = new Rect(Screen.width * 0.5f - 100, Screen.height * 0.5f, 200, 30);
                GUI.Label(rc, "游戏结束! 按回车键继续");
                GUI.color = old;
            }
        }

        void DrawPolygon(List<Vector2> vertices, Color color)
        {
            lineTool.DrawPolygon(vertices, color);
        }

        void DrawAxis()
        {
            Color color = Color.white;
            float lineWidth = 0.01f;

            var renderer = lineTool.DrawLine(new Vector2(0, -10), new Vector2(0, 10), color);
            renderer.startWidth = lineWidth;
            renderer.endWidth = lineWidth;

            renderer = lineTool.DrawLine(new Vector2(-10, 0), new Vector2(10, 0), color);
            renderer.startWidth = lineWidth;
            renderer.endWidth = lineWidth;
        }

        void DrawShapes()
        {
            for (int i = 0; i < shapes.Count; ++i)
            {
                Color color = shapes[i].color;
                if (i == selectedIndex)
                {
                    color = new Color(1, 0, 1);
                }
                //else if (shapes[i].isCollision)
                //{
                //    color = Color.red;
                //}
                DrawPolygon(shapes[i].vertices, color);
            }
        }

        void UpdateSelection()
        {
            if(Input.GetMouseButtonDown(0))
            {
                Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                //Debug.Log("mousePos: " + mousePos);

                selectedIndex = -1;
                for (int i = 0; i < shapes.Count; ++i)
                {
                    if (GJKTool.contains(shapes[i].vertices, mousePos))
                    {
                        selectedIndex = i;
                        break;
                    }
                }

                if(selectedIndex >= 0)
                {
                    var t = shapes[selectedIndex].rigidbody;
                    Vector2 position = t.position;
                    selectedOffset = mousePos - position;

                    float mouseAngle = Mathf.Atan2(selectedOffset.y, selectedOffset.x) * Mathf.Rad2Deg;
                    selectedAngle = mouseAngle - t.rotation;
                }
            }
            else if(Input.GetMouseButtonUp(0))
            {
                selectedIndex = -1;
            }
            else if(Input.GetMouseButton(0))
            {
                if(selectedIndex < 0)
                {
                    return;
                }

                Rigidbody t = shapes[selectedIndex].rigidbody;

                Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                if(selectedOffset.magnitude < 0.4f) // 移动
                {
                    Vector3 position = t.position;
                    position.x = mousePos.x - selectedOffset.x;
                    position.y = mousePos.y - selectedOffset.y;
                    t.position = position;
                }
                else // 旋转
                {
                    Vector2 position = t.position;
                    Vector2 mouseDir = mousePos - position;
                    float mouseAngle = Mathf.Atan2(mouseDir.y, mouseDir.x) * Mathf.Rad2Deg;

                    float angle = mouseAngle - selectedAngle;
                    t.rotation = angle;
                }
            }
        }
    }
}
