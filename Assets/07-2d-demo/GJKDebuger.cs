using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sample07
{

    public class GJKDebuger : MonoBehaviour
    {
        public GJKInputData inputData;
        public GJKLineTool lineTool;
        public int collisionCount = 0;
        public bool verbose = false;

        public float moveForce = 1.0f;

        Physics physics;
        List<Shape> shapes;
        Game game = new Game();
        
        // 当前选中的shape信息
        int selectedIndex = -1;
        Vector2 selectedOffset;
        float selectedAngle;

        GUIContent helpContent = new GUIContent(
            "WASD移动;" +
            "CTRL开火;"
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
            inputData = GetComponent<GJKInputData>();
            lineTool = GetComponent<GJKLineTool>();

            game = new Game();
            game.Init();

            physics = game.physics;
            shapes = physics.shapes;
            
            Camera camera = Camera.main;
            float halfHeight = camera.orthographicSize;
            float halfWidth = camera.aspect * halfHeight;

            // left
            CreateWall(new Vector2(-halfWidth, 0), new Vector2(1, halfHeight * 2));
            // right
            CreateWall(new Vector2(halfWidth, 0), new Vector2(1, halfHeight * 2));
            // top
            CreateWall(new Vector2(0, halfHeight), new Vector2(halfWidth * 2, 1));
            // botom
            CreateWall(new Vector2(0, -halfHeight), new Vector2(halfWidth * 2, 1));
        }

        void CreateWall(Vector2 pos, Vector2 size)
        {
            Rigidbody body = new Rigidbody(float.PositiveInfinity, float.PositiveInfinity);
            body.position = pos;
            body.scale = size;

            body.shape = new Shape(body, boxVertices);
            physics.addRigidbody(body);
        }
        
        void Update()
        {
            lineTool.BeginDraw();

            physics.maxIteration = inputData.maxIteration;
            physics.verbose = verbose;
            
            if (inputData.stepByStep)
            {
                if (Input.GetMouseButtonUp(0))
                {
                    game.Update(Time.deltaTime);
                }
            }
            else
            {
                game.Update(Time.deltaTime);
            }

            collisionCount = physics.collisions.Count;
            UpdateSelection();
            
            DrawAxis();
            DrawShapes();
            
            lineTool.EndDraw();
        }
        
        private void OnGUI()
        {
            GUILayout.BeginVertical();
            GUILayout.Label(helpContent);
            inputData.stepByStep = GUILayout.Toggle(inputData.stepByStep, "分步骤执行(鼠标点击下一步)");
            GUILayout.EndVertical();
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
                Color color = Color.green;
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
