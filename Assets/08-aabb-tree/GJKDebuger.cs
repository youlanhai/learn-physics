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
        Shape selectedShape;
        Vector2 selectedOffset;
        Vector2 lastMouseMovePos;
        bool selectForMove;

        private Vector2 raycastStart;

        GUIContent helpContent = new GUIContent(
            "鼠标左键拖动;" +
            "鼠标右键施加力;" +
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
            //physics.gravity = new Vector2(0, -2.8f);
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
            body.scale = new Vector2(1.5f, 1.5f);
            body.addShape(new CircleShape(Vector2.zero, 1));
            physics.addRigidbody(body);

            for (int k = 0; k < 4; ++k)
            {
                for (int i = 0; i < shapeDatas.Count; ++i)
                {
                    body = new Rigidbody(1 + i, 1 + i);
                    body.addShape(new PolygonShape(shapeDatas[i]));
                    physics.addRigidbody(body);
                }
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
                ResetGame();
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
                Shape shape = shapes[i];
                Color color = shape.color;
                if (shape == selectedShape)
                {
                    color = new Color(1, 0, 1);
                }
                else if (shape.rigidbody.isActive)
                {
                    color = Color.red;
                }
                //lineTool.DrawBox(shape.bounds, Color.white, 0.5f);
                shape.debugDraw(lineTool, color);
            }
        }

        void UpdateSelection()
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            if(Input.GetMouseButtonDown(0))
            {
                //Debug.Log("mousePos: " + mousePos);
                selectedShape = physics.pointCast(mousePos);
                
                if (selectedShape != null)
                {
                    var t = selectedShape.rigidbody;
                    selectedOffset = mousePos - t.position;

                    AABB bounds = t.getBounds();
                    selectForMove = selectedOffset.sqrMagnitude < bounds.extends.sqrMagnitude * (0.5f * 0.5f);

                    lastMouseMovePos = mousePos;
                }
            }
            else if(Input.GetMouseButtonUp(0))
            {
                selectedShape = null;
            }
            else if(Input.GetMouseButton(0))
            {
                Vector2 mouseDir = mousePos - lastMouseMovePos;
                if (selectedShape != null && mouseDir.sqrMagnitude > 0.0001f)
                {
                    lastMouseMovePos = mousePos;
                    Rigidbody t = selectedShape.rigidbody;

                    if (selectForMove) // 移动
                    {
                        t.position = mousePos - selectedOffset;
                    }
                    else // 旋转
                    {
                        Vector2 newOffset = mousePos - t.position;
                        float clockwise = GJKTool.cross(selectedOffset, mouseDir) > 0 ? 1.0f : -1.0f;
                        float mouseAngle = mouseDir.magnitude / 4.0f * 360.0f; // 每4米对应360度
                        t.rotation -= mouseAngle * clockwise;
                    }

                    t.setActive(true);
                }
            }

            if (Input.GetMouseButtonDown(1))
            {
                Shape shape = physics.pointCast(mousePos);
                if (shape != null)
                {
                    var t = shape.rigidbody;
                    Vector2 dir = t.position - mousePos;
                    dir.Normalize();

                    t.forceImpulse = dir * 10.0f;
                    t.setActive(true);
                }
            }

            // 测试射线检测
            if (true)
            {
                Vector2 raycastEnd;
                Vector2 raycastDir = mousePos - raycastStart;
                if (raycastDir.sqrMagnitude < 0.0001f)
                {
                    raycastDir.x = 1.0f;
                }
                raycastDir.Normalize();
                if (physics.raycast(new Ray2D(raycastStart, raycastDir), 20.0f, out RaycastHit hit))
                {
                    raycastEnd = hit.point;
                }
                else
                {
                    raycastEnd = raycastStart + raycastDir * 20.0f;
                }
                lineTool.DrawLine(raycastStart, raycastEnd, Color.red);
            }
        }

        void ResetGame()
        {
            foreach (var body in physics.rigidbodies)
            {
                if (body.isStatic)
                {
                    continue;
                }
                body.position = Vector2.zero;
                body.rotation = 0;
                body.velocity = Vector2.zero;
                body.angleVelocity = 0;
                body.setActive(true);
            }
        }
    }
}
