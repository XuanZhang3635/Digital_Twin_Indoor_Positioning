using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;

public class VoxelIntersectionCounter : MonoBehaviour
{
    public float voxelSize = 0.5f;
    public string outputFilePath = "Assets/voxel_hits.txt";

    private Dictionary<Vector3Int, List<int>> voxelHits = new Dictionary<Vector3Int, List<int>>();
    private Dictionary<Vector3Int, int> voxelIntersectionCounts = new Dictionary<Vector3Int, int>();

    private Vector3 minBounds;
    private Vector3 maxBounds;

    public void CountVoxels(List<RayData> rays)
    {
        voxelHits.Clear();
        voxelIntersectionCounts.Clear();
        ComputeBoundsFromRays(rays);

        for (int i = 0; i < rays.Count; i++)
        {
            HashSet<Vector3Int> visited = GetVoxelsAlongRay(rays[i].ray.origin, rays[i].ray.direction.normalized * 50f);

            foreach (var voxel in visited)
            {
                if (!voxelHits.ContainsKey(voxel))
                    voxelHits[voxel] = new List<int>();

                voxelHits[voxel].Add(rays[i].sourceID);
            }
        }

        // 假设你有4个基站（编号为0,1,2,3），可根据需要修改
        int numBaseStations = 4;

        foreach (var kvp in voxelHits)
        {
            int beta = CountCombinations(kvp.Value, numBaseStations);
            if (beta > 0)
                voxelIntersectionCounts[kvp.Key] = beta;
        }

        SaveResultsToCSV();

        // 输出最大 β_k 所在的 voxel 坐标
        if (voxelIntersectionCounts.Count > 0)
        {
            var best = voxelIntersectionCounts.OrderByDescending(x => x.Value).First();
            Vector3 bestPos = VoxelToWorld(best.Key);
            Debug.Log($"Max β_k = {best.Value} at position {bestPos}");
        }

        Debug.Log($"Stat Finish! The number of voxels is: {voxelIntersectionCounts.Count}");
    }
    int CountCombinations(List<int> sourceIDs, int numBaseStations)
    {
        Dictionary<int, int> bsCounts = new Dictionary<int, int>();

        foreach (int id in sourceIDs)
        {
            if (!bsCounts.ContainsKey(id))
                bsCounts[id] = 0;
            bsCounts[id]++;
        }

        if (bsCounts.Count < numBaseStations)
            return 0;

        int combinations = 1;
        for (int i = 0; i < numBaseStations; i++)
        {
            if (!bsCounts.ContainsKey(i)) return 0;
            combinations *= bsCounts[i];
        }

        return combinations;
    }

    // public float voxelSize = 0.5f; 
    // public string outputFilePath = "Assets/voxel_hits.txt";
    // // public string heatmapFilePath = "Assets/voxel_heatmap.png"; 

    // private Dictionary<Vector3Int, HashSet<int>> voxelHits = new Dictionary<Vector3Int, HashSet<int>>();
    // private Dictionary<Vector3Int, int> voxelIntersectionCounts = new Dictionary<Vector3Int, int>();

    // private Vector3 minBounds;
    // private Vector3 maxBounds;

    // public void CountVoxels(List<RayData> rays)
    // {
    //     voxelHits.Clear();
    //     voxelIntersectionCounts.Clear();
    //     ComputeBoundsFromRays(rays);

    //     for (int i = 0; i < rays.Count; i++)
    //     {
    //         HashSet<Vector3Int> visited = GetVoxelsAlongRay(rays[i].ray.origin, rays[i].ray.direction.normalized * 50f);

    //         foreach (var voxel in visited)
    //         {
    //             if (!voxelHits.ContainsKey(voxel))
    //                 voxelHits[voxel] = new HashSet<int>();

    //             voxelHits[voxel].Add(rays[i].sourceID);
    //         }
    //     }

    //     foreach (var kvp in voxelHits)
    //     {
    //         if (kvp.Value.Count >= 4)
    //         {
    //             if (!voxelIntersectionCounts.ContainsKey(kvp.Key))
    //                 voxelIntersectionCounts[kvp.Key] = 0;

