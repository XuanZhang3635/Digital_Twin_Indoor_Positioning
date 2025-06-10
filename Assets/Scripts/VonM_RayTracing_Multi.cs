using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class VonM_RayTracing_Multi : MonoBehaviour
{
    public GameObject baseStationPrefab;
    public int raysPerBaseStation = 10;
    public int maxReflections = 5;

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
    private string basePath = "Assets/Results/";
    private int topK = 10;
    private string outputFile1;

    private class APParams
    {
        public string AP;  
        public float Azimuth_Gauss_Mean_rad;
        public float Azimuth_Gauss_Std_rad;
        public float Elevation_Gauss_Mean_rad;
        public float Elevation_Gauss_Std_rad;
    }
    private string csvFileName = "distribution_params_rad.csv"; 
    private List<APParams> apList = new List<APParams>();
    public float test_kappa_azi = 4f;
    public float test_kappa_ele = 4f;
    void Start()
    {
        outputFile1 = Path.Combine(basePath, "EstimateByVoxel_" + "VM_"  + test_kappa_azi + "_" + test_kappa_ele + "_" + raysPerBaseStation + "_" + maxReflections + ".csv");

        GetLocationData();
        WriteHeaders();
        LoadAPParamsFromCSV();
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

            // 获得建筑物边界用作采样限制
            // 获取所有子 Renderer
            Renderer[] renderers = roof.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                Bounds combinedBounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                {
                    combinedBounds.Encapsulate(renderers[i].bounds);
                }
            }
            else
            {
                Debug.LogWarning("No Renderers found in roof or its children.");
            }
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

            // intersectionLogger.SaveRaysToFile(rays);
            // intersectionDetector.DetectIntersections(rays);
            // intersectionLogger.SaveIntersectionsToFile();

            // Vector3 est = VoxelUtils.ComputeAveragePositionFromFile("Assets/FourRayIntersections.txt");
            // Debug.Log("Estimated position (four-line intersection): " + est);

            Vector3 est2 = VoxelUtils.ComputeWeightedPositionFromFile("Assets/voxel_hits.txt");

            // Vector3 est2 = VoxelUtils.ComputeStableEstimateFromFile("Assets/voxel_hits.txt",topK);

            // Vector3 est2 = voxelCounter.ComputeLocalWeightedCentroid();

            // Vector3 est2 = KDEEstimator.EstimateByKDEFromVoxelFile("Assets/voxel_hits.txt", 0.5f);

            // Vector3 est2 = VoxelEstimator.ComputeFilteredWeightedCentroid("Assets/voxel_hits.txt",0.8f);


            // Compute the error
            // float errorIntersection = Vector3.Distance(ground_truth[currentMeasureIndex], est);
            float errorVoxel = Vector3.Distance(ground_truth[currentMeasureIndex], est2);
            Debug.Log("error of voxel:" + errorVoxel);
            // Save the results to CSV files
            // SaveResult(outputFile, ground_truth[currentMeasureIndex], est, errorIntersection);
            SaveResult(outputFile1, ground_truth[currentMeasureIndex], est2, errorVoxel);

            currentMeasureIndex++;
        }

        Debug.Log("All measurements completed!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
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

            // 添加碰撞器（如果没有的话）
            if (baseStations[i].GetComponent<Collider>() == null)
            {
                baseStations[i].AddComponent<BoxCollider>();
            }
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
                float azi = (azimuths[currentMeasureIndex][i] ?? 0f)+ Mathf.Deg2Rad*APyaw[i] + VonMisesRandom(0,test_kappa_azi);
                float ele = (elevations[currentMeasureIndex][i] ?? 0f)+ VonMisesRandom(0,test_kappa_ele);
                Vector3 dir = SphericalToCartesian(azi,ele);

                GameObject lineObject = new GameObject("Ray_" + i + "_" + j);
                lineObject.transform.parent = rayContainer.transform;

                LineRenderer lr = lineObject.AddComponent<LineRenderer>();
                lr.startWidth = lr.endWidth = 0.02f;
                lr.material = new Material(Shader.Find("Sprites/Default"));
                lr.startColor = lr.endColor = colors[i % colors.Length];

                // ✅ 正确设置 LineRenderer 的世界坐标点
                // lr.positionCount = 2;
                // lr.SetPosition(0, pos); // 起点：基站位置
                // lr.SetPosition(1, pos + dir * 500f); // 终点：延着方向延伸

                Ray ray = new Ray(pos, dir);
                rays.Add(new RayData(ray, lr, i, colors[i % colors.Length], maxReflections));
            }
        }
    }

    public static float VonMisesRandom(float mean, float kappa)
    {
        // 使用 Best 和 Fisher 的算法（适用于 kappa > 1）
        float s = 0.5f / kappa;
        float r = s + Mathf.Sqrt(1 + s * s);

        while (true)
        {
            float u1 = UnityEngine.Random.value;
            float z = Mathf.Cos(Mathf.PI * u1);
            float f = (1 + r * z) / (r + z);
            float c = kappa * (r - f);

            float u2 = UnityEngine.Random.value;
            if (u2 < c * (2 - c) || u2 <= c * Mathf.Exp(1 - c))
            {
                float u3 = UnityEngine.Random.value;
                float theta = Mathf.Acos(f); // theta ∈ [0, π]

                if (u3 > 0.5f)
                    theta = -theta;

                // 输出的角度加上 mean，结果范围在 [-π, π] 内
                float result = mean + theta;

                // 保证返回值落在 [-π, π]
                result = Mathf.Repeat(result + Mathf.PI, 2 * Mathf.PI) - Mathf.PI;
                return result;
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

    void Update()
    {
        foreach (var rayData in rays) UpdateRay(rayData);
    }

    void UpdateRay(RayData rayData)
    {
        if (rayData.reflectionCount >= maxReflections) return;

        Vector3 startPoint = rayData.ray.origin; // 原始起点
        if (Physics.Raycast(rayData.ray, out RaycastHit hitInfo, Mathf.Infinity))
        {
            Vector3 hitPoint = hitInfo.point;

            // 先设置当前线段
            int index = rayData.lineRenderer.positionCount;
            rayData.lineRenderer.positionCount += 2;
            rayData.lineRenderer.SetPosition(index, startPoint);
            rayData.lineRenderer.SetPosition(index + 1, hitPoint);
            rayData.lineRenderer.useWorldSpace = true;

            // 更新射线方向和起点
            rayData.ray.origin = hitPoint;
            rayData.ray.direction = Vector3.Reflect(rayData.ray.direction, hitInfo.normal).normalized;

            rayData.reflectionCount++;
        }
    }

    (float, float) CartesianToSpherical(Vector3 dir)
    {
        float az = Mathf.Atan2(dir.z, dir.x);
        float el = Mathf.Asin(dir.y);
        return (az, el);
    }

    Vector3 SphericalToCartesian(float az, float el)
    {
        float x = Mathf.Cos(el) * Mathf.Cos(az);
        float y = Mathf.Sin(el);
        float z = Mathf.Cos(el) * Mathf.Sin(az);
        return new Vector3(x, y, z);
    }

    // Write headers to CSV files if they don't exist
    void WriteHeaders()
    {
        // if (File.Exists(outputFile))
        // {
        //     File.Delete(outputFile);
        // }

        // using (StreamWriter sw = new StreamWriter(outputFile, false))
        // {
        //     sw.WriteLine("GT_X,GT_Y,GT_Z,EST_X,EST_Y,EST_Z,Error");
        // }

        if (File.Exists(outputFile1))
        {
            File.Delete(outputFile1);
        }

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

            for (int m = 0; m < APCnt; m++)
            {
                float? val = data.AoA[m][j] ?? 0f;
                aziRow.Add(val);
            }

            for (int m = APCnt; m < 2 * APCnt; m++)
            {
                float? val = data.AoA[m][j] ?? 0f;
                eleRow.Add(val);
            }

            azimuths.Add(aziRow);
            elevations.Add(eleRow);
        }

        for (int k = 0; k < measureCnt; k++)
        {
            ground_truth.Add(new Vector3(data.ground_truth[0][k], data.ground_truth[2][k], data.ground_truth[1][k]));
        }
    }
    
    void LoadAPParamsFromCSV()
    {
        string filePath = Path.Combine(Application.dataPath, csvFileName);

        if (!File.Exists(filePath))
        {
            Debug.LogError("CSV file not found at " + filePath);
            return;
        }

        string[] lines = File.ReadAllLines(filePath);
        for (int i = 1; i < lines.Length; i++) // skip header
        {
            string[] tokens = lines[i].Split(',');

            if (tokens.Length < 5) continue;

            APParams ap = new APParams
            {
                AP = tokens[0],
                Azimuth_Gauss_Mean_rad = float.Parse(tokens[1]),
                Azimuth_Gauss_Std_rad = float.Parse(tokens[2]),
                Elevation_Gauss_Mean_rad = float.Parse(tokens[5]),
                Elevation_Gauss_Std_rad = float.Parse(tokens[6]),
            };
            apList.Add(ap);
        }

        Debug.Log($"✅ Loaded {apList.Count} AP entries from CSV.");
    }
}
