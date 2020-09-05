using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sample04
{

    public class GJKDebuger : MonoBehaviour
    {
        public GJKInputData inputData;
        public GJKLineTool lineTool;

        GJK gjk;
        List<Shape> shapes;
        List<PolygonCollider2D> colliders;

        IEnumerator stepEnumerator;

        // 当前选中的shape信息
        int selectedIndex = -1;
        Vector2 selectedOffset;
        float selectedAngle;

        GUIContent helpContent = new GUIContent(
            "<color=#ff00ff>鼠标选中</color>; " +
            "<color=red>发生碰撞</color>; " +
            "<color=grey>近似的Minkowski差集</color>; " +
            "<color=blue>单形体</color>; " +
            "<color=yellow>当前support的方向</color>; "
            );
        
        void Start()
        {
            inputData = GetComponent<GJKInputData>();
            lineTool = GetComponent<GJKLineTool>();

            colliders = new List<PolygonCollider2D> { inputData.colliderA, inputData.colliderB };

            gjk = new GJK();
            shapes = new List<Shape>() { new Shape(), new Shape() };
        }
        
        void Update()
        {
            if (!inputData.colliderA || !inputData.colliderB)
            {
                return;
            }

            lineTool.BeginDraw();
            
            if (inputData.stepByStep)
            {
                if (Input.GetMouseButtonUp(0))
                {
                    UpdateStepByStep();
                }
            }
            else
            {
                UpdateSelection();
                stepEnumerator = null;
                UpdateShape();
                gjk.queryCollision(shapes[0], shapes[1]);
            }


            DrawAxis();
            DrawShapes();

            if (inputData.showMinkowskiSet)
            {
                var points = MinkowskiTool.computeMinkowskiSet(shapes[0], shapes[1]);
                DrawPolygon(points, Color.grey);
            }

            if (inputData.showSimplex)
            {
                DrawPolygon(gjk.simplex.points, Color.blue);
            }

            if(inputData.showDirection && inputData.stepByStep)
            {
                lineTool.DrawLine(Vector2.zero, gjk.direction, Color.gray, Color.yellow);
            }

            if (!gjk.isCollision)
            {
                lineTool.DrawLine(gjk.closestOnA, gjk.closestOnB, Color.red);
            }

            lineTool.EndDraw();
        }

        private void OnGUI()
        {
            GUILayout.BeginVertical();
            GUILayout.Label(helpContent);
            inputData.stepByStep = GUILayout.Toggle(inputData.stepByStep, "分步骤执行(鼠标点击下一步)");
            inputData.showMinkowskiSet = GUILayout.Toggle(inputData.showMinkowskiSet, "显示Minkowski集合");
            inputData.showSimplex = GUILayout.Toggle(inputData.showSimplex, "显示单形体");
            inputData.showDirection = GUILayout.Toggle(inputData.showDirection, "显示support方向");
            GUILayout.EndVertical();
        }

        void UpdateStepByStep()
        {
            if (stepEnumerator == null)
            {
                Debug.Log("begin step by step");
                UpdateShape();
                stepEnumerator = gjk.queryStepByStep(shapes[0], shapes[1]);
            }

            if (!stepEnumerator.MoveNext())
            {
                stepEnumerator = null;
                Debug.Log("finish step by step");
            }
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
                else if (gjk.isCollision)
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
