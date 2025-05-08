using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class IntersectionLogger
{
    private Dictionary<Vector3, int> intersectionCounts = new Dictionary<Vector3, int>(); // Intersection points and their number
    private float intersectionThreshold = 0.05f; // Error margin to avoid storing duplicate points

    public void LogIntersection(Vector3 intersectionPoint)
    {
        Vector3 roundedPoint = RoundVector(intersectionPoint, intersectionThreshold);

        if (intersectionCounts.ContainsKey(roundedPoint))
        {
            intersectionCounts[roundedPoint]++;
        }
        else
        {
            intersectionCounts[roundedPoint] = 1;
        }
    }

    public void SaveIntersectionsToFile()
    {
        string filePath = Path.Combine(Application.dataPath, "FourRayIntersections.txt");
        using (StreamWriter writer = new StreamWriter(filePath))
        {
            foreach (var entry in intersectionCounts)
            {
                writer.WriteLine($"{entry.Key.x}, {entry.Key.y}, {entry.Key.z}, {entry.Value}");
            }
        }
        Debug.Log($"The intersection data has been saved to {filePath}");
    }

    private Vector3 RoundVector(Vector3 point, float precision)
    {
        return new Vector3(
            Mathf.Round(point.x / precision) * precision,
            Mathf.Round(point.y / precision) * precision,
            Mathf.Round(point.z / precision) * precision
        );
    }

    // Save ray data to txt file
    public void SaveRaysToFile(List<RayData> rayDatas, string filename = "RayData.txt")
    {
        string filePath = Path.Combine(Application.dataPath, filename);

        using (StreamWriter writer = new StreamWriter(filePath))
        {
            foreach (var rayData in rayDatas)
            {
                foreach(var hitPoint in rayData.hitPoints)
                {
                    writer.WriteLine($"{rayData.sourceID}, " +
                                 $"{hitPoint.x}, {hitPoint.y}, {hitPoint.z}");
                }
            }
        }

        Debug.Log($"Ray data has been saved to {filePath}");
    }
}
