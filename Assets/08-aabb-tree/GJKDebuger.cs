using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sample08
{

    public class GJKDebuger : MonoBehaviour
    {
        public GJKLineTool lineTool;
        public int collisionCount = 0;
        public bool verbose = false;
        
        public int maxIteration = 5;

        Physics physics;
        List<Shape> shapes;
        
        // 当前选中的shape信息
        int selectedIndex = -1;
        Vector2 selectedOffset;
        float selectedAngle;

        GUIContent helpContent = new GUIContent(
            "WASD移动;" +
            "CTRL开火;" +
            "ENTER重置;"
            );

        List<Vector2[]> shapeDatas = new List<Vector2[]>
        {
            // 三角形
            new Vector2[]
            {
                new Vector2(0,  0.5f),
                new Vector2(-0.5f, -0.5f),
                new Vector2( 0.5f, -0.5f),
            },
            // 正方形
            new Vector2[]
            {
                new Vector2(-0.5f,  0.5f),
                new Vector2(-0.5f, -0.5f),
                new Vector2( 0.5f, -0.5f),
                new Vector2( 0.5f,  0.5f),
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

        void Start()
        {
            lineTool = GetComponent<GJKLineTool>();
            
            physics = new Physics();
            shapes = physics.shapes;
            
            Camera camera = Camera.main;
            float halfHeight = camera.orthographicSize;
            float halfWidth = camera.aspect * halfHeight;

            //game.rect = new Rect(-halfWidth, -halfHeight, halfWidth * 2, halfHeight * 2);

            // left
            CreateWall(new Vector2(-halfWidth - 0.4f, 0), new Vector2(1, halfHeight * 2));
            // right
            CreateWall(new Vector2(halfWidth + 0.5f, 0), new Vector2(1, halfHeight * 2));
            // top
            CreateWall(new Vector2(0, halfHeight + 0.4f), new Vector2(halfWidth * 2, 1));
            // botom
            CreateWall(new Vector2(0, -halfHeight - 0.4f), new Vector2(halfWidth * 2, 1));

            Rigidbody body = new Rigidbody(1, 1);
            body.addShape(new CircleShape(Vector2.zero, 1));
            physics.addRigidbody(body);

            body = new Rigidbody(2, 1);
            body.scale = new Vector2(2, 2);
            body.addShape(new CircleShape(Vector2.zero, 1));
            physics.addRigidbody(body);

            for (int i = 0; i < shapeDatas.Count; ++i)
            {
                body = new Rigidbody(1 + i, 1 + i);
                body.addShape(new PolygonShape(shapeDatas[i]));
                physics.addRigidbody(body);
            }
        }

        void CreateWall(Vector2 pos, Vector2 size)
        {
            Rigidbody body = new Rigidbody(float.PositiveInfinity, float.PositiveInfinity);
            body.position = pos;
            body.scale = size;

            var shape = new PolygonShape(shapeDatas[1]);
            shape.selfMask = 0xffffffff;
            shape.collisionMask = 0;

            body.addShape(shape);
            physics.addRigidbody(body);
        }
        
        void Update()
        {
            lineTool.BeginDraw();

            physics.maxIteration = maxIteration;
            physics.verbose = verbose;

            if (Input.GetKeyUp(KeyCode.Return))
            {
                //game.Restart();
            }

            physics.update(Time.fixedDeltaTime);

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
            GUILayout.EndArea();
            
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
                shapes[i].debugDraw(lineTool, color);
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
                    if (shapes[i].contains(mousePos))
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

            if (Input.GetMouseButtonDown(1))
            {
                Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

                Shape shape = null;
                for (int i = 0; i < shapes.Count; ++i)
                {
                    if (shapes[i].contains(mousePos))
                    {
                        shape = shapes[i];
                        break;
                    }
                }

                if (shape != null)
                {
                    var t = shape.rigidbody;
                    Vector2 dir = t.position - mousePos;
                    dir.Normalize();

                    t.forceImpulse = dir * 2.0f;
                }
            }
        }
    }
}
