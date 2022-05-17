using UnityEngine;
using System.Collections.Generic;

namespace Sample08
{
    public class GJKLineTool : MonoBehaviour
    {
        public GameObject lineRendererPrefab;
        private LineRenderer originLineRenderer;

        int lineRendererIndex;
        List<LineRenderer> lineRenderers = new List<LineRenderer>();

        private void Awake()
        {
            originLineRenderer = lineRendererPrefab.GetComponent<LineRenderer>();
        }

        public LineRenderer requireLineRenderer(float width = 1.0f)
        {
            if (lineRendererIndex >= lineRenderers.Count)
            {
                GameObject go = Instantiate(lineRendererPrefab, transform, false);
                go.name = "LineRender " + lineRendererIndex;
                var renderer = go.GetComponent<LineRenderer>();
                renderer.sortingOrder = lineRendererIndex;
                lineRenderers.Add(renderer);
            }

            var lineRenderer = lineRenderers[lineRendererIndex++];
            lineRenderer.startWidth = originLineRenderer.startWidth * width;
            lineRenderer.endWidth = originLineRenderer.endWidth * width;
            lineRenderer.loop = false;
            return lineRenderer;
        }

        public void BeginDraw()
        {
            lineRendererIndex = 0;
        }

        public LineRenderer DrawLine(Vector2 a, Vector2 b, Color color)
        {
            return DrawLine(a, b, color, color);
        }

        public LineRenderer DrawLine(Vector2 a, Vector2 b, Color colorA, Color colorB)
        {
            LineRenderer lineRenderer = requireLineRenderer();
            lineRenderer.startColor = colorA;
            lineRenderer.endColor = colorB;

            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, a);

            if(Vector2.Distance(a, b) > float.Epsilon)
            {
                lineRenderer.SetPosition(1, b);
            }
            else
            {
                // 做一点偏移，避免重合看不见
                lineRenderer.SetPosition(1, b + new Vector2(0.05f, 0));
            }
            return lineRenderer;
        }

        public LineRenderer DrawPolygon(List<Vector2> vertices, Color color)
        {
            int n = vertices.Count;
            if (n == 0)
            {
                return null;
            }

            LineRenderer lineRenderer = requireLineRenderer();
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;

            lineRenderer.positionCount = n + 1;
            for (int i = 0; i < n; ++i)
            {
                lineRenderer.SetPosition(i, vertices[i]);
            }

            if (n == 1)
            {
                lineRenderer.SetPosition(n, vertices[0] + new Vector2(0.05f, 0));
            }
            else
            {
                lineRenderer.SetPosition(n, vertices[0]);
            }
            return lineRenderer;
        }

        public void DrawCircle(Vector2 center, float radius, Color color)
        {
            LineRenderer lineRenderer = requireLineRenderer();
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;

            int n = 36;
            float stepAngle = Mathf.PI * 2 / n;
            lineRenderer.positionCount = n;
            lineRenderer.loop = true;
            for (int i = 0; i < n; ++i)
            {
                Vector2 pos = new Vector2(
                    center.x + Mathf.Cos(i * stepAngle) * radius,
                    center.y + Mathf.Sin(i * stepAngle) * radius
                    );
                lineRenderer.SetPosition(i, pos);
            }
        }

        public void DrawBox(AABB bounds, Color color, float width = 1.0f)
        {
            LineRenderer lineRenderer = requireLineRenderer(width);
            lineRenderer.startColor = color;

            lineRenderer.positionCount = 4;
            lineRenderer.loop = true;

            lineRenderer.SetPosition(0, new Vector3(bounds.xMin, bounds.yMin, 0));
            lineRenderer.SetPosition(1, new Vector3(bounds.xMin, bounds.yMax, 0));
            lineRenderer.SetPosition(2, new Vector3(bounds.xMax, bounds.yMax, 0));
            lineRenderer.SetPosition(3, new Vector3(bounds.xMax, bounds.yMin, 0));
        }

        public void EndDraw()
        {
            for (int i = 0; i < lineRenderers.Count; ++i)
            {
                lineRenderers[i].enabled = i < lineRendererIndex;
            }
        }
    }
}
