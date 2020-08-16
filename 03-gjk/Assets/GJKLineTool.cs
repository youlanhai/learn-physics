using UnityEngine;
using System.Collections.Generic;

public class GJKLineTool : MonoBehaviour
{
    public GameObject lineRendererPrefab;

    int lineRendererIndex;
    List<LineRenderer> lineRenderers = new List<LineRenderer>();

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
        return lineRenderers[lineRendererIndex++];
    }

    public void BeginDraw()
    {
        lineRendererIndex = 0;
    }

    public void DrawPolygon(List<Vector2> vertices, Color color)
    {
        int n = vertices.Count;
        if (n == 0)
        {
            return;
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
    }

    public void EndDraw()
    {
        for (int i = 0; i < lineRenderers.Count; ++i)
        {
            lineRenderers[i].enabled = i < lineRendererIndex;
        }
    }
}
