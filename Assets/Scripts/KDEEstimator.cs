using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

public static class KDEEstimator
{
    public static Vector3 EstimateByKDEFromVoxelFile(string path, float bandwidth = 0.5f)
    {
        List<Vector3> points = new List<Vector3>();
        List<int> counts = new List<int>();

        foreach (var line in File.ReadAllLines(path))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            string[] tokens = line.Split(',');

            if (tokens.Length < 4) continue;

            float x = float.Parse(tokens[0], CultureInfo.InvariantCulture);
            float y = float.Parse(tokens[1], CultureInfo.InvariantCulture);
            float z = float.Parse(tokens[2], CultureInfo.InvariantCulture);
            int count = int.Parse(tokens[3]);

            points.Add(new Vector3(x, y, z));
            counts.Add(count);
        }

        if (points.Count == 0)
        {
            Debug.LogWarning("No valid voxel points found in file.");
            return Vector3.zero;
        }

        Vector3 est = Vector3.zero;
        float totalWeight = 0f;

        // KDE 核函数叠加（简化：每个 voxel 是一个高斯核中心）
        for (int i = 0; i < points.Count; i++)
        {
            Vector3 p = points[i];
            int count = counts[i];

            // 权重是 voxel 命中次数和核密度函数值的乘积
            float weight = count * Mathf.Exp(-(p - points[0]).sqrMagnitude / (2 * bandwidth * bandwidth));  // 用第一个点作为参考中心估计密度
            est += p * weight;
            totalWeight += weight;
        }

        if (totalWeight > 0f)
            return est / totalWeight;
        else
            return Vector3.zero;
    }
}
