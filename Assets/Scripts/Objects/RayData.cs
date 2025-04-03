using System.Collections.Generic;
using UnityEngine;

public class RayData
{
    public Ray ray; 
    public LineRenderer lineRenderer; // LineRenderer for visualization
    public int reflectionCount = 0; // Current reflection count
    public int maxReflections = 5; // Maximum number of reflections (default 5)
    public int sourceID; // base station ID
    public Color rayColor = Color.white; 
    public bool isActive = true; 
    public List<Vector3> hitPoints = new List<Vector3>(); // Store all intersection points

    public RayData(Ray ray, LineRenderer lineRenderer,int sourceID, Color color, int maxReflections = 5)
    {
        this.ray = ray;
        this.lineRenderer = lineRenderer;
        this.sourceID = sourceID;
        this.rayColor = color;
        this.maxReflections = maxReflections;
    }

    public void AddHitPoint(Vector3 point)
    {
        hitPoints.Add(point);
    }

    // Reset ray (clear intersection information, etc.)
    public void Reset()
    {
        reflectionCount = 0;
        hitPoints.Clear();
    }
}
