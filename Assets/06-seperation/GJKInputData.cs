using UnityEngine;
using System.Collections.Generic;

namespace Sample06
{
    public class GJKInputData : MonoBehaviour
    {
        public List<Vector2> vertices1 = new List<Vector2>
        {
            new Vector2(0.0f, 1.0f),
            new Vector2(-1.0f, 0.3f),
            new Vector2(-0.6f, -0.8f),
            new Vector2(0.6f, -0.8f),
            new Vector2(1.0f, 0.3f),
        };

        public List<Vector2> vertices2 = new List<Vector2>
        {
            new Vector2(0.0f, 1.0f),
            new Vector2(-1.0f, 0.3f),
            new Vector2(-0.6f, -0.8f),
            new Vector2(0.6f, -0.8f),
            new Vector2(1.0f, 0.3f),
        };

        public bool stepByStep;

        public bool showMinkowskiSet = true;
        public bool showSimplex = true;
        public bool showDirection = true;
        public bool showPenetrateVector = true;
        public bool showEPAEdges = true;

        public int maxIteration = 10;
    }
}