    //             voxelIntersectionCounts[kvp.Key]++;
    //         }
    //     }

    //     SaveResultsToCSV();
    // 	// SaveHeatmapAsImage();
    //     Debug.Log($"Stat Finish! The number of voxel is ：{voxelIntersectionCounts.Count}");
    // }

    void ComputeBoundsFromRays(List<RayData> rays)
    {
        Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

        foreach (var ray in rays)
        {
            Vector3 origin = ray.ray.origin;
            Vector3 endpoint = origin + ray.ray.direction.normalized * 50f;

            min = Vector3.Min(min, origin);
            min = Vector3.Min(min, endpoint);
            max = Vector3.Max(max, origin);
            max = Vector3.Max(max, endpoint);
        }

        float margin = 0.5f;
        minBounds = min - Vector3.one * margin;
        maxBounds = max + Vector3.one * margin;
    }

    HashSet<Vector3Int> GetVoxelsAlongRay(Vector3 origin, Vector3 direction)
    {
        HashSet<Vector3Int> voxels = new HashSet<Vector3Int>();

        Vector3 currentPos = origin;
        Vector3Int currentVoxel = WorldToVoxel(currentPos);

        Vector3 endPos = origin + direction;

        Vector3 deltaDist = new Vector3(
            Mathf.Abs(voxelSize / direction.x),
            Mathf.Abs(voxelSize / direction.y),
            Mathf.Abs(voxelSize / direction.z)
        );

        Vector3Int step = new Vector3Int(
            direction.x >= 0 ? 1 : -1,
            direction.y >= 0 ? 1 : -1,
            direction.z >= 0 ? 1 : -1
        );

        Vector3 nextVoxelBoundary = VoxelToWorld(currentVoxel) + new Vector3(
            direction.x >= 0 ? voxelSize : 0,
            direction.y >= 0 ? voxelSize : 0,
            direction.z >= 0 ? voxelSize : 0
        );

        Vector3 tMax = new Vector3(
            (nextVoxelBoundary.x - currentPos.x) / direction.x,
            (nextVoxelBoundary.y - currentPos.y) / direction.y,
            (nextVoxelBoundary.z - currentPos.z) / direction.z
        );

        Vector3Int voxel = currentVoxel;

        int maxSteps = 1000;
        for (int i = 0; i < maxSteps; i++)
        {
            if (IsWithinBounds(voxel))
                voxels.Add(voxel);

            if (tMax.x < tMax.y && tMax.x < tMax.z)
            {
                voxel.x += step.x;
                tMax.x += deltaDist.x;
            }
            else if (tMax.y < tMax.z)
            {
                voxel.y += step.y;
                tMax.y += deltaDist.y;
            }
            else
            {
                voxel.z += step.z;
                tMax.z += deltaDist.z;
            }

            Vector3 worldPos = VoxelToWorld(voxel);
            if (!IsWithinBounds(voxel) || Vector3.Distance(worldPos, origin) > 50f)
                break;
        }

        return voxels;
    }

    Vector3Int WorldToVoxel(Vector3 pos)
    {
        Vector3 relative = pos - minBounds;
        return new Vector3Int(
            Mathf.FloorToInt(relative.x / voxelSize),
            Mathf.FloorToInt(relative.y / voxelSize),
            Mathf.FloorToInt(relative.z / voxelSize)
        );
    }

    Vector3 VoxelToWorld(Vector3Int voxel)
    {
        return minBounds + new Vector3(
            voxel.x * voxelSize,
            voxel.y * voxelSize,
            voxel.z * voxelSize
        );
    }

    bool IsWithinBounds(Vector3Int voxel)
    {
        Vector3 pos = VoxelToWorld(voxel);
        return pos.x >= minBounds.x && pos.x <= maxBounds.x &&
               pos.y >= minBounds.y && pos.y <= maxBounds.y &&
               pos.z >= minBounds.z && pos.z <= maxBounds.z;
    }

