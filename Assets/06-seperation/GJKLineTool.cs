using UnityEngine;
using System.Collections.Generic;

namespace Sample06
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

        public LineRenderer requireLineRenderer()
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
            lineRenderer.startWidth = originLineRenderer.startWidth;
            lineRenderer.endWidth = originLineRenderer.endWidth;
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

        public void EndDraw()
        {
            for (int i = 0; i < lineRenderers.Count; ++i)
            {
                lineRenderers[i].enabled = i < lineRendererIndex;
            }
        }
    }
}
