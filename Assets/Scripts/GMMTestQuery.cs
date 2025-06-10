using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;


public class GMMTestQuery : MonoBehaviour
{
	public TextAsset gmmJsonFile;
    private List<GMMTablePerBS> gmmTables;

	private List<Vector3> baseStationPositions = new List<Vector3>();
    private List<float> APpitch = new List<float>();
    private List<float> APyaw = new List<float>();
    private List<List<float?>> azimuths = new List<List<float?>>();
    private List<List<float?>> elevations = new List<List<float?>>();
    private List<Vector3> ground_truth = new List<Vector3>();

	private int currentMeasureIndex = 0;
    private int measureCnt = 0;
    private int APCnt = 0;

	private List<string> resultLines = new List<string>();

	void Start()
    {
        LoadGMMTable();
        GetLocationData();  // TODO: 替换为你的实际数据加载方法
        RunAllQueries();
    }

    void LoadGMMTable()
    {
        gmmTables = JsonUtility.FromJson<GMMTableListWrapper>(gmmJsonFile.text).tables;
    }

	void RunAllQueries()
    {
        measureCnt = azimuths.Count;
        APCnt = baseStationPositions.Count;

        resultLines.Add("Index,EstX,EstY,EstZ,GTX,GTY,GTZ,Error");

        for (int i = 0; i < measureCnt; i++)
        {
            Vector3 estimated = EstimatePosition(i);
            Vector3 gt = ground_truth[i];
            float err = Vector3.Distance(estimated, gt);

            resultLines.Add($"{i},{estimated.x:F3},{estimated.y:F3},{estimated.z:F3},{gt.x:F3},{gt.y:F3},{gt.z:F3},{err:F3}");
        }

        File.WriteAllLines(Path.Combine(Application.dataPath, "GMM_Localization_Result.csv"), resultLines);
        Debug.Log("Results written to GMM_Localization_Result.csv");
    }

	Vector3 EstimatePosition(int measureIndex)
    {
        List<Vector3> allSamples = new List<Vector3>();

        for (int apIdx = 0; apIdx < APCnt; apIdx++)
        {
            float? az = azimuths[measureIndex][apIdx];
            float? el = elevations[measureIndex][apIdx];

            if (!az.HasValue || !el.HasValue)
                continue;

            float azAdj = Mathf.Rad2Deg * az.Value;
            float elAdj = Mathf.Rad2Deg * el.Value;

            GMMEntry entry = FindClosestGMMEntry(gmmTables[apIdx].entries, azAdj, elAdj);
            if (entry == null) continue;

            // 采样该 GMM 100 个点
            foreach (var comp in entry.components)
            {
                int sampleCount = Mathf.CeilToInt(500 * comp.weight);
                for (int i = 0; i < sampleCount; i++)
                {
                    Vector3 sample = SampleGaussian(comp.mean, comp.covarianceDiag);
                    allSamples.Add(sample);
                }
            }
        }

        if (allSamples.Count == 0)
            return Vector3.zero;

        // 平均位置作为估计
        Vector3 meanEst = allSamples.Aggregate(Vector3.zero, (a, b) => a + b) / allSamples.Count;
        return meanEst;
    }

	GMMEntry FindClosestGMMEntry(List<GMMEntry> entries, float azDeg, float elDeg)
	{
		GMMEntry best = null;
		float minDist = float.MaxValue;

		foreach (var entry in entries)
		{
			float dAz = Mathf.Abs(Mathf.DeltaAngle(azDeg, entry.azimuthDeg)); // 处理360度环绕
			float dEl = Mathf.Abs(elDeg - entry.elevationDeg);
			float dist = dAz * dAz + dEl * dEl;

			if (dist < minDist)
			{
				minDist = dist;
				best = entry;
			}
		}

		return best;
	}


    Vector3 SampleGaussian(Vector3 mean, Vector3 diagCov)
    {
        float x = RandomNormal() * Mathf.Sqrt(diagCov.x) + mean.x;
        float y = RandomNormal() * Mathf.Sqrt(diagCov.y) + mean.y;
        float z = RandomNormal() * Mathf.Sqrt(diagCov.z) + mean.z;
        return new Vector3(x, y, z);
    }

    float RandomNormal()
    {
        float u1 = 1.0f - Random.value;
        float u2 = 1.0f - Random.value;
        return Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Cos(2 * Mathf.PI * u2);
    }

	void GetLocationData()
    {
        var data = GlobalDataManager.Instance.Data;
        APCnt = data.AP[0].Count;
        measureCnt = data.AoA[0].Count;

        for (int i = 0; i < APCnt; i++)
        {
            baseStationPositions.Add(new Vector3(data.AP[0][i], data.AP[2][i], data.AP[1][i]));
            APyaw.Add(data.APyaw[i]);
            APpitch.Add(data.APpitch[i]);
        }

        for (int j = 0; j < measureCnt; j++)
        {
            List<float?> aziRow = new List<float?>();
            List<float?> eleRow = new List<float?>();

            for (int m = 0; m < APCnt; m++)
            {
                float? val = data.AoA[m][j] ?? 0f;
                aziRow.Add(val);
                // if (val.HasValue)
                // {
                //     if (aziMin == null || val < aziMin) aziMin = val;
                //     if (aziMax == null || val > aziMax) aziMax = val;
                // }
            }

            for (int m = APCnt; m < 2 * APCnt; m++)
            {
                float? val = data.AoA[m][j] ?? 0f;
                eleRow.Add(val);
                // if (val.HasValue)
                // {
                //     if (eleMin == null || val < eleMin) eleMin = val;
                //     if (eleMax == null || val > eleMax) eleMax = val;
                // }
            }

            azimuths.Add(aziRow);
            elevations.Add(eleRow);
        }

        // Debug.Log($"Azimuth Range: [{aziMin}, {aziMax}]");
        // Debug.Log($"Elevation Range: [{eleMin}, {eleMax}]");

        for (int k = 0; k < measureCnt; k++)
        {
            ground_truth.Add(new Vector3(data.ground_truth[0][k], data.ground_truth[2][k], data.ground_truth[1][k]));
        }
    }
}