    void SaveResultsToCSV()
    {
        using (StreamWriter sw = new StreamWriter(outputFilePath))
        {
            foreach (var kvp in voxelIntersectionCounts)
            {
                Vector3 pos = VoxelToWorld(kvp.Key);
                sw.WriteLine($"{pos.x:F3},{pos.y:F3},{pos.z:F3},{kvp.Value}");
            }
        }

        Debug.Log($"Voxels saved as : {outputFilePath}");
    }

    public Vector3 ComputeLocalWeightedCentroid()
    {
        if (voxelIntersectionCounts.Count == 0)
        {
            Debug.LogWarning("voxelIntersectionCounts为空！");
            return Vector3.zero;
        }

        // 找到最高击中次数的体素
        var maxVoxel = voxelIntersectionCounts.OrderByDescending(kvp => kvp.Value).First().Key;

        // 在3x3x3邻域内取体素
        List<Vector3> positions = new List<Vector3>();
        List<float> weights = new List<float>();

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    Vector3Int neighbor = new Vector3Int(
                        maxVoxel.x + dx,
                        maxVoxel.y + dy,
                        maxVoxel.z + dz
                    );

                    if (voxelIntersectionCounts.TryGetValue(neighbor, out int count))
                    {
                        Vector3 pos = VoxelToWorld(neighbor);
                        positions.Add(pos);
                        weights.Add(count);
                    }
                }
            }
        }

        if (positions.Count == 0) return VoxelToWorld(maxVoxel); // fallback

        float totalWeight = weights.Sum();
        Vector3 weightedSum = Vector3.zero;
        for (int i = 0; i < positions.Count; i++)
        {
            weightedSum += positions[i] * weights[i];
        }

        return weightedSum / totalWeight;
    }


    // void SaveHeatmapAsImage()
    // {
    // 	int xSize = Mathf.CeilToInt((maxBounds.x - minBounds.x) / voxelSize);
    // 	int zSize = Mathf.CeilToInt((maxBounds.z - minBounds.z) / voxelSize);

    // 	int[,] heatmap2D = new int[xSize, zSize];

    // 	foreach (var kvp in voxelIntersectionCounts)
    // 	{
    // 		Vector3Int voxel = kvp.Key;
    // 		int count = kvp.Value;

    // 		heatmap2D[voxel.x, voxel.z] += count;
    // 	}

    // 	Texture2D texture = new Texture2D(xSize, zSize);
    // 	int maxCount = 1;

    // 	foreach (int count in heatmap2D)
    // 		maxCount = Mathf.Max(maxCount, count);

    // 	for (int x = 0; x < xSize; x++)
    // 	{
    // 		for (int z = 0; z < zSize; z++)
    // 		{
    // 			float t = Mathf.Clamp01((float)heatmap2D[x, z] / maxCount);
    // 			Color color = GetHeatmapColor((int)t);
    // 			texture.SetPixel(x, z, color);
    // 		}
    // 	}

    // 	texture.Apply();

    // 	byte[] pngData = texture.EncodeToPNG();
    // 	File.WriteAllBytes(heatmapFilePath, pngData);
    // 	Debug.Log("The heat map saved as：" + heatmapFilePath);
    // }

    // void OnDrawGizmos()
    // {
    // 	if (voxelIntersectionCounts == null) return;

    // 	foreach (var kvp in voxelIntersectionCounts)
    // 	{
    // 		Vector3 voxelPos = VoxelToWorld(kvp.Key);
    // 		int hitCount = kvp.Value;

    // 		Color color = GetHeatmapColor(hitCount);

    // 		Gizmos.color = color;
    // 		Gizmos.DrawCube(voxelPos, Vector3.one * (voxelSize-0.05f)); // 使用一个小立方体表示体素
    // 	}
    // }

    // Color GetHeatmapColor(int hitCount)
    // {
    // 	float t = Mathf.Clamp01(hitCount / 10f);  
    // 	return Color.Lerp(Color.blue, Color.red, t); 
    // }

}
