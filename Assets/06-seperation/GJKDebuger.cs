using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sample06
{

    public class GJKDebuger : MonoBehaviour
    {
        public GJKInputData inputData;
        public GJKLineTool lineTool;

        Physics physics;
        GJK gjk;
        List<Shape> shapes;

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
            "<color=yellow>当前support的方向</color>; \n" +
            "<color=red>最近距离和最近点</color>; \n" +
            "<color=green>穿透向量</color>; " +
            "<color=#ff00ff>当前EPA选择的边</color>; " +
            "<color=#5C91C4>EPA边界</color>; "
            );
        
        void Start()
        {
            inputData = GetComponent<GJKInputData>();
            lineTool = GetComponent<GJKLineTool>();

            physics = new Physics();
            gjk = physics.gjk;
            shapes = physics.shapes;

            Rigidbody bodyA = new Rigidbody(1, 1);
            Shape shapeA = new Shape(bodyA);
            shapeA.originVertices.AddRange(inputData.vertices1);
            physics.addRigidbody(bodyA);

            Rigidbody bodyB = new Rigidbody(2, 1);
            Shape shapeB = new Shape(bodyB);
            shapeB.originVertices.AddRange(inputData.vertices2);
            physics.addRigidbody(bodyB);

            ResetPosition();
        }

        void ResetPosition()
        {
            Rigidbody bodyA = physics.rigidbodies[0];
            bodyA.position = new Vector2(-2, 0);
            bodyA.rotation = 0;
            bodyA.velocity = Vector2.zero;
            bodyA.angleVelocity = 0;
            //bodyA.forceImpulse = new Vector2(1, 0);
            //bodyA.torqueImpulse = 90;
            bodyA.updateTransform();

            Rigidbody bodyB = physics.rigidbodies[1];
            bodyB.position = new Vector2(2, 0);
            bodyB.rotation = 0;
            bodyB.velocity = Vector2.zero;
            bodyB.angleVelocity = 0;
            bodyB.updateTransform();
        }

        void UpdateMotion()
        {
            float s = 1f;
            Rigidbody body = physics.rigidbodies[0];
            if (Input.GetKeyDown(KeyCode.W))
            {
                body.forceImpulse += new Vector2(0, s);
            }
            if (Input.GetKeyDown(KeyCode.S))
            {
                body.forceImpulse += new Vector2(0, -s);
            }
            if (Input.GetKeyDown(KeyCode.A))
            {
                body.forceImpulse += new Vector2(-s, 0);
            }
            if (Input.GetKeyDown(KeyCode.D))
            {
                body.forceImpulse += new Vector2(s, 0);
            }
        }

        void Update()
        {
            lineTool.BeginDraw();

            physics.maxIteration = inputData.maxIteration;

            if (inputData.stepByStep)
            {
                if (Input.GetMouseButtonUp(0))
                {
                    physics.update(Time.fixedDeltaTime);
                }
            }
            else
            {
                physics.update(Time.fixedDeltaTime);
            }

            UpdateSelection();

            if (Input.GetKeyDown(KeyCode.Space))
            {
                ResetPosition();
            }

            UpdateMotion();


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
                lineTool.DrawLine(Vector2.zero, gjk.direction, Color.yellow * 0.5f, Color.yellow);
            }

            //if (!gjk.isCollision)
            {
                lineTool.DrawLine(gjk.closestOnA, gjk.closestOnB, Color.red);
            }
            
            if (gjk.isCollision)
            {
                if (inputData.showEPAEdges)
                {
                    DrawSimplexEdges(gjk.simplexEdge, new Color(0.36f, 0.57f, 0.77f));

                    if (gjk.currentEpaEdge != null)
                    {
                        lineTool.DrawLine(gjk.currentEpaEdge.a.point, gjk.currentEpaEdge.b.point, new Color(1, 0, 1, 1));
                    }
                }

                if (inputData.showPenetrateVector)
                {
                    lineTool.DrawLine(Vector2.zero, gjk.penetrationVector, Color.green * 0.5f, Color.green);
                }

                if (Input.GetKeyUp(KeyCode.Return))
                {
                    Rigidbody t = physics.rigidbodies[1];
                    t.position = t.position + new Vector2(gjk.penetrationVector.x, gjk.penetrationVector.y);
                }
            }

            Rigidbody body = physics.rigidbodies[0];
            lineTool.DrawLine(body.position, body.position + body.velocity, Color.red, Color.green);
            body = physics.rigidbodies[1];
            lineTool.DrawLine(body.position, body.position + body.velocity, Color.red, Color.green);

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
            inputData.showEPAEdges = GUILayout.Toggle(inputData.showEPAEdges, "显示EPA的边");
            inputData.showPenetrateVector = GUILayout.Toggle(inputData.showPenetrateVector, "显示穿透向量");
            GUILayout.EndVertical();
        }

        void UpdateStepByStep()
        {
            if (stepEnumerator == null)
            {
                Debug.Log("begin step by step");
                return;
            }

            if (!stepEnumerator.MoveNext())
            {
                stepEnumerator = null;
                Debug.Log("finish step by step");
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
        
        void DrawSimplexEdges(SimplexEdge simplex, Color cr)
        {
            List<Vector2> edgePoints = new List<Vector2>();
            var edges = simplex.edges;
            for (int i = 0; i < edges.Count; ++i)
            {
                edgePoints.Add(edges[i].a.point + edges[i].normal * 0.02f);
            }
            DrawPolygon(edgePoints, cr);
        }
    }
}
