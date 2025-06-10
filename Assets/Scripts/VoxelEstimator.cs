using System.Globalization;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public static class VoxelEstimator
{
    public static Vector3 ComputeFilteredWeightedCentroid(string path, float distanceThreshold = 0.8f)
	{
		var lines = File.ReadAllLines(path);
		Dictionary<Vector3Int, int> voxelHits = new Dictionary<Vector3Int, int>();

		foreach (var line in lines)
		{
			if (string.IsNullOrWhiteSpace(line)) continue;

			var tokens = line.Split(',');
			if (tokens.Length < 4) continue;

			int x = int.Parse(tokens[0]);
			int y = int.Parse(tokens[1]);
			int z = int.Parse(tokens[2]);
			int count = int.Parse(tokens[3]);

			voxelHits[new Vector3Int(x, y, z)] = count;
		}

		if (voxelHits.Count == 0)
		{
			Debug.LogWarning("没有找到有效 voxel 数据！");
			return Vector3.zero;
		}

		// int topN = Mathf.Min(30, voxelHits.Count);
		int topN = voxelHits.Count;
		var topVoxels = voxelHits
			.OrderByDescending(kv => kv.Value)
			.Take(topN)
			.Select(kv => new { pos = (Vector3)kv.Key, count = kv.Value })
			.ToList();

		Vector3 firstEstimate = Vector3.zero;
		float totalWeight = 0f;
		foreach (var v in topVoxels)
		{
			firstEstimate += v.pos * v.count;
			totalWeight += v.count;
		}
		firstEstimate /= totalWeight;

		// 2. 只保留靠近初步估计中心的 voxel
		// var filteredVoxels = topVoxels
		// 	.Where(v => Vector3.Distance(v.pos, firstEstimate) <= distanceThreshold)
		// 	.ToList();

		// if (filteredVoxels.Count == 0)
		// {
		// 	Debug.LogWarning("过滤后无可用 voxel，使用初始估计");
		// 	return firstEstimate;
		// }

		// // 3. 重新加权估计
		// Vector3 refinedEstimate = Vector3.zero;
		// float refinedWeight = 0f;
		// foreach (var v in filteredVoxels)
		// {
		// 	refinedEstimate += v.pos * v.count;
		// 	refinedWeight += v.count;
		// }

		return firstEstimate;
	}

}
