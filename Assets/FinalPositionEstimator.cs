using System.IO;
using System.Globalization;
using System.Collections.Generic;
using UnityEngine;

public static class VoxelUtils
{
    public static Vector3 ComputeAveragePositionFromFile(string path)
    {
        List<Vector3> points = new List<Vector3>();
        foreach (var line in File.ReadAllLines(path))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] tokens = line.Split(',');
            if (tokens.Length < 3) continue;

            float x = float.Parse(tokens[0], CultureInfo.InvariantCulture);
            float y = float.Parse(tokens[1], CultureInfo.InvariantCulture);
            float z = float.Parse(tokens[2], CultureInfo.InvariantCulture);

            points.Add(new Vector3(x, y, z));
        }

        if (points.Count == 0)
        {
            Debug.LogWarning("没有有效点被读取！");
            return Vector3.zero;
        }

        Vector3 sum = Vector3.zero;
        foreach (var p in points)
        {
            sum += p;
        }

        return sum / points.Count;
    }
}
