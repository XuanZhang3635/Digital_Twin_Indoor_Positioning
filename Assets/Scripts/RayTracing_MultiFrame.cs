using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayTracing_MultiFrame : MonoBehaviour
{
    public GameObject baseStationPrefab;
    public int raysPerBaseStation = 10;
    public int maxReflections = 5;
    public float azimuthOffsetDeg = 20f;
    public float elevationOffsetDeg = 10f;

    private GameObject[] baseStations;
    private List<Vector3> baseStationPositions = new List<Vector3>();
    private List<float> APpitch = new List<float>();
    private List<float> APyaw = new List<float>();
    private List<List<float?>> azimuths = new List<List<float?>>();
    private List<List<float?>> elevations = new List<List<float?>>();
    private List<Vector3> ground_truth = new List<Vector3>();

    private GameObject rayContainer;
    private List<RayData> rays = new List<RayData>();
    private IntersectionLogger intersectionLogger;
    private IntersectionDetector intersectionDetector;
    private VoxelIntersectionCounter voxelCounter;

    private int currentMeasureIndex = 0;
    private int measureCnt = 0;
    private int APCnt = 0;

    private string outputFile = "Assets/Results/EstimateByIntersection.csv";
    private string outputFile1 = "Assets/Results/EstimateByVoxel.csv";

    void Start()
    {
        GetLocationData();
        WriteHeaders();
        InitializeBaseStations();

        rayContainer = new GameObject("RayContainer");
        intersectionLogger = new IntersectionLogger();
        intersectionDetector = new IntersectionDetector(intersectionLogger);
        voxelCounter = new VoxelIntersectionCounter();

        GameObject roof = GameObject.Find("roof");
        if (roof != null)
        {
            MeshCollider mc = roof.GetComponent<MeshCollider>();
            if (mc == null) mc = roof.AddComponent<MeshCollider>();
            mc.convex = false;

            MeshFilter mf = roof.GetComponentInChildren<MeshFilter>();
            if (mf != null && mf.sharedMesh != null) mc.sharedMesh = mf.sharedMesh;
        }

        StartCoroutine(RunSimulation());
    }

    IEnumerator RunSimulation()
    {
        while (currentMeasureIndex < measureCnt)
        {
            ClearRays();
            InitializeRays();

            yield return new WaitForSeconds(1.0f);

            voxelCounter.CountVoxels(rays);
            intersectionLogger.SaveRaysToFile(rays);
            intersectionDetector.DetectIntersections(rays);
            intersectionLogger.SaveIntersectionsToFile();

            Vector3 est = VoxelUtils.ComputeAveragePositionFromFile("Assets/FourRayIntersections.txt");
            Debug.Log("Estimated position (four-line intersection): " + est);
            Vector3 est2 = VoxelUtils.ComputeAveragePositionFromFile("Assets/voxel_hits.txt");
            Debug.Log("Estimated position (voxel): " + est2);

            // Compute the error
            float errorIntersection = Vector3.Distance(ground_truth[currentMeasureIndex], est);
            float errorVoxel = Vector3.Distance(ground_truth[currentMeasureIndex], est2);
            // Save the results to CSV files
            SaveResult(outputFile, ground_truth[currentMeasureIndex], est, errorIntersection);
            SaveResult(outputFile1, ground_truth[currentMeasureIndex], est2, errorVoxel);

            currentMeasureIndex++;
        }

        Debug.Log("All measurements completed");
    }

    void ClearRays()
    {
        rays.Clear();
        foreach (Transform child in rayContainer.transform) Destroy(child.gameObject);
    }

    void InitializeBaseStations()
    {
        baseStations = new GameObject[baseStationPositions.Count];
        for (int i = 0; i < baseStationPositions.Count; i++)
        {
            baseStations[i] = Instantiate(baseStationPrefab, baseStationPositions[i], Quaternion.identity);
            baseStations[i].name = "BaseStation" + (i + 1);
        }
    }

    void InitializeRays()
    {
        Color[] colors = new Color[] { Color.red, Color.green, Color.blue, Color.yellow };

        for (int i = 0; i < baseStations.Length; i++)
        {
            Vector3 pos = baseStations[i].transform.position;
            for (int j = 0; j < raysPerBaseStation; j++)
            {
                float? az = azimuths[currentMeasureIndex][i];
                float? el = elevations[currentMeasureIndex][i];
                if (!az.HasValue || !el.HasValue) continue;

                float azDeg = Mathf.Rad2Deg * az.Value + APyaw[i] + UnityEngine.Random.Range(-azimuthOffsetDeg, azimuthOffsetDeg);
                float elDeg = Mathf.Rad2Deg * el.Value + UnityEngine.Random.Range(-elevationOffsetDeg, elevationOffsetDeg);

                Vector3 dir = SphericalToCartesianRad(Mathf.Deg2Rad * azDeg, Mathf.Deg2Rad * elDeg);

                GameObject lineObject = new GameObject("Ray_" + i + "_" + j);
                lineObject.transform.parent = rayContainer.transform;

                LineRenderer lr = lineObject.AddComponent<LineRenderer>();
                lr.startWidth = lr.endWidth = 0.05f;
                lr.material = new Material(Shader.Find("Sprites/Default"));
                lr.startColor = lr.endColor = colors[i % colors.Length];

                Ray ray = new Ray(pos, dir);
                rays.Add(new RayData(ray, lr, i, colors[i % colors.Length], maxReflections));
            }
        }
    }

    Vector3 SphericalToCartesianRad(float az, float el)
    {
        float x = Mathf.Cos(el) * Mathf.Cos(az);
        float y = Mathf.Sin(el);
        float z = Mathf.Cos(el) * Mathf.Sin(az);
        return new Vector3(x, y, z);
    }

    // Write headers to CSV files if they don't exist
    void WriteHeaders()
    {
        // 如果文件已经存在，删除它
        if (File.Exists(outputFile))
        {
            File.Delete(outputFile);
        }

        // 创建新的文件并写入表头
        using (StreamWriter sw = new StreamWriter(outputFile, false))
        {
            sw.WriteLine("GT_X,GT_Y,GT_Z,EST_X,EST_Y,EST_Z,Error");
        }

        // 如果文件已经存在，删除它
        if (File.Exists(outputFile1))
        {
            File.Delete(outputFile1);
        }

        // 创建新的文件并写入表头
        using (StreamWriter sw = new StreamWriter(outputFile1, false))
        {
            sw.WriteLine("GT_X,GT_Y,GT_Z,EST_X,EST_Y,EST_Z,Error");
        }
    }


    void SaveResult(string path, Vector3 gt, Vector3 est, float error)
    {
        using (StreamWriter sw = new StreamWriter(path, true))
        {
            sw.WriteLine($"{gt.x:F3},{gt.y:F3},{gt.z:F3},{est.x:F3},{est.y:F3},{est.z:F3},{error:F3}");
        }
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
            for (int m = 0; m < APCnt; m++) aziRow.Add(data.AoA[m][j]);
            for (int m = APCnt; m < 2 * APCnt; m++) eleRow.Add(data.AoA[m][j]);
            azimuths.Add(aziRow);
            elevations.Add(eleRow);
        }

        for (int k = 0; k < measureCnt; k++)
        {
            ground_truth.Add(new Vector3(data.ground_truth[0][k], data.ground_truth[2][k], data.ground_truth[1][k]));
        }
    }

    void Update()
    {
        foreach (var rayData in rays) UpdateRay(rayData);
    }

    void UpdateRay(RayData rayData)
    {
        if (rayData.reflectionCount >= maxReflections) return;

        if (Physics.Raycast(rayData.ray, out RaycastHit hitInfo, Mathf.Infinity))
        {
            rayData.AddHitPoint(hitInfo.point);
            rayData.ray.origin = hitInfo.point;
            rayData.ray.direction = Vector3.Reflect(rayData.ray.direction, hitInfo.normal).normalized;

            rayData.lineRenderer.positionCount += 2;
            rayData.lineRenderer.SetPosition(rayData.reflectionCount * 2, hitInfo.point);
            rayData.lineRenderer.SetPosition(rayData.reflectionCount * 2 + 1, rayData.ray.origin);

            rayData.reflectionCount++;
        }
    }
}
