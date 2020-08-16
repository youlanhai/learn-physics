using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace gjk2d
{

    public class GJKDebuger : MonoBehaviour
    {
        public GJKInputData inputData;
        public GJKLineTool lineTool;

        GJK gjk;
        Shape shapeA;
        Shape shapeB;

        IEnumerator stepEnumerator;
        
        // Start is called before the first frame update
        void Start()
        {
            inputData = GetComponent<GJKInputData>();
            lineTool = GetComponent<GJKLineTool>();

            gjk = new GJK();
            shapeA = new Shape();
            shapeB = new Shape();
        }

        // Update is called once per frame
        void Update()
        {
            if (!inputData.colliderA || !inputData.colliderB)
            {
                return;
            }

            lineTool.BeginDraw();

            if (Input.GetKeyUp(KeyCode.Space))
            {
                inputData.stepByStep = !inputData.stepByStep;
            }

            if (inputData.stepByStep)
            {
                if (Input.GetMouseButtonUp(0))
                {
                    UpdateStepByStep();
                }
            }
            else
            {
                stepEnumerator = null;
                UpdateShape();
                gjk.queryCollision(shapeA, shapeB);
            }

            Color color = gjk.isCollision ? Color.red : Color.green;
            DrawPolygon(shapeA.vertices, color);
            DrawPolygon(shapeB.vertices, color);

            //if (stepByStep)
            {
                DrawPolygon(gjk.simplex.points, Color.blue);

                List<Vector2> vertices = new List<Vector2> { Vector2.zero, gjk.direction };
                DrawPolygon(vertices, Color.yellow);
            }

            lineTool.EndDraw();
        }

        void UpdateStepByStep()
        {
            if (stepEnumerator == null)
            {
                Debug.Log("begin step by step");
                UpdateShape();
                stepEnumerator = gjk.queryStepByStep(shapeA, shapeB);
            }

            if (!stepEnumerator.MoveNext())
            {
                stepEnumerator = null;
                Debug.Log("finish step by step");
            }
        }
        
        void UpdateShape()
        {
            UpdateShape(shapeA, inputData.colliderA);
            UpdateShape(shapeB, inputData.colliderB);
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

    }
}
