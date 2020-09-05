using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sample02
{

    public class SATDebuger : MonoBehaviour
    {
        public InputData inputData;
        public LineTool lineTool;

        bool isCollision = false;

        List<Shape> shapes;
        List<PolygonCollider2D> colliders;
        
        // 当前选中的shape信息
        int selectedIndex = -1;
        Vector2 selectedOffset;
        float selectedAngle;
        
        void Start()
        {
            inputData = GetComponent<InputData>();
            lineTool = GetComponent<LineTool>();

            colliders = new List<PolygonCollider2D> { inputData.colliderA, inputData.colliderB };
            shapes = new List<Shape>() { new Shape(), new Shape() };
        }
        
        void Update()
        {
            if (!inputData.colliderA || !inputData.colliderB)
            {
                return;
            }

            lineTool.BeginDraw();

            UpdateSelection();
            UpdateShape();

            isCollision = SAT.queryCollision(shapes[0], shapes[1]);

            DrawAxis();
            DrawShapes();
            
            lineTool.EndDraw();
        }
        
        void UpdateShape()
        {
            for(int i = 0; i < shapes.Count; ++i)
            {
                UpdateShape(shapes[i], colliders[i]);
            }
        }

        static void UpdateShape(Shape shape, PolygonCollider2D collider)
        {
            shape.vertices.Clear();
            Matrix4x4 matrix = collider.transform.localToWorldMatrix;
            foreach (Vector2 point in collider.points)
            {
                shape.vertices.Add(matrix.MultiplyPoint(point));
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
                Color color = Color.green;
                if (i == selectedIndex)
                {
                    color = new Color(1, 0, 1);
                }
                else if (isCollision)
                {
                    color = Color.red;
                }
                DrawPolygon(shapes[i].vertices, color);
            }
        }

        void UpdateSelection()
        {
            if(Input.GetMouseButtonDown(0))
            {
                Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

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
                    var t = colliders[selectedIndex].transform;
                    Vector2 position = t.position;
                    selectedOffset = mousePos - position;

                    float mouseAngle = Mathf.Atan2(selectedOffset.y, selectedOffset.x) * Mathf.Rad2Deg;
                    selectedAngle = mouseAngle - t.eulerAngles.z;
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

                Transform t = colliders[selectedIndex].transform;

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
                    t.eulerAngles = new Vector3(0, 0, angle);
                }
            }
        }
    }
}
