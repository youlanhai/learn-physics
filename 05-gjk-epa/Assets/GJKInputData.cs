﻿using UnityEngine;
using System.Collections.Generic;

public class GJKInputData : MonoBehaviour
{
    public PolygonCollider2D colliderA;
    public PolygonCollider2D colliderB;

    public bool stepByStep;

    public bool showMinkowskiSet = true;
    public bool showSimplex = true;
    public bool showDirection = true;
    public bool showPenetrateVector = true;
    public bool showEPAEdges = true;
}
