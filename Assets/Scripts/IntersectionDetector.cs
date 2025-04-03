using System.Collections.Generic;
using UnityEngine;

public class IntersectionDetector
{
    private float intersectionThreshold = 0.2f; // Intersection error range
    private IntersectionLogger intersectionLogger;

    public IntersectionDetector(IntersectionLogger logger)
    {
        intersectionLogger = logger;
    }

    // Traverse all rays and check whether there is an intersection of four rays
    public void DetectIntersections(List<RayData> rays)
    {
        Debug.Log("detect the intersections:" + rays);
        
        Dictionary<Vector3, HashSet<int>> pointToBaseStations = new Dictionary<Vector3, HashSet<int>>();
        
        foreach (var ray in rays)
        {
            foreach (var point in ray.hitPoints)
            {
                Vector3 roundedPoint = RoundVector(point, intersectionThreshold);

                if (!pointToBaseStations.ContainsKey(roundedPoint))
                {
                    pointToBaseStations[roundedPoint] = new HashSet<int>();
                }

                pointToBaseStations[roundedPoint].Add(ray.sourceID);
            }
        }

        foreach (var entry in pointToBaseStations)
        {
            if (entry.Value.Count == 4) // The rays of the four base stations all pass through this point
            {
                Debug.Log("Useful Intersection:" + entry.Key);
                intersectionLogger.LogIntersection(entry.Key);
            }
        }

    }

    private Vector3 RoundVector(Vector3 point, float precision)
    {
        return new Vector3(
            Mathf.Round(point.x / precision) * precision,
            Mathf.Round(point.y / precision) * precision,
            Mathf.Round(point.z / precision) * precision
        );
    }
}
